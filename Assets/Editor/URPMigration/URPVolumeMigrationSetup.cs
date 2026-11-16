#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SS3D.Editor.URPMigration
{
    static class URPVolumeMigrationSetup
    {
        const string VolumeProfilesFolder = "Assets/Settings/URP/VolumeProfiles";
        const string LobbyVolumeProfilePath = VolumeProfilesFolder + "/SS3D_LobbyVolumeProfile.asset";
        const string StartupVolumeProfilePath = VolumeProfilesFolder + "/SS3D_StartupVolumeProfile.asset";
        const string PlayerCameraPrefabPath = "Assets/Content/Systems/Lobby/PlayerCamera.prefab";
        const string IntroObjectsPrefabPath = "Assets/Content/Systems/Intro/Intro Objects.prefab";
        const string TypographyScenePath = "Assets/Content/Scenes/SS3DTypography.unity";
        const string RendererAssetPath = "Assets/Settings/URP/SS3D_ForwardPlusRenderer.asset";
        const string PostProcessLayerGuid = "948f4100a11a5c24981795d21301da5c";

        [InitializeOnLoadMethod]
        static void AutoMigrateOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (!NeedsMigration())
                {
                    return;
                }

                if (Run(silent: true))
                {
                    Debug.Log("URP volume migration completed automatically.");
                }
            };
        }

        public static void MigrateFromMenu()
        {
            if (Run(silent: false))
            {
                Debug.Log("URP volume migration completed.");
            }
        }

        public static bool Run(bool silent = false)
        {
            EnsureFolder(VolumeProfilesFolder);

            var lobbyProfile = CreateOrUpdateLobbyProfile();
            var startupProfile = CreateOrUpdateStartupProfile();
            if (lobbyProfile == null || startupProfile == null)
            {
                return false;
            }

            var migratedAny = false;
            migratedAny |= MigratePrefab(PlayerCameraPrefabPath, lobbyProfile, silent);
            migratedAny |= MigratePrefab(IntroObjectsPrefabPath, startupProfile, silent);
            migratedAny |= MigrateScene(TypographyScenePath, startupProfile, silent);

            EnsureScreenSpaceAmbientOcclusion();

            if (migratedAny)
            {
                RemovePostProcessingDefine();
            }

            AssetDatabase.SaveAssets();
            return migratedAny;
        }

        static bool NeedsMigration()
        {
            return AssetUsesLegacyPostProcessing(PlayerCameraPrefabPath)
                || AssetUsesLegacyPostProcessing(IntroObjectsPrefabPath)
                || AssetUsesLegacyPostProcessing(TypographyScenePath);
        }

        static bool AssetUsesLegacyPostProcessing(string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                return false;
            }

            return File.ReadAllText(assetPath).Contains(PostProcessLayerGuid);
        }

        static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            const string parent = "Assets/Settings/URP";
            AssetDatabase.CreateFolder(parent, "VolumeProfiles");
        }

        static VolumeProfile CreateOrUpdateLobbyProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(LobbyVolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, LobbyVolumeProfilePath);
            }

            ConfigureLobbyProfile(profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static VolumeProfile CreateOrUpdateStartupProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(StartupVolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, StartupVolumeProfilePath);
            }

            ConfigureStartupProfile(profile);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static void ConfigureLobbyProfile(VolumeProfile profile)
        {
            var bloom = GetOrAdd<Bloom>(profile);
            bloom.active = true;
            bloom.threshold.Override(1f);
            bloom.intensity.Override(4.7f);
            bloom.scatter.Override(0.682f);
            bloom.clamp.Override(65472f);

            var tonemapping = GetOrAdd<Tonemapping>(profile);
            tonemapping.active = true;
            tonemapping.mode.Override(TonemappingMode.ACES);

            var colorAdjustments = GetOrAdd<ColorAdjustments>(profile);
            colorAdjustments.active = true;
            colorAdjustments.contrast.Override(-44.2f);
            colorAdjustments.saturation.Override(17f);
            colorAdjustments.colorFilter.Override(new Color(1f, 0.995283f, 0.995283f, 1f));

            var vignette = GetOrAdd<Vignette>(profile);
            vignette.active = true;
            vignette.intensity.Override(0.33f);
            vignette.smoothness.Override(0.448f);
        }

        static void ConfigureStartupProfile(VolumeProfile profile)
        {
            var bloom = GetOrAdd<Bloom>(profile);
            bloom.active = true;
            bloom.threshold.Override(1.33f);
            bloom.intensity.Override(2.25f);
            bloom.scatter.Override(0.7f);

            var vignette = GetOrAdd<Vignette>(profile);
            vignette.active = true;
            vignette.intensity.Override(0.248f);
            vignette.smoothness.Override(0.2f);

            var chromaticAberration = GetOrAdd<ChromaticAberration>(profile);
            chromaticAberration.active = true;
            chromaticAberration.intensity.Override(0.174f);

            var lensDistortion = GetOrAdd<LensDistortion>(profile);
            lensDistortion.active = true;
            lensDistortion.intensity.Override(0.14f);
            lensDistortion.xMultiplier.Override(1f);
            lensDistortion.yMultiplier.Override(0.393f);

            var filmGrain = GetOrAdd<FilmGrain>(profile);
            filmGrain.active = true;
            filmGrain.type.Override(FilmGrainLookup.Medium1);
            filmGrain.intensity.Override(0.23f);
            filmGrain.response.Override(0.3f);
        }

        static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>(false);
            }

            return component;
        }

        static bool MigratePrefab(string prefabPath, VolumeProfile profile, bool silent)
        {
            if (!AssetUsesLegacyPostProcessing(prefabPath))
            {
                return false;
            }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var changed = MigrateCameraHierarchy(root, profile);
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }

                return changed;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static bool MigrateScene(string scenePath, VolumeProfile profile, bool silent)
        {
            if (!AssetUsesLegacyPostProcessing(scenePath))
            {
                return false;
            }

            var previouslyActiveScenePath = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var changed = false;

            foreach (var root in scene.GetRootGameObjects())
            {
                changed |= MigrateCameraHierarchy(root, profile);
            }

            if (changed)
            {
                EditorSceneManager.SaveScene(scene);
            }

            if (!string.IsNullOrEmpty(previouslyActiveScenePath) && previouslyActiveScenePath != scenePath)
            {
                EditorSceneManager.OpenScene(previouslyActiveScenePath, OpenSceneMode.Single);
            }

            return changed;
        }

        static bool MigrateCameraHierarchy(GameObject root, VolumeProfile profile)
        {
            var changed = false;
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            {
                if (!HasLegacyPostProcessing(camera.gameObject))
                {
                    continue;
                }

                changed |= MigrateCameraGameObject(camera.gameObject, profile);
            }

            return changed;
        }

        static bool HasLegacyPostProcessing(GameObject gameObject)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                var typeName = component.GetType().FullName;
                if (typeName == "UnityEngine.Rendering.PostProcessing.PostProcessLayer"
                    || typeName == "UnityEngine.Rendering.PostProcessing.PostProcessVolume")
                {
                    return true;
                }
            }

            return false;
        }

        static bool MigrateCameraGameObject(GameObject cameraObject, VolumeProfile profile)
        {
            if (cameraObject.GetComponent<Camera>() == null)
            {
                return false;
            }

            RemoveLegacyPostProcessing(cameraObject);

            var cameraData = cameraObject.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            }

            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.TemporalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
            EnsurePipelineMsaaDisabledForTaa();
            cameraData.volumeLayerMask = ~0;

            var cameraDataSettings = new SerializedObject(cameraData);
            cameraDataSettings.FindProperty("m_VolumeFrameworkUpdateModeOption").enumValueIndex =
                (int)VolumeFrameworkUpdateMode.EveryFrame;
            cameraDataSettings.ApplyModifiedPropertiesWithoutUndo();

            var taa = cameraData.taaSettings;
            taa.jitterScale = 0.75f;
            taa.baseBlendFactor = 0.95f;
            taa.varianceClampScale = 0.85f;

            var volume = cameraObject.GetComponent<Volume>();
            if (volume == null)
            {
                volume = cameraObject.AddComponent<Volume>();
            }

            volume.isGlobal = true;
            volume.weight = 1f;
            volume.priority = 0f;
            volume.sharedProfile = profile;

            return true;
        }

        static void RemoveLegacyPostProcessing(GameObject root)
        {
            foreach (var component in root.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                var typeName = component.GetType().FullName;
                if (typeName == "UnityEngine.Rendering.PostProcessing.PostProcessLayer"
                    || typeName == "UnityEngine.Rendering.PostProcessing.PostProcessVolume")
                {
                    Object.DestroyImmediate(component);
                }
            }
        }

        static void EnsureScreenSpaceAmbientOcclusion()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (renderer == null)
            {
                Debug.LogWarning($"Could not add SSAO; missing renderer at {RendererAssetPath}");
                return;
            }

            foreach (var feature in renderer.rendererFeatures)
            {
                if (feature is ScreenSpaceAmbientOcclusion existing)
                {
                    ConfigureSsao(existing);
                    EditorUtility.SetDirty(existing);
                    EditorUtility.SetDirty(renderer);
                    return;
                }
            }

            var ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            ssao.name = "ScreenSpaceAmbientOcclusion";
            ConfigureSsao(ssao);

            AssetDatabase.AddObjectToAsset(ssao, renderer);
            renderer.rendererFeatures.Add(ssao);

            EditorUtility.SetDirty(ssao);
            EditorUtility.SetDirty(renderer);
        }

        static void ConfigureSsao(ScreenSpaceAmbientOcclusion ssao)
        {
            var settings = new SerializedObject(ssao);
            var ssaoSettings = settings.FindProperty("m_Settings");
            ssaoSettings.FindPropertyRelative("Intensity").floatValue = 1f;
            ssaoSettings.FindPropertyRelative("Radius").floatValue = 0.15f;
            ssaoSettings.FindPropertyRelative("Samples").enumValueIndex = 1;
            ssaoSettings.FindPropertyRelative("BlurQuality").enumValueIndex = 0;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static void RemovePostProcessingDefine()
        {
            foreach (BuildTargetGroup group in System.Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown)
                {
                    continue;
                }

                try
                {
                    var namedTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                    var defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
                    if (!defines.Contains("UNITY_POST_PROCESSING_STACK_V2"))
                    {
                        continue;
                    }

                    defines = defines
                        .Replace("UNITY_POST_PROCESSING_STACK_V2;", string.Empty)
                        .Replace(";UNITY_POST_PROCESSING_STACK_V2", string.Empty)
                        .Replace("UNITY_POST_PROCESSING_STACK_V2", string.Empty);
                    PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
                }
                catch
                {
                    // Some BuildTargetGroup values are obsolete or unsupported.
                }
            }
        }

        static void EnsurePipelineMsaaDisabledForTaa()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null || pipeline.msaaSampleCount == (int)MsaaQuality.Disabled)
            {
                return;
            }

            pipeline.msaaSampleCount = (int)MsaaQuality.Disabled;
            EditorUtility.SetDirty(pipeline);
        }
    }
}
#endif
