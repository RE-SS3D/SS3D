using NUnit.Framework;
using SS3D.Systems.Health;

namespace EditorTests
{
    public class HealthPersonalAudioMapperTests
    {
        [Test]
        public void HealthySnapshotProducesNoHeartbeat()
        {
            Assert.AreEqual(0f, HealthPersonalAudioMapper.ComputeHeartbeatIntensity(HealthSnapshot.Default), 0.001f);
        }

        [Test]
        public void DeadSnapshotSilencesHeartbeatEntirely()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.State = HealthState.Dead;
            snapshot.BrainFunctionPercent = 0f;

            Assert.AreEqual(0f, HealthPersonalAudioMapper.ComputeHeartbeatIntensity(snapshot), 0.001f);
        }

        [Test]
        public void CardiacArrestSilencesHeartbeatEvenWhileStateStaysCritical()
        {
            // Mirrors HealthScreenEffectMapperTests.CardiacArrestSetsDyingCriticalToFull's setup —
            // IsCardiacArrest can be true while State reads Critical — but the heartbeat cue means
            // the opposite of the screen vignette: the heart has stopped, so it goes silent.
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.State = HealthState.Critical;
            snapshot.IsCardiacArrest = true;
            snapshot.BrainFunctionPercent = 80f;

            Assert.AreEqual(0f, HealthPersonalAudioMapper.ComputeHeartbeatIntensity(snapshot), 0.001f);
        }

        [Test]
        public void CriticalWithoutArrestRampsWithBrainFunction()
        {
            HealthSnapshot highBrain = HealthSnapshot.Default;
            highBrain.State = HealthState.Critical;
            highBrain.BrainFunctionPercent = 100f;

            HealthSnapshot lowBrain = HealthSnapshot.Default;
            lowBrain.State = HealthState.Critical;
            lowBrain.BrainFunctionPercent = HealthConstants.ConsciousnessBrainFunctionPercent;

            float high = HealthPersonalAudioMapper.ComputeHeartbeatIntensity(highBrain);
            float low = HealthPersonalAudioMapper.ComputeHeartbeatIntensity(lowBrain);

            Assert.AreEqual(0.4f, high, 0.001f);
            Assert.AreEqual(1f, low, 0.001f);
            Assert.Greater(low, high);
        }

        [Test]
        public void ResumingFromCardiacArrestToCriticalProducesNonzeroHeartbeat()
        {
            // Worked example C: defib succeeds -> heartbeat resumes.
            HealthSnapshot resumed = HealthSnapshot.Default;
            resumed.State = HealthState.Critical;
            resumed.IsCardiacArrest = false;
            resumed.BrainFunctionPercent = 60f;

            Assert.Greater(HealthPersonalAudioMapper.ComputeHeartbeatIntensity(resumed), 0f);
        }

        [Test]
        public void HealthySnapshotProducesNoLaboredBreathing()
        {
            Assert.AreEqual(0f, HealthPersonalAudioMapper.ComputeBreathingIntensity(HealthSnapshot.Default), 0.001f);
        }

        [Test]
        public void OxyDebtRampsLaboredBreathing()
        {
            HealthSnapshot soft = HealthSnapshot.Default;
            soft.Pools.OxyDebt = HealthPersonalAudioMapper.BreathingOxyDebtStart;

            HealthSnapshot full = HealthSnapshot.Default;
            full.Pools.OxyDebt = HealthPersonalAudioMapper.BreathingOxyDebtFull;

            Assert.AreEqual(0f, HealthPersonalAudioMapper.ComputeBreathingIntensity(soft), 0.001f);
            Assert.AreEqual(1f, HealthPersonalAudioMapper.ComputeBreathingIntensity(full), 0.001f);
        }

        [Test]
        public void CriticalWithoutArrestProducesLaboredBreathingFloor()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.State = HealthState.Critical;
            snapshot.IsCardiacArrest = false;

            Assert.AreEqual(0.55f, HealthPersonalAudioMapper.ComputeBreathingIntensity(snapshot), 0.001f);
        }

        [Test]
        public void UnconsciousSilencesLaboredBreathing()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.State = HealthState.Critical;
            snapshot.IsConscious = false;
            snapshot.Pools.OxyDebt = 1f;

            Assert.AreEqual(0f, HealthPersonalAudioMapper.ComputeBreathingIntensity(snapshot), 0.001f);
        }
    }
}
