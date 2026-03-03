namespace GorillaLocomotion.Editor
{
    using UnityEditor;
    using UnityEngine;
    

    public static class GorillaLocomotionSetup
    {
        [MenuItem("Tools/Gorilla Locomotion/Setup Scene")]
        public static void SetupScene()
        {
            // --- Find or validate XR Origin ---
            var xrOriginComponent = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOriginComponent == null)
            {
                EditorUtility.DisplayDialog("Gorilla Locomotion Setup",
                    "No XR Origin found in the scene. Add the 'XR Origin (XR Rig)' prefab first.",
                    "OK");
                return;
            }

            GameObject xrOrigin = xrOriginComponent.gameObject;

            if (xrOrigin.transform.parent != null &&
                xrOrigin.transform.parent.GetComponent<Player>() != null)
            {
                EditorUtility.DisplayDialog("Gorilla Locomotion Setup",
                    "Scene already has a GorillaPlayer wrapping the XR Origin.",
                    "OK");
                return;
            }

            Undo.SetCurrentGroupName("Setup Gorilla Locomotion");
            int undoGroup = Undo.GetCurrentGroup();

            // --- Create GorillaPlayer root ---
            var gorillaPlayer = new GameObject("GorillaPlayer");
            Undo.RegisterCreatedObjectUndo(gorillaPlayer, "Create GorillaPlayer");
            gorillaPlayer.transform.position = xrOrigin.transform.position;

            // Rigidbody
            var rb = Undo.AddComponent<Rigidbody>(gorillaPlayer);
            rb.mass = 1f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // Body CapsuleCollider — must be on a CHILD object, not GorillaPlayer itself.
            // Player.cs rotates bodyCollider.transform each frame to match head yaw.
            // If bodyCollider is on the root, that rotation also spins the XR Origin child,
            // which moves the controller transforms and causes a spin feedback loop.
            var bodyColliderGO = new GameObject("BodyCollider");
            Undo.RegisterCreatedObjectUndo(bodyColliderGO, "Create BodyCollider");
            bodyColliderGO.transform.SetParent(gorillaPlayer.transform, false);
            var bodyCapsule = Undo.AddComponent<CapsuleCollider>(bodyColliderGO);
            bodyCapsule.height = 1.8f;
            bodyCapsule.radius = 0.3f;
            bodyCapsule.center = new Vector3(0f, 0.9f, 0f);

            // Player script
            var player = Undo.AddComponent<Player>(gorillaPlayer);

            // --- Reparent XR Origin under GorillaPlayer ---
            Undo.SetTransformParent(xrOrigin.transform, gorillaPlayer.transform, "Reparent XR Origin");
            xrOrigin.transform.localPosition = Vector3.zero;
            xrOrigin.transform.localRotation = Quaternion.identity;

            // --- Disable conflicting locomotion components ---
            DisableLocomotionComponents(xrOrigin);

            // --- Find camera, controllers ---
            Camera mainCamera = xrOriginComponent.Camera;
            if (mainCamera == null)
                mainCamera = xrOrigin.GetComponentInChildren<Camera>();

            Transform cameraOffset = xrOriginComponent.CameraFloorOffsetObject != null
                ? xrOriginComponent.CameraFloorOffsetObject.transform
                : xrOrigin.transform;

            Transform leftController = FindHandTransform(xrOrigin, true);
            Transform rightController = FindHandTransform(xrOrigin, false);

            // --- Add SphereCollider to camera head ---
            SphereCollider headCollider = null;
            if (mainCamera != null)
            {
                headCollider = Undo.AddComponent<SphereCollider>(mainCamera.gameObject);
                headCollider.radius = 0.2f;
                headCollider.center = Vector3.zero;
            }

            // --- Create hand follower GameObjects ---
            var leftFollowerGO = new GameObject("LeftHandFollower");
            Undo.RegisterCreatedObjectUndo(leftFollowerGO, "Create LeftHandFollower");
            leftFollowerGO.transform.SetParent(cameraOffset, false);

            var rightFollowerGO = new GameObject("RightHandFollower");
            Undo.RegisterCreatedObjectUndo(rightFollowerGO, "Create RightHandFollower");
            rightFollowerGO.transform.SetParent(cameraOffset, false);

            // --- Wire Player references ---
            if (headCollider != null)
                player.headCollider = headCollider;

            player.bodyCollider = bodyCapsule;
            player.leftHandFollower = leftFollowerGO.transform;
            player.rightHandFollower = rightFollowerGO.transform;

            if (leftController != null)
                player.leftHandTransform = leftController;

            if (rightController != null)
                player.rightHandTransform = rightController;

            // Recommended default values
            player.velocityHistorySize = 10;
            player.maxArmLength = 1.5f;
            player.unStickDistance = 1f;
            player.velocityLimit = 0.5f;
            player.maxJumpSpeed = 6.5f;
            player.jumpMultiplier = 1.1f;
            player.minimumRaycastDistance = 0.05f;
            player.defaultSlideFactor = 0.03f;
            player.defaultPrecision = 0.995f;

            // Default layers: locomotionEnabledLayers = Default layer
            player.locomotionEnabledLayers = LayerMask.GetMask("Default");

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.SetDirty(gorillaPlayer);
            Selection.activeGameObject = gorillaPlayer;

            string leftStatus = leftController != null ? leftController.name : "NOT FOUND — assign manually";
            string rightStatus = rightController != null ? rightController.name : "NOT FOUND — assign manually";
            string camStatus = mainCamera != null ? mainCamera.name : "NOT FOUND — assign manually";

            EditorUtility.DisplayDialog("Gorilla Locomotion Setup Complete",
                $"GorillaPlayer created.\n\n" +
                $"Head: {camStatus}\n" +
                $"Left hand: {leftStatus}\n" +
                $"Right hand: {rightStatus}\n\n" +
                "Check the Player component in the Inspector to verify all references, " +
                "and set Locomotion Enabled Layers to include the layer(s) your environment geometry uses.",
                "OK");
        }

        private static void DisableLocomotionComponents(GameObject xrOrigin)
        {
            // Disable CharacterController (conflicts with Rigidbody-driven movement)
            var cc = xrOrigin.GetComponent<CharacterController>();
            if (cc != null)
            {
                Undo.RecordObject(cc, "Disable CharacterController");
                cc.enabled = false;
            }

            // Disable XRI locomotion providers (Move, Turn, Gravity / Climb, etc.)
            foreach (var provider in xrOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>())
            {
                Undo.RecordObject(provider, "Disable LocomotionProvider");
                provider.enabled = false;
            }

            // Disable the locomotion mediator if present (XRI 3.x)
            foreach (var comp in xrOrigin.GetComponentsInChildren<MonoBehaviour>())
            {
                if (comp == null) continue;
                string typeName = comp.GetType().Name;
                if (typeName == "LocomotionMediator" || typeName == "XRBodyTransformer")
                {
                    Undo.RecordObject(comp, $"Disable {typeName}");
                    comp.enabled = false;
                }
            }
        }

        private static Transform FindHandTransform(GameObject xrOrigin, bool isLeft)
        {
            string keyword = isLeft ? "Left" : "Right";
            foreach (Transform t in xrOrigin.GetComponentsInChildren<Transform>())
            {
                string n = t.name;
                if (n.Contains(keyword) && (n.Contains("Controller") || n.Contains("Hand") || n.Contains("Interactor")))
                    return t;
            }
            return null;
        }
    }
}
