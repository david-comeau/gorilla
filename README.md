# Gorilla VR

A Unity VR project implementing Gorilla Tag-style locomotion — move by physically pushing off surfaces with your hands.

Based on [GorillaLocomotion](https://github.com/Another-Axiom/GorillaLocomotion) (MIT).

---

## Requirements

- Unity 6 (URP)
- A supported OpenXR headset (Meta Quest, etc.)

**Packages (already configured in `manifest.json`):**

| Package | Version |
|---|---|
| XR Interaction Toolkit | 3.3.1 |
| OpenXR | 1.16.1 |
| XR Hands | 1.7.3 |
| Input System | 1.18.0 |
| Universal RP | 17.3.0 |

---

## Scene Setup

Run the automated setup from the Unity menu:

**Tools > Gorilla Locomotion > Setup Scene**

This requires an **XR Origin (XR Rig)** to already be present in the scene. The tool will:

1. Create a `GorillaPlayer` root GameObject with a `Rigidbody` and `Player` script
2. Create a `BodyCollider` child with a `CapsuleCollider`
3. Reparent the XR Origin under `GorillaPlayer`
4. Disable the XR Origin's `CharacterController` and all XRI locomotion providers (they conflict with Rigidbody-driven movement)
5. Add a `SphereCollider` to the Main Camera
6. Create `LeftHandFollower` and `RightHandFollower` GameObjects
7. Wire all references on the `Player` component automatically

After setup, open the `Player` component in the Inspector and:
- Verify all Transform/Collider references are assigned
- Set **Locomotion Enabled Layers** to include the layer(s) your environment geometry uses

---

## Scene Hierarchy

```
GorillaPlayer              ← Rigidbody + Player script (no collider here)
  BodyCollider             ← CapsuleCollider (rotates with head yaw)
  XR Origin (XR Rig)      ← CharacterController DISABLED, locomotion providers DISABLED
    Camera Offset
      Main Camera          ← SphereCollider (headCollider)
      Left Controller      ← leftHandTransform
      Right Controller     ← rightHandTransform
      LeftHandFollower     ← leftHandFollower (empty GO, tracks left hand contact point)
      RightHandFollower    ← rightHandFollower (empty GO, tracks right hand contact point)
```

> **Why is `BodyCollider` a separate child?**
> `Player.cs` rotates `bodyCollider.transform` every frame to match the head's yaw. If the collider were on the `GorillaPlayer` root, this would also rotate the `XR Origin` child — spinning the tracked controller positions and creating a runaway feedback loop in VR.

---

## Player Inspector Parameters

| Field | Default | Description |
|---|---|---|
| `velocityHistorySize` | 10 | Frames of velocity history used to calculate launch speed |
| `maxArmLength` | 1.5 | Maximum reach distance from head to hand (meters) |
| `unStickDistance` | 1.0 | How far a hand can move from its contact point before it unsticks |
| `velocityLimit` | 0.5 | Minimum velocity required to trigger a jump/push |
| `maxJumpSpeed` | 6.5 | Maximum launch speed (m/s) |
| `jumpMultiplier` | 1.1 | Scalar applied to launch velocity |
| `minimumRaycastDistance` | 0.05 | Sphere radius used for hand collision detection |
| `defaultSlideFactor` | 0.03 | How much hands slide along a surface (lower = stickier) |
| `defaultPrecision` | 0.995 | Iterative collision solver precision |
| `handHeightBias` | 0.5 | Lowers the locomotion detection point below the physical hand in world space, so you can push off the floor without physically bending down. Tune until floor contact feels natural. |
| `locomotionEnabledLayers` | Default | LayerMask — only geometry on these layers triggers locomotion |

---

## Scripts

### `Assets/GorillaLocomotion/Player.cs`
Main locomotion driver. Runs every frame in `Update()`:
- Raycasts from each hand's position to detect surface contact
- When a hand contacts a surface and moves away from it, the inverse of that movement is applied to the `Rigidbody` as a push
- Velocity is averaged over `velocityHistorySize` frames and applied on release

### `Assets/GorillaLocomotion/Surface.cs`
Add this component to any GameObject to override its slip factor. `slipPercentage = 0` is fully sticky; higher values make the surface more slippery.

### `Assets/GorillaLocomotion/Editor/GorillaLocomotionSetup.cs`
Editor-only setup wizard (see **Scene Setup** above).

---

## How Locomotion Works

The system tracks each hand's world-space position each frame. When a hand is near a surface (detected via `SphereCast`), it "sticks" to that point. As the player's body moves, the hand's world position changes — the difference is applied as a force to the Rigidbody in the opposite direction, pushing the player.

Swinging both arms downward and releasing pushes the player upward, just like in Gorilla Tag.

The `handHeightBias` field shifts the collision detection point downward from the physical controller position, allowing natural arm swings to reach the floor without the player physically bending down.

---

## License

GorillaLocomotion source: MIT — [Another-Axiom/GorillaLocomotion](https://github.com/Another-Axiom/GorillaLocomotion)
