using System.Linq;
using NUnit.Framework;
using SS3D.Rendering.URP;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EditorTests
{
    public class StationShadingFeatureTests
    {
        private const string RendererPath = "Assets/Settings/URP/SS3D_ForwardPlusRenderer.asset";

        // The station shaders read their lighting from globals this feature publishes. Without it they keep stale
        // values or the HLSL fallback, with no error, so the look must not depend on the AO feature being on.
        [Test]
        public void RendererHasOneActiveStationShadingFeature()
        {
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);

            Assert.That(renderer.rendererFeatures.OfType<StationShadingFeature>().Count(feature => feature.isActive),
                Is.EqualTo(1));
        }

        [Test]
        public void PublishWithoutAVolume_SetsTheStationShadingDefaults()
        {
            StationShadingFeature.Publish(null);

            Assert.That(Shader.GetGlobalVector("_SS3DStationAmbient"), Is.EqualTo(StationShading.DefaultAmbientParams));
            Assert.That(Shader.GetGlobalVector("_SS3DStationLights"), Is.EqualTo(StationShading.DefaultLightParams));
            Assert.That(Shader.GetGlobalVector("_SS3DStationFalloff"), Is.EqualTo(StationShading.DefaultFalloffParams));
        }
    }
}
