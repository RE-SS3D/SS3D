using NUnit.Framework;
using SS3D.Systems.Audio;

namespace EditorTests
{
    public class AudioOcclusionStateTests
    {
        [Test]
        public void LerpCutoffHz_Clear_MovesTowardClearCutoff()
        {
            float result = AudioOcclusionState.LerpCutoffHz(AudioOcclusionState.OccludedCutoffHz, false, 0.5f);
            Assert.Greater(result, AudioOcclusionState.OccludedCutoffHz);
            Assert.LessOrEqual(result, AudioOcclusionState.ClearCutoffHz);
        }

        [Test]
        public void LerpCutoffHz_Occluded_MovesTowardOccludedCutoff()
        {
            float result = AudioOcclusionState.LerpCutoffHz(AudioOcclusionState.ClearCutoffHz, true, 0.5f);
            Assert.Less(result, AudioOcclusionState.ClearCutoffHz);
            Assert.GreaterOrEqual(result, AudioOcclusionState.OccludedCutoffHz);
        }

        [Test]
        public void LerpCutoffHz_FullLerpFactor_SnapsExactlyToTarget()
        {
            float clear = AudioOcclusionState.LerpCutoffHz(0f, false, 1f);
            float occluded = AudioOcclusionState.LerpCutoffHz(AudioOcclusionState.ClearCutoffHz, true, 1f);
            Assert.AreEqual(AudioOcclusionState.ClearCutoffHz, clear, 0.001f);
            Assert.AreEqual(AudioOcclusionState.OccludedCutoffHz, occluded, 0.001f);
        }

        [Test]
        public void LerpVolumeScale_Occluded_MovesTowardOccludedScale()
        {
            float result = AudioOcclusionState.LerpVolumeScale(1f, true, 1f);
            Assert.AreEqual(AudioOcclusionState.OccludedVolumeScale, result, 0.001f);
        }

        [Test]
        public void LerpVolumeScale_Clear_ReturnsToFullScale()
        {
            float result = AudioOcclusionState.LerpVolumeScale(AudioOcclusionState.OccludedVolumeScale, false, 1f);
            Assert.AreEqual(1f, result, 0.001f);
        }
    }
}
