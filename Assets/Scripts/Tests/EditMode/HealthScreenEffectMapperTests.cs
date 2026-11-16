using NUnit.Framework;
using SS3D.Systems.Health;
using UnityEngine;

namespace EditorTests
{
    public class HealthScreenEffectMapperTests
    {
        [Test]
        public void HealthySnapshotProducesZeroIntensities()
        {
            HealthScreenEffectMapper.Intensities intensities =
                HealthScreenEffectMapper.Compute(HealthSnapshot.Default);

            Assert.AreEqual(0f, intensities.DyingCritical, 0.001f);
            Assert.AreEqual(0f, intensities.BloodLossTunnelVision, 0.001f);
            Assert.AreEqual(0f, intensities.LowOxygen, 0.001f);
            Assert.AreEqual(0f, intensities.Concussion, 0.001f);
            Assert.AreEqual(0f, intensities.Unconscious, 0.001f);
        }

        [Test]
        public void DeadSnapshotClearsAllIntensities()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.State = HealthState.Dead;
            snapshot.IsConscious = false;
            snapshot.IsCardiacArrest = true;
            snapshot.Pools.BloodVolumeRatio = 0.1f;
            snapshot.Pools.OxyDebt = 1f;
            snapshot.BrainFunctionPercent = 0f;

            HealthScreenEffectMapper.Intensities intensities =
                HealthScreenEffectMapper.Compute(snapshot);

            Assert.AreEqual(0f, intensities.DyingCritical, 0.001f);
            Assert.AreEqual(0f, intensities.BloodLossTunnelVision, 0.001f);
            Assert.AreEqual(0f, intensities.LowOxygen, 0.001f);
            Assert.AreEqual(0f, intensities.Concussion, 0.001f);
            Assert.AreEqual(0f, intensities.Unconscious, 0.001f);
        }

        [Test]
        public void BloodLossRampsBetweenSoftStartAndCritical()
        {
            HealthSnapshot softStart = HealthSnapshot.Default;
            softStart.Pools.BloodVolumeRatio = HealthScreenEffectMapper.BloodLossSoftStart;

            HealthSnapshot mid = HealthSnapshot.Default;
            mid.Pools.BloodVolumeRatio = (HealthScreenEffectMapper.BloodLossSoftStart
                + HealthConstants.CriticalBloodVolumeRatio) * 0.5f;

            HealthSnapshot critical = HealthSnapshot.Default;
            critical.Pools.BloodVolumeRatio = HealthConstants.CriticalBloodVolumeRatio;

            HealthSnapshot empty = HealthSnapshot.Default;
            empty.Pools.BloodVolumeRatio = 0f;

            Assert.AreEqual(0f, HealthScreenEffectMapper.Compute(softStart).BloodLossTunnelVision, 0.001f);
            Assert.Greater(HealthScreenEffectMapper.Compute(mid).BloodLossTunnelVision, 0.4f);
            Assert.AreEqual(1f, HealthScreenEffectMapper.Compute(critical).BloodLossTunnelVision, 0.001f);
            Assert.AreEqual(1f, HealthScreenEffectMapper.Compute(empty).BloodLossTunnelVision, 0.001f);
        }

        [Test]
        public void LowOxygenRampsWithOxyDebt()
        {
            HealthSnapshot none = HealthSnapshot.Default;
            none.Pools.OxyDebt = 0f;

            HealthSnapshot half = HealthSnapshot.Default;
            half.Pools.OxyDebt = HealthConstants.CriticalOxyDebt * 0.5f;

            HealthSnapshot critical = HealthSnapshot.Default;
            critical.Pools.OxyDebt = HealthConstants.CriticalOxyDebt;

            Assert.AreEqual(0f, HealthScreenEffectMapper.Compute(none).LowOxygen, 0.001f);
            Assert.AreEqual(0.5f, HealthScreenEffectMapper.Compute(half).LowOxygen, 0.001f);
            Assert.AreEqual(1f, HealthScreenEffectMapper.Compute(critical).LowOxygen, 0.001f);
        }

        [Test]
        public void ConcussionRampsAsBrainDeclines()
        {
            HealthSnapshot healthy = HealthSnapshot.Default;
            healthy.BrainFunctionPercent = 100f;

            HealthSnapshot mid = HealthSnapshot.Default;
            mid.BrainFunctionPercent = (100f + HealthConstants.ConsciousnessBrainFunctionPercent) * 0.5f;

            HealthSnapshot floor = HealthSnapshot.Default;
            floor.BrainFunctionPercent = HealthConstants.ConsciousnessBrainFunctionPercent;

            Assert.AreEqual(0f, HealthScreenEffectMapper.Compute(healthy).Concussion, 0.001f);
            Assert.Greater(HealthScreenEffectMapper.Compute(mid).Concussion, 0.4f);
            Assert.AreEqual(1f, HealthScreenEffectMapper.Compute(floor).Concussion, 0.001f);
        }

