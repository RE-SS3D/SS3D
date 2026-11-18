using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EditorTests
{
    public class QualityTierLightLimitTests
    {
        private const string RendererPath = "Assets/Settings/URP/SS3D_ForwardPlusRenderer.asset";

        // URP's Forward path keeps only each mesh's N most important lights, ranked without regard to which side
        // of the mesh they light. Station fixtures put 13-27 lights in range of every tile, so a lower limit can keep
        // only lights behind a wall, and with no ambient that wall renders black (Low at 2 did). 8 is URP's maximum.
        [Test]
        public void EveryQualityTierKeepsEightLightsPerObject_WhileTheRendererIsForward()
        {
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            Assume.That(renderer.renderingMode, Is.EqualTo(RenderingMode.Forward), "Forward+ has no per-object limit");

            for (int i = 0; i < QualitySettings.count; i++)
            {
                var asset = (UniversalRenderPipelineAsset)QualitySettings.GetRenderPipelineAssetAt(i);
                Assert.That(asset.maxAdditionalLightsCount, Is.EqualTo(8), QualitySettings.names[i]);
            }
        }
    }
}
