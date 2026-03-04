# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 6 VR project implementing Gorilla Tag-style locomotion (physics-based hand-push movement). Based on [Another-Axiom/GorillaLocomotion](https://github.com/Another-Axiom/GorillaLocomotion) (MIT).

- **Unity version**: 6000.3.10f1 (Unity 6)
- **Render pipeline**: URP 17.3.0
- **Build target**: Android (Meta Quest / OpenXR headsets)

## MCP Unity Server

This project has `com.gamelovers.mcp-unity` (v1.2.0, via Git) installed. Claude Code can interact with the Unity Editor directly via MCP tools:

- Inspect/modify GameObjects, components, and transforms
- Read scene hierarchy, assets, and packages
- Execute menu items, create/load/save scenes
- Read Unity console logs and run tests
- Recompile scripts

Use these MCP tools instead of guessing scene state — always query the live editor when you need current values.

## Common Commands

Unity has no CLI build/test commands in this project. All tasks are done through the Unity Editor:

- **Scene setup**: `Tools > Gorilla Locomotion > Setup Scene` (requires an XR Origin already in scene)
- **Build**: File > Build Settings > Android > Build
- **Play in editor**: Press Play (requires XR Device Simulator or a connected headset)

## Architecture

### Core Scripts (`Assets/GorillaLocomotion/`)

**`Player.cs`** — Main locomotion driver. Every `Update()`:
1. SphereCasts from each hand position (offset by `handHeightBias` downward) to detect surface contact
2. When a hand "sticks" to a surface, tracks how its world-space position changes relative to body movement
3. On release, applies the inverse of accumulated hand movement as a velocity impulse to the `Rigidbody`
4. Runs an iterative collision solver (`defaultPrecision`) and uses velocity history averaging (`velocityHistorySize` frames)
5. Rotates `bodyCollider.transform` every frame to match head yaw — this is why BodyCollider must be a separate child GO

**`Surface.cs`** — Tiny component; adds `slipPercentage` to any surface GO (0 = sticky, higher = slippery).

**`Editor/GorillaLocomotionSetup.cs`** — Editor wizard that builds the entire scene hierarchy automatically.

### Scene Hierarchy (Critical)

```
GorillaPlayer              ← Rigidbody + Player script (NO collider here)
  BodyCollider             ← CapsuleCollider only (rotates with head yaw each frame)
  XR Origin (XR Rig)      ← CharacterController DISABLED, XROrigin, InputActionManager
    Camera Offset
      Main Camera          ← SphereCollider (headCollider ref), TrackedPoseDriver
      Gaze Interactor      ← XRGazeInteractor, GazeInputManager
      Left Controller      ← leftHandTransform ref, XRInteractionGroup, TrackedPoseDriver
        (Poke/Near-Far/Teleport Interactors, Left Controller Visual)
      Right Controller     ← rightHandTransform ref (mirror of Left)
      LeftHandFollower     ← leftHandFollower ref (empty GO)
      RightHandFollower    ← rightHandFollower ref (empty GO)
    Locomotion             ← LocomotionMediator, XRBodyTransformer
      (Turn, Move, Grab Move, Teleportation, Climb, Gravity, Jump providers — must be DISABLED)
```

**Why BodyCollider is a separate child**: `Player.cs` calls `bodyCollider.transform.eulerAngles = new Vector3(0, head.y, 0)` every frame. If this collider were on the GorillaPlayer root, it would rotate XR Origin too — spinning tracked controller positions and creating a runaway feedback loop.

**Why XRI locomotion providers are disabled**: They conflict with Rigidbody-driven movement. GorillaLocomotion drives the player entirely through `Rigidbody.linearVelocity` (Unity 6 API).

### Packages

Key installed packages:

| Package | Version | Notes |
|---|---|---|
| XR Interaction Toolkit | 3.3.1 | Uses `LocomotionMediator`, not old `LocomotionSystem` |
| OpenXR | 1.16.1 | |
| Android XR OpenXR | 1.1.0 | |
| XR Hands | 1.7.3 | |
| XR Management | 4.5.4 | |
| Input System | 1.18.0 | |
| URP | 17.3.0 | |
| AR Foundation | 6.3.3 | |
| MCP Unity Server | 1.2.0 | `com.gamelovers.mcp-unity` (Git) — Claude Code integration |

CharacterController and all XRI locomotion providers must be disabled — incompatible with GorillaLocomotion's Rigidbody approach.

## Key API Notes

- Use `Rigidbody.linearVelocity` (not `.velocity`) — this is the Unity 6 API
- `locomotionEnabledLayers` must include the layer(s) of environment geometry or no surfaces will trigger locomotion