        [Test]
        public void CardiacArrestSetsDyingCriticalToFull()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.State = HealthState.Critical;
            snapshot.IsCardiacArrest = true;
            snapshot.BrainFunctionPercent = 80f;

            Assert.AreEqual(1f, HealthScreenEffectMapper.Compute(snapshot).DyingCritical, 0.001f);
        }

        [Test]
        public void CriticalWithoutArrestRampsDyingWithBrain()
        {
            HealthSnapshot highBrain = HealthSnapshot.Default;
            highBrain.State = HealthState.Critical;
            highBrain.BrainFunctionPercent = 100f;

            HealthSnapshot lowBrain = HealthSnapshot.Default;
            lowBrain.State = HealthState.Critical;
            lowBrain.BrainFunctionPercent = HealthConstants.ConsciousnessBrainFunctionPercent;

            float high = HealthScreenEffectMapper.Compute(highBrain).DyingCritical;
            float low = HealthScreenEffectMapper.Compute(lowBrain).DyingCritical;

            Assert.AreEqual(HealthScreenEffectMapper.CriticalDyingFloor, high, 0.001f);
            Assert.AreEqual(1f, low, 0.001f);
            Assert.Greater(low, high);
        }

        [Test]
        public void CriticalAttenuatesLowOxygenScreenIntensity()
        {
            HealthSnapshot oxyOnly = HealthSnapshot.Default;
            oxyOnly.Pools.OxyDebt = HealthConstants.CriticalOxyDebt;

            HealthSnapshot criticalWithOxy = HealthSnapshot.Default;
            criticalWithOxy.State = HealthState.Critical;
            criticalWithOxy.BrainFunctionPercent = 100f;
            criticalWithOxy.Pools.OxyDebt = HealthConstants.CriticalOxyDebt;

            float alone = HealthScreenEffectMapper.Compute(oxyOnly).LowOxygen;
            HealthScreenEffectMapper.Intensities stacked =
                HealthScreenEffectMapper.Compute(criticalWithOxy);

            Assert.AreEqual(1f, alone, 0.001f);
            Assert.AreEqual(HealthScreenEffectMapper.CriticalDyingFloor, stacked.DyingCritical, 0.001f);
            float expectedLowOxy = Mathf.Lerp(
                1f,
                HealthScreenEffectMapper.LowOxygenUnderDyingFloor,
                stacked.DyingCritical);
            Assert.AreEqual(expectedLowOxy, stacked.LowOxygen, 0.001f);
            Assert.Less(stacked.LowOxygen, alone);
        }

        [Test]
        public void CardiacArrestNearlyMutesLowOxygenScreen()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.IsCardiacArrest = true;
            snapshot.State = HealthState.CardiacArrest;
            snapshot.Pools.OxyDebt = HealthConstants.CriticalOxyDebt;

            HealthScreenEffectMapper.Intensities intensities =
                HealthScreenEffectMapper.Compute(snapshot);

            Assert.AreEqual(1f, intensities.DyingCritical, 0.001f);
            Assert.AreEqual(
                HealthScreenEffectMapper.LowOxygenUnderDyingFloor,
                intensities.LowOxygen,
                0.001f);
        }

        [Test]
        public void UnconsciousSetsBlackoutOnlyWhenNotConscious()
        {
            HealthSnapshot conscious = HealthSnapshot.Default;
            conscious.IsConscious = true;

            HealthSnapshot unconscious = HealthSnapshot.Default;
            unconscious.IsConscious = false;

            Assert.AreEqual(0f, HealthScreenEffectMapper.Compute(conscious).Unconscious, 0.001f);
            Assert.AreEqual(1f, HealthScreenEffectMapper.Compute(unconscious).Unconscious, 0.001f);
        }

        [Test]
        public void CardiacArrestAloneDoesNotForceUnconsciousBlackout()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.State = HealthState.Critical;
            snapshot.IsCardiacArrest = true;
            snapshot.IsConscious = true;

            HealthScreenEffectMapper.Intensities intensities =
                HealthScreenEffectMapper.Compute(snapshot);

            Assert.AreEqual(1f, intensities.DyingCritical, 0.001f);
            Assert.AreEqual(0f, intensities.Unconscious, 0.001f);
        }
    }
}
