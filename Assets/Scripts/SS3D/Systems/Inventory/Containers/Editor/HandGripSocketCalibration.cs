#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Bakes a <see cref="GripSocket"/> onto every <see cref="ContainerItemDisplay"/> display point of
    /// <c>Human.prefab</c> (both hands, the torso). It poses an instance of the prefab in the Animator's
    /// default state (idle, time 0), measures each display against the character root, and writes the
    /// result into the prefab. Held items then grip at a constant hand-local rotation instead of whatever
    /// the hand's pose was at pickup. Documents/plans/item-orientation.plan.md Phase A.
    /// </summary>
    /// <remarks>
    /// Re-run after changing the idle clip, the avatar, the hand display transforms, or the default state of
    /// the Human Animator. Registered on <c>HumanPrefabRecipes</c>.
    /// </remarks>
    public static class HandGripSocketCalibration
    {
        public const string HumanPrefabPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";

        public const string ReferencePoseName = "Human Animator default state at t=0 (idle)";

        /// <summary>A hand display must turn at least this far from the bind pose, or the rig was not posed.</summary>
        private const float MinPosedAngleDegrees = 1f;

        [MenuItem("SS3D/Inventory/Calibrate Hand Grip Sockets")]
        public static void CalibrateMenu()
        {
            bool ok = Calibrate(out string report);
            EditorUtility.DisplayDialog("Calibrate Hand Grip Sockets", report, "OK");
            if (!ok)
            {
                Debug.LogError("[HandGripSocketCalibration] " + report);
            }
        }

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Inventory.Containers.Editor.HandGripSocketCalibration.CalibrateBatch</c></summary>
        public static void CalibrateBatch()
        {
            bool ok = Calibrate(out string report);
            if (ok)
            {
                Debug.Log("[HandGripSocketCalibration] " + report);
            }
            else
            {
                Debug.LogError("[HandGripSocketCalibration] " + report);
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(ok ? 0 : 1);
            }
        }

        /// <summary>Measure and write every socket. Returns false (and writes nothing) if the rig could not be posed.</summary>
        public static bool Calibrate(out string report)
        {
            if (!TryMeasure(out Dictionary<string, Quaternion> references, out report))
            {
                return false;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HumanPrefabPath);
            if (prefabRoot == null)
            {
                report = $"Missing prefab: {HumanPrefabPath}";
                return false;
            }

            try
            {
                StringBuilder lines = new();
                int changed = 0;
                foreach (KeyValuePair<string, Quaternion> entry in references)
                {
                    Transform display = FindByIndexPath(prefabRoot.transform, entry.Key);
                    if (display == null)
                    {
                        report = $"Display at index path {entry.Key} not found in the prefab contents; nothing written.";
                        return false;
                    }

                    if (!display.TryGetComponent(out GripSocket socket))
                    {
                        socket = display.gameObject.AddComponent<GripSocket>();
                    }

                    if (socket.IsCalibrated
                        && Quaternion.Angle(socket.ReferenceRotation, entry.Value) < 0.01f
                        && socket.ReferencePose == ReferencePoseName)
                    {
                        lines.AppendLine($"{display.name}: unchanged");
                        continue;
                    }

                    SerializedObject serialized = new(socket);
                    serialized.FindProperty("_referenceRotation").quaternionValue = entry.Value;
                    serialized.FindProperty("_calibrated").boolValue = true;
                    serialized.FindProperty("_referencePose").stringValue = ReferencePoseName;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    changed++;
                    lines.AppendLine($"{display.name}: {entry.Value.eulerAngles}");
                }

                if (changed > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, HumanPrefabPath);
                }

                report = $"Calibrated {references.Count} display(s), {changed} changed.\n{lines}";
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        /// <summary>
        /// Instance the prefab, pose it in the reference pose, and record each display's reference rotation,
        /// keyed by its sibling-index path from the root (the same in the instance and the prefab contents).
        /// </summary>
        private static bool TryMeasure(out Dictionary<string, Quaternion> references, out string error)
        {
            references = new Dictionary<string, Quaternion>();
            error = null;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath);
            if (prefab == null)
            {
                error = $"Missing prefab: {HumanPrefabPath}";
                return false;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                Animator animator = instance.GetComponent<Animator>();
                if (animator == null || animator.runtimeAnimatorController == null || !animator.isHuman)
                {
                    error = "Human.prefab needs an Animator with a controller and a humanoid avatar.";
                    return false;
                }

                List<Transform> displays = CollectDisplays(instance);
                if (displays.Count == 0)
                {
                    error = "No ContainerItemDisplay display points found on Human.prefab.";
                    return false;
                }

                Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                List<Transform> handDisplays = displays.FindAll(d =>
                    (leftHand != null && d.IsChildOf(leftHand)) || (rightHand != null && d.IsChildOf(rightHand)));
                if (handDisplays.Count == 0)
                {
                    error = "No display point sits under the avatar's hand bones.";
                    return false;
                }

                Dictionary<Transform, Quaternion> bindRotations = new();
                foreach (Transform display in handDisplays)
                {
                    bindRotations[display] = display.rotation;
                }

                Dictionary<string, Quaternion> measured = references;
                void Record()
                {
                    Quaternion rootRotation = instance.transform.rotation;
                    foreach (Transform display in displays)
                    {
                        measured[IndexPath(instance.transform, display)] =
                            HeldItemGrip.ReferenceRotation(display.rotation, rootRotation);
                    }
                }

                if (PoseWithAnimator(animator, handDisplays, bindRotations))
                {
                    Record();
                    return true;
                }

                return PoseWithDefaultStateClip(instance, animator, handDisplays, bindRotations, Record, out error);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>Evaluate the controller's default state at t=0, as the runtime Animator does on spawn.</summary>
        private static bool PoseWithAnimator(
            Animator animator,
            List<Transform> handDisplays,
            Dictionary<Transform, Quaternion> bindRotations)
        {
            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.Update(0f);
            return AllMoved(handDisplays, bindRotations);
        }

        /// <summary>Fallback: sample the default state's motion (the idle child of a blend tree) directly.</summary>
        private static bool PoseWithDefaultStateClip(
            GameObject instance,
            Animator animator,
            List<Transform> handDisplays,
            Dictionary<Transform, Quaternion> bindRotations,
            System.Action record,
            out string error)
        {
            error = null;
            AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
            AnimatorState defaultState = controller != null && controller.layers.Length > 0
                ? controller.layers[0].stateMachine.defaultState
                : null;
            AnimationClip clip = defaultState != null ? IdleClip(defaultState.motion) : null;
            if (clip == null)
            {
                error = "The Animator did not pose the hands, and the default state has no clip to sample.";
                return false;
            }

            // AnimationMode reverts the pose on Stop, so record while sampling is active.
            AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(instance, clip, 0f);
                AnimationMode.EndSampling();

                if (!AllMoved(handDisplays, bindRotations))
                {
                    error = $"Neither the Animator nor sampling '{clip.name}' moved the hands from the bind pose; nothing written.";
                    return false;
                }

                record();
                return true;
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
        }

        /// <summary>The clip nearest zero speed in a (nested) blend tree, or the clip itself.</summary>
        private static AnimationClip IdleClip(Motion motion)
        {
            if (motion is AnimationClip clip)
            {
                return clip;
            }

            if (motion is not BlendTree tree || tree.children.Length == 0)
            {
                return null;
            }

            ChildMotion best = tree.children[0];
            float bestDistance = float.MaxValue;
            foreach (ChildMotion child in tree.children)
            {
                float distance = tree.blendType == BlendTreeType.Simple1D
                    ? Mathf.Abs(child.threshold)
                    : child.position.sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = child;
                }
            }

            return IdleClip(best.motion);
        }

        private static bool AllMoved(List<Transform> displays, Dictionary<Transform, Quaternion> bindRotations)
        {
            foreach (Transform display in displays)
            {
                if (Quaternion.Angle(display.rotation, bindRotations[display]) < MinPosedAngleDegrees)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<Transform> CollectDisplays(GameObject root)
        {
            List<Transform> displays = new();
            foreach (ContainerItemDisplay itemDisplay in root.GetComponentsInChildren<ContainerItemDisplay>(true))
            {
                if (itemDisplay.attachedContainer == null || itemDisplay.attachedContainer.Displays == null)
                {
                    continue;
                }

                foreach (Transform display in itemDisplay.attachedContainer.Displays)
                {
                    if (display != null && !displays.Contains(display))
                    {
                        displays.Add(display);
                    }
                }
            }

            return displays;
        }

        private static string IndexPath(Transform root, Transform target)
        {
            List<string> parts = new();
            for (Transform t = target; t != null && t != root; t = t.parent)
            {
                parts.Add(t.GetSiblingIndex().ToString());
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static Transform FindByIndexPath(Transform root, string path)
        {
            Transform current = root;
            if (string.IsNullOrEmpty(path))
            {
                return current;
            }

            foreach (string part in path.Split('/'))
            {
                int index = int.Parse(part);
                if (index < 0 || index >= current.childCount)
                {
                    return null;
                }

                current = current.GetChild(index);
            }

            return current;
        }
    }
}
#endif
