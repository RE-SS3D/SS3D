#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SS3D.Editor.URPMigration
{
    /// <summary>
    /// Validates and wires Phase 1 URP foundation assets created under Assets/Settings/URP/.
    /// Run via: Unity -batchmode -executeMethod SS3D.Editor.URPMigration.URPFoundationSetup.Execute
    /// </summary>
    public static class URPFoundationSetup
    {
        const string PipelineAssetPath = "Assets/Settings/URP/SS3D_URPAsset.asset";
        const string RendererAssetPath = "Assets/Settings/URP/SS3D_ForwardPlusRenderer.asset";

        public static void Execute()
        {
            EditorApplication.Exit(Run() ? 0 : 1);
        }

        public static void SetupFromMenu()
        {
            if (Run())
            {
                Debug.Log("URP Foundation setup completed successfully.");
            }
        }

        static bool Run()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);

            if (pipeline == null || renderer == null)
            {
                Debug.LogError(
                    "Missing URP foundation assets. Expected:\n" +
                    $"  {PipelineAssetPath}\n" +
                    $"  {RendererAssetPath}");
                return false;
            }

            ApplySerializedSettings(renderer, pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            GraphicsSettings.lightsUseLinearIntensity = true;
            GraphicsSettings.lightsUseColorTemperature = true;

            AssignPipelineToAllQualityLevels(pipeline);
            ApplyGraphicsProjectSettings();

            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
            }

            SelectionPickRendererFeatureSetup.EnsureWired();

            AssetDatabase.SaveAssets();
            return true;
        }

        static void ApplySerializedSettings(
            UniversalRendererData renderer,
            UniversalRenderPipelineAsset pipeline)
        {
            var rendererSettings = new SerializedObject(renderer);
            rendererSettings.FindProperty("m_RenderingMode").intValue = (int)RenderingMode.ForwardPlus;
            rendererSettings.ApplyModifiedPropertiesWithoutUndo();

            var pipelineSettings = new SerializedObject(pipeline);
            pipelineSettings.FindProperty("m_RequireDepthTexture").boolValue = true;
            pipelineSettings.FindProperty("m_RequireOpaqueTexture").boolValue = true;
            // Disabled: Linux OpenGL / some editor APIs do not support BatchBufferTarget.RawBuffer
            // and spam "GPUResidentDrawer The current platform does not support…" on every rebuild.
            pipelineSettings.FindProperty("m_GPUResidentDrawerMode").intValue = 0;
            pipelineSettings.FindProperty("m_UseSRPBatcher").boolValue = true;
            pipelineSettings.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(pipeline);
        }

        static void ApplyGraphicsProjectSettings()
        {
            var graphicsSettings = Unsupported.GetSerializedAssetInterfaceSingleton("GraphicsSettings");
            var settings = new SerializedObject(graphicsSettings);

            var brgStripping = settings.FindProperty("m_BrgStripping");
            if (brgStripping != null)
            {
                brgStripping.intValue = 2;
            }

            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignPipelineToAllQualityLevels(UniversalRenderPipelineAsset pipeline)
        {
            var originalQuality = QualitySettings.GetQualityLevel();

            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipeline;
            }

            QualitySettings.SetQualityLevel(originalQuality, applyExpensiveChanges: false);
        }
    }
}
#endif
