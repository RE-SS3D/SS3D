using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Entities.Humanoid.Body;
using SS3D.Systems.Health;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SS3D.Editor
{
    public static partial class HumanoidLocomotionBlendSetup
    {
        private const string DownedCrawlTreeName = "Downed Crawl 1D";
        private const string LayingBreathlessPath = "Assets/Art/Animations/Misc/X Bot@Laying Breathless.fbx";

        /// <summary>
        /// Downed on Full Body Override (<see cref="DownedAnimation"/>): a fall state per gait, the belly-down Crawl blend
        /// (still pose to Zombie Crawl on Speed, calibrated to the Downed crawl speed) and the face-up lying state.
        /// <see cref="Ragdoll"/> plays them by name, so Any State no longer enters Crawl on IsCrawling: that transition
        /// would cut a fall short. Each state leaves for Empty Override when IsCrawling clears.
        /// </summary>
        public static string WireDownedStates(AnimatorController controller, float humanScale)
        {
            if (controller.layers.Length <= DownedAnimation.Layer)
            {
                return "ERROR: no Full Body Override layer";
            }

            ConfigureDownedImports();
            AnimatorStateMachine machine = controller.layers[DownedAnimation.Layer].stateMachine;
            AnimatorState empty = FindState(machine, "Empty Override");
            foreach (AnimatorStateTransition any in machine.anyStateTransitions
                         .Where(t => t.destinationState != null && t.destinationState.name == DownedAnimation.CrawlState).ToArray())
            {
                machine.RemoveAnyStateTransition(any);
            }

            foreach (Object subAsset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
            {
                if (subAsset is BlendTree old && old.name == DownedCrawlTreeName)
                {
                    AssetDatabase.RemoveObjectFromAsset(old);
                    Object.DestroyImmediate(old, true);
                }
            }

            (float crawlSpeed, float walkValue) = ReadDownedCrawlSpeed();
            BlendTree crawlTree = new BlendTree
            {
                name = DownedCrawlTreeName,
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false,
            };
            AnimationClip still = LoadPackClip($"{AnimationsRoot}/{DownedAnimation.Still.File}", DownedAnimation.Still.ClipName);
            AnimationClip crawl = LoadPackClip($"{AnimationsRoot}/{DownedAnimation.Crawl.File}", DownedAnimation.Crawl.ClipName);
            crawlTree.AddChild(still, 0f);
            // Downed always walks (CanSprint is off), so the crawl sits at the walk Speed value.
            crawlTree.AddChild(crawl, walkValue);
            ChildMotion[] children = crawlTree.children;
            float clipSpeed = new Vector2(crawl.averageSpeed.x, crawl.averageSpeed.z).magnitude * humanScale;
            children[1].timeScale = Mathf.Clamp(crawlSpeed / Mathf.Max(clipSpeed, 0.05f), MinCalibratedTimeScale, MaxCalibratedTimeScale);
            crawlTree.children = children;
            AssetDatabase.AddObjectToAsset(crawlTree, controller);

            var log = new List<string>();
            AnimatorState crawlState = FindOrCreateState(machine, DownedAnimation.CrawlState, new Vector3(300, 200, 0));
            ConfigureDownedState(crawlState, crawlTree, empty);
            log.Add($"{DownedAnimation.CrawlState}: crawl {clipSpeed:F2} m/s -> {crawlSpeed:F2} m/s, timeScale {children[1].timeScale:F2}");

            AnimatorState faceUp = FindOrCreateState(machine, DownedAnimation.FaceUpState, new Vector3(300, 260, 0));
            ConfigureDownedState(faceUp, LoadPackClip(LayingBreathlessPath, "Mix_Laying Breathless"), empty);

            for (int index = 0; index < DownedAnimation.Falls.Length; index++)
            {
                DownedAnimation.Clip fall = DownedAnimation.Falls[index];
                AnimatorState state = FindOrCreateState(machine, fall.StateName, new Vector3(550, 200 + 60 * index, 0));
                ConfigureDownedState(state, LoadPackClip($"{AnimationsRoot}/{fall.File}", fall.ClipName), empty);
                log.Add($"{fall.StateName}: {fall.ClipName} frames {fall.FirstFrame}-{fall.LastFrame}");
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return "OK: Downed states. " + string.Join("; ", log);
        }

        private static void ConfigureDownedState(AnimatorState state, Motion motion, AnimatorState empty)
        {
            state.motion = motion;
            state.speed = 1f;
            state.speedParameterActive = false;
            state.writeDefaultValues = false;
            state.iKOnFeet = false;
            ClearTransitions(state);
            if (empty != null)
            {
                AnimatorStateTransition exit = state.AddTransition(empty);
                exit.hasExitTime = false;
                exit.duration = 0.2f;
                exit.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrawling");
            }
        }

        /// <summary>World speed of a Downed crawl: the walk speed times <see cref="HealthConstants.DownedCrawlSpeedMultiplier"/>.</summary>
        /// <returns>The crawl's world speed and the animator Speed value it plays at.</returns>
        private static (float WorldSpeed, float WalkValue) ReadDownedCrawlSpeed()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath);
            HumanoidLivingController living = prefab.GetComponent<HumanoidLivingController>();
            float movementSpeed = new SerializedObject(living).FindProperty("_movementSpeed").floatValue;
            return (movementSpeed * living.WalkAnimatorValue * HealthConstants.DownedCrawlSpeedMultiplier, living.WalkAnimatorValue);
        }

        /// <summary>
        /// Humanoid import for the Downed clips (<see cref="DownedAnimation"/>). All bake rotation and height but keep
        /// their travel out of the pose: <see cref="Ragdoll"/> moves the controller by a fall's root motion, so a fall
        /// stops at walls, and the controller moves a crawling body. The still pose stays centred on the root with them. Laying Breathless takes its
        /// height from the feet: its Mixamo source lies on a bed about 0.9 m up.
        /// </summary>
        private static void ConfigureDownedImports()
        {
            Avatar xBot = AssetDatabase.LoadAllAssetsAtPath(XBotAvatarPath).OfType<Avatar>().FirstOrDefault();
            var clips = DownedAnimation.Falls.Concat(new[] { DownedAnimation.Still, DownedAnimation.Crawl });
            foreach (IGrouping<string, DownedAnimation.Clip> file in clips.GroupBy(clip => clip.File))
            {
                string path = $"{AnimationsRoot}/{file.Key}";
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                {
                    Debug.LogError($"[HumanoidLocomotionBlendSetup] Missing FBX {path}");
                    continue;
                }

                string takeName = importer.defaultClipAnimations[0].takeName;
                ModelImporterClipAnimation[] imports = file.Select(clip => new ModelImporterClipAnimation
                {
                    name = clip.ClipName,
                    takeName = takeName,
                    firstFrame = clip.FirstFrame,
                    lastFrame = clip.LastFrame,
                    loopTime = clip.Loop,
                    lockRootRotation = true,
                    lockRootHeightY = true,
                    lockRootPositionXZ = false,
                    keepOriginalOrientation = true,
                    keepOriginalPositionY = true,
                    keepOriginalPositionXZ = true,
                }).ToArray();
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = xBot;
                importer.importAnimation = true;
                importer.clipAnimations = imports;
                importer.SaveAndReimport();
            }

            if (AssetImporter.GetAtPath(LayingBreathlessPath) is ModelImporter laying)
            {
                ModelImporterClipAnimation[] layingClips = laying.clipAnimations;
                ModelImporterClipAnimation clip = layingClips.Length > 0 ? layingClips[0] : laying.defaultClipAnimations[0];
                clip.loopTime = true;
                clip.lockRootRotation = clip.lockRootHeightY = clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = clip.keepOriginalPositionXZ = true;
                clip.keepOriginalPositionY = false;
                clip.heightFromFeet = true;
                laying.clipAnimations = new[] { clip };
                laying.SaveAndReimport();
            }
        }
    }
}
