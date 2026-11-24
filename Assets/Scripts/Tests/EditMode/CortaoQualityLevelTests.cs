using System.Linq;
using NUnit.Framework;
using SS3D.Rendering.URP;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using QualityLevelSettings = SS3D.Rendering.URP.CortaoAmbientOcclusionFeature.QualityLevelSettings;
using SampleQuality = SS3D.Rendering.URP.CortaoAmbientOcclusionFeature.SampleQuality;

namespace EditorTests
{
    public class CortaoQualityLevelTests
    {
        private const string RendererPath = "Assets/Settings/URP/SS3D_ForwardPlusRenderer.asset";

        // The feature picks its entry by QualitySettings index, so a level added, removed or reordered in
        // QualitySettings would silently run another tier's AO cost.
        [Test]
        public void RendererHasOneAoEntryPerQualityLevel_InQualitySettingsOrder()
        {
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            CortaoAmbientOcclusionFeature feature =
                renderer.rendererFeatures.OfType<CortaoAmbientOcclusionFeature>().Single();

            string[] levels = feature.QualityLevels.Select(level => level.Level).ToArray();
            Assert.That(levels, Is.EqualTo(QualitySettings.names));
        }

        [Test]
        public void ResolveLevel_PastTheEndUsesTheLastEntry()
        {
            QualityLevelSettings[] levels = CortaoAmbientOcclusionFeature.DefaultQualityLevels();

            Assert.That(CortaoAmbientOcclusionFeature.ResolveLevel(levels, 0).Level, Is.EqualTo("Low"));
            Assert.That(CortaoAmbientOcclusionFeature.ResolveLevel(levels, 9).Level, Is.EqualTo("Ultra"));
            Assert.That(CortaoAmbientOcclusionFeature.ResolveLevel(levels, -1).Level, Is.EqualTo("Low"));
        }

        [Test]
        public void ResolveLevel_WithoutEntriesRunsFullQuality()
        {
            QualityLevelSettings level = CortaoAmbientOcclusionFeature.ResolveLevel(new QualityLevelSettings[0], 2);

            Assert.That(level.Enabled, Is.True);
            Assert.That(level.Samples, Is.EqualTo(SampleQuality.High));
            Assert.That(level.HalfResolution, Is.False);
        }
    }
}
