using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SS3D.Editor
{
    /// <summary>
    /// Rebuilds the Human Character Movement blend tree to use the full Locomotion Pack
    /// (idle / walk / run / strafes) plus Jump and in-place Turn states.
    /// </summary>
    public static class HumanoidLocomotionBlendSetup
    {
        private const string ControllerPath =
            "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanCharacterAnimator.controller";
        private const string PackFolder = "Assets/Art/Animations/Locomotion Pack";

        private static readonly (string File, string ClipName, Vector2 Pos)[] LocomotionClips =
        {
            ("idle.fbx", "Mix_Idle", new Vector2(0f, 0f)),
            ("walking.fbx", "Mix_Walking", new Vector2(0f, 0.3f)),
            ("running.fbx", "Mix_Running", new Vector2(0f, 1f)),
            ("left strafe walking.fbx", "Mix_LeftStrafeWalking", new Vector2(-1f, 0.3f)),
            ("right strafe walking.fbx", "Mix_RightStrafeWalking", new Vector2(1f, 0.3f)),
            ("left strafe.fbx", "Mix_LeftStrafe", new Vector2(-1f, 1f)),
            ("right strafe.fbx", "Mix_RightStrafe", new Vector2(1f, 1f)),
            // Continuous turn-in-place near idle, driven when VelZ≈0 and VelX from yaw.
            ("left turn.fbx", "Mix_LeftTurn", new Vector2(-0.35f, 0f)),
            ("right turn.fbx", "Mix_RightTurn", new Vector2(0.35f, 0f)),
        };

        [MenuItem("SS3D/Animation/Rebuild Locomotion Pack Blend Tree")]
        public static void RebuildLocomotionBlendTreeMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Locomotion Blend Tree",
                    "Replace the Movement 1D Speed blend with a FreeformCartesian2D tree using " +
                    "idle/walk/run/strafes/turns from Locomotion Pack, and add Jump + Turn90 states.\n\n" +
                    "This modifies HumanCharacterAnimator.controller.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            string result = RebuildLocomotionBlendTree();
            EditorUtility.DisplayDialog("Rebuild Locomotion Blend Tree", result, "OK");
        }

        /// <summary>Batchmode entry: -executeMethod SS3D.Editor.HumanoidLocomotionBlendSetup.RebuildLocomotionBlendTreeBatch</summary>
        public static void RebuildLocomotionBlendTreeBatch()
        {
            string result = RebuildLocomotionBlendTree();
            Debug.Log($"[HumanoidLocomotionBlendSetup] {result}");
            if (result.StartsWith("ERROR"))
            {
                EditorApplication.Exit(1);
            }
        }

        public static string RebuildLocomotionBlendTree()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                return $"ERROR: Missing controller at {ControllerPath}";
            }

            EnsureFloatParam(controller, "VelX");
            EnsureFloatParam(controller, "VelZ");
            EnsureFloatParam(controller, "Turn");
            EnsureTriggerParam(controller, "Jump");
            EnsureTriggerParam(controller, "TurnLeft90");
            EnsureTriggerParam(controller, "TurnRight90");

            AnimatorStateMachine baseMachine = controller.layers[0].stateMachine;
            AnimatorState movementState = FindState(baseMachine, "Movement");
            if (movementState == null)
            {
                return "ERROR: Movement state not found on Base Layer";
            }

            BlendTree tree = new BlendTree
            {
                name = "Locomotion 2D",
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = "VelX",
                blendParameterY = "VelZ",
                useAutomaticThresholds = false,
            };

            List<(AnimationClip Clip, Vector2 Pos)> children = new();
            foreach ((string File, string ClipName, Vector2 Pos) entry in LocomotionClips)
            {
                AnimationClip clip = LoadPackClip($"{PackFolder}/{entry.File}", entry.ClipName);
                if (clip == null)
                {
                    return $"ERROR: Could not load clip '{entry.ClipName}' from {entry.File}";
                }

                children.Add((clip, entry.Pos));
            }

            // Side-step at zero forward so pure strafe input blends cleanly.
            AnimationClip leftStrafeWalk = children.First(c => c.Clip.name == "Mix_LeftStrafeWalking").Clip;
            AnimationClip rightStrafeWalk = children.First(c => c.Clip.name == "Mix_RightStrafeWalking").Clip;
            children.Add((leftStrafeWalk, new Vector2(-1f, 0f)));
            children.Add((rightStrafeWalk, new Vector2(1f, 0f)));

            foreach ((AnimationClip Clip, Vector2 Pos) child in children)
            {
                tree.AddChild(child.Clip, child.Pos);
            }

            AssetDatabase.AddObjectToAsset(tree, controller);
            movementState.motion = tree;

            AnimationClip jumpClip = LoadPackClip($"{PackFolder}/jump.fbx", "Mix_Jump");
            AnimationClip turnLeft90 = LoadPackClip($"{PackFolder}/left turn 90.fbx", "Mix_LeftTurn90");
            AnimationClip turnRight90 = LoadPackClip($"{PackFolder}/right turn 90.fbx", "Mix_RightTurn90");
            if (jumpClip == null || turnLeft90 == null || turnRight90 == null)
            {
                return "ERROR: Missing jump or turn-90 clips";
            }

            AnimatorState jumpState = FindOrCreateState(baseMachine, "Jump", new Vector3(550, 120, 0));
            jumpState.motion = jumpClip;
            jumpState.writeDefaultValues = true;

            AnimatorState turnLeft90State = FindOrCreateState(baseMachine, "Turn Left 90", new Vector3(550, 200, 0));
            turnLeft90State.motion = turnLeft90;

            AnimatorState turnRight90State = FindOrCreateState(baseMachine, "Turn Right 90", new Vector3(550, 280, 0));
            turnRight90State.motion = turnRight90;

            EnsureAnyStateTrigger(baseMachine, jumpState, "Jump", canTransitionToSelf: false);
            EnsureAnyStateTrigger(baseMachine, turnLeft90State, "TurnLeft90", canTransitionToSelf: false);
            EnsureAnyStateTrigger(baseMachine, turnRight90State, "TurnRight90", canTransitionToSelf: false);

            EnsureExitToMovement(jumpState, movementState, hasExitTime: true, exitTime: 0.85f, duration: 0.1f);
            EnsureExitToMovement(turnLeft90State, movementState, hasExitTime: true, exitTime: 0.9f, duration: 0.1f);
            EnsureExitToMovement(turnRight90State, movementState, hasExitTime: true, exitTime: 0.9f, duration: 0.1f);

            // Keep Speed as magnitude alias for legacy bindings / ragdoll zeroing.
            // Runtime still writes Speed = length of (VelX, VelZ) for the 1D fallthrough states.

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return $"OK: Rebuilt Movement FreeformCartesian2D with {children.Count} clips; " +
                   "Jump / Turn Left 90 / Turn Right 90 states wired.";
        }

        private static AnimationClip LoadPackClip(string fbxPath, string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && clip.name == clipName && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }

            // Fallback: first non-preview clip in the FBX.
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    Debug.LogWarning($"[HumanoidLocomotionBlendSetup] Using '{clip.name}' instead of '{clipName}' from {fbxPath}");
                    return clip;
                }
            }

            return null;
        }

        private static void EnsureFloatParam(AnimatorController controller, string name)
        {
            if (controller.parameters.Any(p => p.name == name))
            {
                return;
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Float);
        }

        private static void EnsureTriggerParam(AnimatorController controller, string name)
        {
            if (controller.parameters.Any(p => p.name == name))
            {
                return;
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string name)
        {
            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state != null && child.state.name == name)
                {
                    return child.state;
                }
            }

            return null;
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine machine, string name, Vector3 position)
        {
            AnimatorState existing = FindState(machine, name);
            if (existing != null)
            {
                return existing;
            }

            return machine.AddState(name, position);
        }

        private static void EnsureAnyStateTrigger(
            AnimatorStateMachine machine,
            AnimatorState destination,
            string triggerName,
            bool canTransitionToSelf)
        {
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions)
            {
                if (transition.destinationState == destination
                    && transition.conditions.Any(c => c.parameter == triggerName))
                {
                    return;
                }
            }

            AnimatorStateTransition created = machine.AddAnyStateTransition(destination);
            created.hasExitTime = false;
            created.hasFixedDuration = true;
            created.duration = 0.05f;
            created.canTransitionToSelf = canTransitionToSelf;
            created.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
        }

        private static void EnsureExitToMovement(
            AnimatorState from,
            AnimatorState movement,
            bool hasExitTime,
            float exitTime,
            float duration)
        {
            foreach (AnimatorStateTransition transition in from.transitions)
            {
                if (transition.destinationState == movement)
                {
                    return;
                }
            }

            AnimatorStateTransition created = from.AddTransition(movement);
            created.hasExitTime = hasExitTime;
            created.exitTime = exitTime;
            created.hasFixedDuration = true;
            created.duration = duration;
        }
    }
}
