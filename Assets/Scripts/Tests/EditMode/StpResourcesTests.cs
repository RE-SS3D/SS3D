using NUnit.Framework;
using SS3D.Systems.Settings;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EditorTests
{
    public class StpResourcesTests
    {
        // A build strips STP's compute shaders unless some URP asset selects STP (core RP's STPResourceStripper).
        // The player picks STP at runtime, so a build without them threw in STP.Execute every frame at render
        // scale below 100 % and drew nothing (player report, 2026-11-23).
        [Test]
        public void EveryQualityTierAssetSelectsStp_SoBuildsKeepItsShaders()
        {
            for (int i = 0; i < QualitySettings.count; i++)
            {
                UniversalRenderPipelineAsset asset = QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset;
                Assert.That(asset, Is.Not.Null, $"Quality level {QualitySettings.names[i]} has no URP asset.");
                Assert.That(asset.upscalingFilter, Is.EqualTo(UpscalingFilterSelection.STP),
                    $"{asset.name} must serialize STP; PlayerDisplayApplier overwrites the filter at runtime.");
            }
        }

        // The build-side fallback finds STP.RuntimeResources by reflection. If Unity renames it, the probe returns
        // null and the fallback silently stops protecting builds.
        [Test]
        public void StpResourceProbe_FindsTheResourcesInTheEditor()
        {
            Assert.That(PlayerDisplayApplier.ProbeStpResourcesOrNull(), Is.True);
        }
    }
}
