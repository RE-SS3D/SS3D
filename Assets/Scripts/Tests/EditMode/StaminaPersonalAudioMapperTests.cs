using NUnit.Framework;
using SS3D.Systems.Stamina;

namespace EditorTests
{
    public class StaminaPersonalAudioMapperTests
    {
        [Test]
        public void FullStaminaProducesNoBreathing()
        {
            Assert.AreEqual(0f, StaminaPersonalAudioMapper.ComputeBreathingIntensity(1f), 0.001f);
        }

        [Test]
        public void AboveThresholdProducesNoBreathing()
        {
            Assert.AreEqual(0f, StaminaPersonalAudioMapper.ComputeBreathingIntensity(StaminaPersonalAudioMapper.AudibleThreshold), 0.001f);
        }

        [Test]
        public void EmptyStaminaStopsAtTheExertionCap()
        {
            Assert.AreEqual(StaminaPersonalAudioMapper.MaxIntensity, StaminaPersonalAudioMapper.ComputeBreathingIntensity(0f), 0.001f);
        }

        [Test]
        public void BreathingRampsBetweenThresholdAndEmpty()
        {
            float half = StaminaPersonalAudioMapper.ComputeBreathingIntensity(StaminaPersonalAudioMapper.AudibleThreshold * 0.5f);
            Assert.Greater(half, 0f);
            Assert.Less(half, StaminaPersonalAudioMapper.MaxIntensity);
        }
    }
}
