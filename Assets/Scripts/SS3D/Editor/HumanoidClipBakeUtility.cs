using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SS3D.Editor
{
    /// <summary>
    /// Bakes generic transform Human*.anim clips into humanoid muscle curves so they work with HumanAvatar.
    /// </summary>
    public static class HumanoidClipBakeUtility
    {
        private const string HumanPrefabPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";
        private const string HumanFbxPath = "Assets/Art/Models/Entities/Humanoids/Human/Human.fbx";
        private const string HumanClipsFolder = "Assets/Content/WorldObjects/Entities/Humanoids/Human";
        private const float MotionEpsilon = 0.0001f;

        public static void FixHumanClipNamesMenu()
        {
            int fixedCount = FixHumanClipNames(saveAssets: true);
            EditorUtility.DisplayDialog(
                "Fix Human Clip Names",
                $"Renamed {fixedCount} clip(s) to match their filenames.",
                "OK");
        }

        private static int FixHumanClipNames(bool saveAssets)
        {
            string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { HumanClipsFolder });
            int fixedCount = 0;

            foreach (string guid in clipGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".anim", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null)
                {
                    continue;
                }

                string expectedName = Path.GetFileNameWithoutExtension(path);
                if (clip.name == expectedName)
                {
                    continue;
                }

                Undo.RecordObject(clip, "Fix Human Clip Name");
                clip.name = expectedName;
                EditorUtility.SetDirty(clip);
                fixedCount++;
            }

            if (saveAssets && fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            return fixedCount;
        }

        public static void BakeHumanClips()
        {
            if (!EditorUtility.DisplayDialog(
                "Bake Humanoid Clips",
                "This converts generic transform clips to humanoid muscle curves in place.\n\n" +
                "Bake core clips first (HumanWalk, HumanIdle, HumanRun). Then use\n" +
                "'Copy Baked Locomotion To Placeholders' for duplicate placeholder clips.\n\n" +
                "For new animations, prefer importing Mixamo FBX as Humanoid instead of baking.",
                "Bake",
                "Cancel"))
            {
                return;
            }

            FixHumanClipNames(saveAssets: true);

            Avatar avatar = LoadHumanAvatar();
            if (avatar == null)
            {
                EditorUtility.DisplayDialog(
                    "Bake Humanoid Clips",
                    $"Could not find a Humanoid Avatar on {HumanFbxPath}.",
                    "OK");
                return;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Bake Humanoid Clips", $"Could not load {HumanPrefabPath}.", "OK");
                return;
            }

            Scene bakeScene = EditorSceneManager.GetActiveScene();
            GameObject instance = null;
            GameObject previousSelection = Selection.activeGameObject;

            try
            {
                instance = PrefabUtility.InstantiatePrefab(prefab, bakeScene) as GameObject;
                if (instance == null)
                {
                    EditorUtility.DisplayDialog("Bake Humanoid Clips", "Could not instantiate Human prefab.", "OK");
                    return;
                }

                instance.hideFlags = HideFlags.HideAndDontSave;
                instance.name = "HumanClipBakeTemp";
                Selection.activeGameObject = instance;

                if (!TryPrepareBakeInstance(instance, out Transform skeletonRoot))
                {
                    EditorUtility.DisplayDialog(
                        "Bake Humanoid Clips",
                        "Could not find Armature on Human prefab.",
                        "OK");
                    return;
                }

                string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { HumanClipsFolder });
                int bakedCount = 0;
                int skippedCount = 0;
                int failedCount = 0;

                foreach (string guid in clipGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".anim", System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                    if (clip == null)
                    {
                        continue;
                    }

                    string expectedName = Path.GetFileNameWithoutExtension(path);
                    if (clip.name != expectedName)
                    {
                        Debug.LogWarning($"Skipping {path}: clip name '{clip.name}' does not match filename. Run Fix Human Clip Names first.");
                        skippedCount++;
                        continue;
                    }

                    if (!IsGenericTransformClip(clip))
                    {
                        skippedCount++;
                        continue;
                    }

                    Undo.RecordObject(clip, "Bake Humanoid Clip");

                    Animator animator = instance.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.Rebind();
                        animator.Update(0f);
                    }

                    if (TryBakeClipInPlace(instance, avatar, clip))
                    {
                        EditorUtility.SetDirty(clip);
                        AssetDatabase.SaveAssetIfDirty(clip);
                        bakedCount++;
                    }
                    else
                    {
                        failedCount++;
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Bake Humanoid Clips",
                    $"Baked {bakedCount} clip(s).\nSkipped {skippedCount} clip(s).\nFailed {failedCount} clip(s).\n\n" +
                    "Next: SS3D → Animation → Assign Humanoid Avatar On Human Prefab",
                    "OK");
            }
            finally
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }

                Selection.activeGameObject = previousSelection;
            }
        }

        public static void CopyBakedLocomotionToPlaceholders()
        {
            string folder = HumanClipsFolder;
            int copiedCount = 0;

            copiedCount += CopyBakedClip(folder, "HumanWalk", new[]
            {
                "HumanRun",
                "HumanCrawl",
                "HumanLimpLeft",
                "HumanLimpRight",
                "HumanAttackStab",
                "HumanAttackSwing",
                "HumanHoldItem",
                "HumanHoldWeapon",
                "HumanThrow",
            });

            copiedCount += CopyBakedClip(folder, "HumanIdle", new[]
            {
                "HumanHoldDefault",
                "HumanSit",
                "HumanStandUp",
                "HumanEmote",
                "HumanFlinch",
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Copy Baked Locomotion To Placeholders",
                $"Copied baked clip data into {copiedCount} placeholder clip(s).\n\n" +
                "These are temporary stand-ins until real Mixamo clips are imported.",
                "OK");
        }

        private static int CopyBakedClip(string folder, string sourceClipName, string[] destinationClipNames)
        {
            string sourcePath = $"{folder}/{sourceClipName}.anim";
            AnimationClip sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(sourcePath);
            if (sourceClip == null)
            {
                Debug.LogWarning($"Could not load source clip {sourcePath}.");
                return 0;
            }

            if (IsGenericTransformClip(sourceClip))
            {
                Debug.LogWarning(
                    $"{sourceClipName} is still a generic clip. Bake it to humanoid first, then run this copy step.");
                return 0;
            }

            int copiedCount = 0;
            foreach (string destinationClipName in destinationClipNames)
            {
                string destinationPath = $"{folder}/{destinationClipName}.anim";
                AnimationClip destinationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(destinationPath);
                if (destinationClip == null)
                {
                    Debug.LogWarning($"Could not load destination clip {destinationPath}.");
                    continue;
                }

                Undo.RecordObject(destinationClip, "Copy Baked Locomotion Clip");
                EditorUtility.CopySerialized(sourceClip, destinationClip);
                destinationClip.name = destinationClipName;
                EditorUtility.SetDirty(destinationClip);
                copiedCount++;
            }

            return copiedCount;
        }

        public static void AssignHumanoidAvatar()
        {
            Avatar avatar = LoadHumanAvatar();
            if (avatar == null)
            {
                EditorUtility.DisplayDialog(
                    "Assign Humanoid Avatar",
                    $"Could not find a Humanoid Avatar on {HumanFbxPath}.",
                    "OK");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HumanPrefabPath);
            try
            {
                Animator animator = prefabRoot.GetComponent<Animator>();
                if (animator == null)
                {
                    EditorUtility.DisplayDialog("Assign Humanoid Avatar", "Human prefab has no Animator.", "OK");
                    return;
                }

                animator.avatar = avatar;
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, HumanPrefabPath);
                EditorUtility.DisplayDialog(
                    "Assign Humanoid Avatar",
                    "HumanAvatar assigned on Human.prefab. Enter Play mode to verify locomotion.",
                    "OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        public static void ClearHumanoidAvatar()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(HumanPrefabPath);
            try
            {
                Animator animator = prefabRoot.GetComponent<Animator>();
                if (animator == null)
                {
                    EditorUtility.DisplayDialog("Clear Humanoid Avatar", "Human prefab has no Animator.", "OK");
                    return;
                }

                animator.avatar = null;
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, HumanPrefabPath);
                EditorUtility.DisplayDialog(
                    "Clear Humanoid Avatar",
                    "Avatar cleared. Use this with generic transform clips.",
                    "OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static Avatar LoadHumanAvatar()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(HumanFbxPath);
            foreach (Object asset in assets)
            {
                if (asset is Avatar humanoidAvatar && humanoidAvatar.isHuman)
                {
                    return humanoidAvatar;
                }
            }

            return null;
        }

        private static bool TryPrepareBakeInstance(GameObject instance, out Transform skeletonRoot)
        {
            skeletonRoot = instance.transform.Find("HumanArmature");
            if (skeletonRoot == null)
            {
                skeletonRoot = instance.transform.Find("Armature");
            }
            if (skeletonRoot == null)
            {
                return false;
            }

            Animator animator = instance.GetComponent<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = null;
                animator.avatar = null;
                animator.Rebind();
                animator.Update(0f);
            }

            foreach (Behaviour behaviour in instance.GetComponentsInChildren<Behaviour>(true))
            {
                if (behaviour != null && behaviour.GetType().Name == "Ragdoll")
                {
                    behaviour.enabled = false;
                }
            }

            foreach (Rigidbody rigidbody in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                rigidbody.isKinematic = true;
            }

            return true;
        }

        private static bool IsGenericTransformClip(AnimationClip clip)
        {
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.path.StartsWith("Armature/", System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryBakeClipInPlace(
            GameObject root,
            Avatar avatar,
            AnimationClip clip)
        {
            AnimationClip sampleClip = Object.Instantiate(clip);
            sampleClip.name = clip.name;

            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            string clipName = clip.name;
            float frameRate = clip.frameRate > 0f ? clip.frameRate : 30f;
            float length = Mathf.Max(sampleClip.length, 1f / frameRate);
            int frameCount = Mathf.Max(1, Mathf.CeilToInt(length * frameRate));

            HumanPoseHandler handler = new HumanPoseHandler(avatar, root.transform);
            HumanPose pose = new HumanPose
            {
                muscles = new float[HumanTrait.MuscleCount],
            };

            string[] muscleNames = HumanTrait.MuscleName;
            Dictionary<string, AnimationCurve> muscleCurves = new(muscleNames.Length);
            for (int i = 0; i < muscleNames.Length; i++)
            {
                muscleCurves[muscleNames[i]] = new AnimationCurve();
            }

            AnimationCurve rootTx = new();
            AnimationCurve rootTy = new();
            AnimationCurve rootTz = new();
            AnimationCurve rootQx = new();
            AnimationCurve rootQy = new();
            AnimationCurve rootQz = new();
            AnimationCurve rootQw = new();

            bool sourceAnimatesBones = ClipHasAnimatedCurves(clip);

            try
            {
                AnimationMode.StartAnimationMode();
                try
                {
                    for (int frame = 0; frame <= frameCount; frame++)
                    {
                        float time = Mathf.Min(frame / frameRate, length);
                        SampleClipAtTime(root, sampleClip, time);
                        handler.GetHumanPose(ref pose);

                        AddKey(rootTx, time, pose.bodyPosition.x);
                        AddKey(rootTy, time, pose.bodyPosition.y);
                        AddKey(rootTz, time, pose.bodyPosition.z);
                        AddKey(rootQx, time, pose.bodyRotation.x);
                        AddKey(rootQy, time, pose.bodyRotation.y);
                        AddKey(rootQz, time, pose.bodyRotation.z);
                        AddKey(rootQw, time, pose.bodyRotation.w);

                        for (int muscleIndex = 0; muscleIndex < pose.muscles.Length; muscleIndex++)
                        {
                            AddKey(muscleCurves[muscleNames[muscleIndex]], time, pose.muscles[muscleIndex]);
                        }
                    }
                }
                finally
                {
                    AnimationMode.StopAnimationMode();
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Bake failed for '{clipName}': {exception.Message}");
                return false;
            }
            finally
            {
                handler.Dispose();
                Object.DestroyImmediate(sampleClip);
            }

            if (BakedCurvesContainNaN(muscleCurves, rootTx, rootTy, rootTz, rootQx, rootQy, rootQz, rootQw))
            {
                Debug.LogError(
                    $"Bake for '{clipName}' produced invalid humanoid curves (NaN). Clip left unchanged.");
                return false;
            }

            if (!BakedClipHasMotion(muscleCurves, rootTx, rootTy, rootTz, rootQx, rootQy, rootQz, rootQw)
                && sourceAnimatesBones)
            {
                Debug.LogWarning(
                    $"Bake for '{clipName}' produced static humanoid curves even though the source clip animates bones. " +
                    "Clip left unchanged. If this is a placeholder duplicate of HumanWalk/HumanIdle, bake the source clip " +
                    "and run SS3D → Animation → Copy Baked Locomotion To Placeholders.");
                return false;
            }

            ClearCurves(clip);

            SetAnimatorCurve(clip, "RootT.x", rootTx);
            SetAnimatorCurve(clip, "RootT.y", rootTy);
            SetAnimatorCurve(clip, "RootT.z", rootTz);
            SetAnimatorCurve(clip, "RootQ.x", rootQx);
            SetAnimatorCurve(clip, "RootQ.y", rootQy);
            SetAnimatorCurve(clip, "RootQ.z", rootQz);
            SetAnimatorCurve(clip, "RootQ.w", rootQw);

            for (int i = 0; i < muscleNames.Length; i++)
            {
                SetAnimatorCurve(clip, muscleNames[i], muscleCurves[muscleNames[i]]);
            }

            clip.name = clipName;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AnimationUtility.SetAnimationEvents(clip, events);
            clip.frameRate = frameRate;
            return true;
        }

        private static bool ClipHasAnimatedCurves(AnimationClip clip)
        {
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (!binding.path.StartsWith("Armature/", System.StringComparison.Ordinal))
                {
                    continue;
                }

                AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (CurveHasVariation(curve))
                {
                    return true;
                }
            }

            return false;
        }

        private static void SampleClipAtTime(GameObject root, AnimationClip sampleClip, float time)
        {
            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(root, sampleClip, time);
                AnimationMode.EndSampling();
                return;
            }

            sampleClip.SampleAnimation(root, time);
        }

        private static bool BakedCurvesContainNaN(
            Dictionary<string, AnimationCurve> muscleCurves,
            params AnimationCurve[] rootCurves)
        {
            foreach (AnimationCurve curve in rootCurves)
            {
                if (CurveContainsNaN(curve))
                {
                    return true;
                }
            }

            foreach (AnimationCurve curve in muscleCurves.Values)
            {
                if (CurveContainsNaN(curve))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CurveContainsNaN(AnimationCurve curve)
        {
            if (curve == null)
            {
                return false;
            }

            foreach (Keyframe key in curve.keys)
            {
                if (float.IsNaN(key.value) || float.IsNaN(key.inTangent) || float.IsNaN(key.outTangent))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool BakedClipHasMotion(
            Dictionary<string, AnimationCurve> muscleCurves,
            params AnimationCurve[] rootCurves)
        {
            foreach (AnimationCurve curve in rootCurves)
            {
                if (CurveHasVariation(curve))
                {
                    return true;
                }
            }

            foreach (AnimationCurve curve in muscleCurves.Values)
            {
                if (CurveHasVariation(curve))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CurveHasVariation(AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
            {
                return false;
            }

            float first = curve.keys[0].value;
            for (int i = 1; i < curve.length; i++)
            {
                if (Mathf.Abs(curve.keys[i].value - first) > MotionEpsilon)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ClearCurves(AnimationClip clip)
        {
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }

            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
            }
        }

        private static void SetAnimatorCurve(AnimationClip clip, string propertyName, AnimationCurve curve)
        {
            clip.SetCurve(string.Empty, typeof(Animator), propertyName, curve);
        }

        private static void AddKey(AnimationCurve curve, float time, float value)
        {
            curve.AddKey(time, value);
        }
    }
}
