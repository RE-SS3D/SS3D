using NUnit.Framework;
using SS3D.Systems.Settings;

namespace EditorTests
{
    public class PlayerAudioMixTests
    {
        [TestCase(1f, 0f)]
        [TestCase(0.5f, -6.0206f)]
        [TestCase(0.1f, -20f)]
        public void SliderLevelMapsToDecibels(float linear, float decibels)
        {
            Assert.That(PlayerAudioMix.ToDecibels(linear), Is.EqualTo(decibels).Within(0.001f));
        }

        [TestCase(0f)]
        [TestCase(0.00001f)]
        public void ZeroAndNearZeroAreSilent(float linear)
        {
            Assert.That(PlayerAudioMix.ToDecibels(linear), Is.EqualTo(-80f));
        }

        [Test]
        public void MuteAllSilencesMaster()
        {
            PlayerAudioSettings audio = new() { Master = 1f, MuteAll = true };

            Assert.That(PlayerAudioMix.MasterDecibels(audio, applicationFocused: true), Is.EqualTo(-80f));
        }

        [Test]
        public void LosingFocusSilencesMasterWhenMuteWhenUnfocusedIsOn()
        {
            PlayerAudioSettings audio = new() { Master = 1f, MuteWhenUnfocused = true };

            Assert.That(PlayerAudioMix.MasterDecibels(audio, applicationFocused: false), Is.EqualTo(-80f));
            Assert.That(PlayerAudioMix.MasterDecibels(audio, applicationFocused: true), Is.EqualTo(0f));
        }

        [Test]
        public void LosingFocusKeepsMasterWhenMuteWhenUnfocusedIsOff()
        {
            PlayerAudioSettings audio = new() { Master = 0.5f, MuteWhenUnfocused = false };

            Assert.That(PlayerAudioMix.MasterDecibels(audio, applicationFocused: false), Is.EqualTo(-6.0206f).Within(0.001f));
        }

        [TestCase(true, true, true, true)]
        [TestCase(false, true, false, true)]
        [TestCase(false, true, true, false)]
        [TestCase(false, false, false, false)]
        public void MutedWhenMuteAllOrUnfocusedWithMuteWhenUnfocused(bool muteAll, bool muteWhenUnfocused, bool focused, bool muted)
        {
            PlayerAudioSettings audio = new() { MuteAll = muteAll, MuteWhenUnfocused = muteWhenUnfocused };

            Assert.That(PlayerAudioMix.IsMuted(audio, focused), Is.EqualTo(muted));
        }
    }
}
