using NUnit.Framework;
using SS3D.Systems.Health;

namespace EditorTests
{
    public class HealthAlertStackMapperTests
    {
        [Test]
        public void HealthySnapshotProducesAllClear()
        {
            HealthAlertStackMapper.HealthAlertSignals signals =
                HealthAlertStackMapper.Compute(HealthSnapshot.Default);

            Assert.AreEqual(HealthAlertStackMapper.Severity.None, signals.Bleeding);
            Assert.AreEqual(HealthAlertStackMapper.Severity.None, signals.Dying);
            Assert.AreEqual(HealthAlertStackMapper.Severity.None, signals.CardiacArrest);
            Assert.AreEqual(HealthAlertStackMapper.Severity.None, signals.LowOxygen);
        }

        [Test]
        public void DeadSnapshotClearsAllSignals()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.State = HealthState.Dead;
            snapshot.IsBleeding = true;
            snapshot.TotalBleedingRate = 2f;
            snapshot.IsCardiacArrest = true;
            snapshot.Pools.OxyDebt = 1f;
            snapshot.CriticalFlags = HealthCriticalFlags.HighOxyDebt;

            HealthAlertStackMapper.HealthAlertSignals signals =
                HealthAlertStackMapper.Compute(snapshot);

            Assert.AreEqual(HealthAlertStackMapper.Severity.None, signals.Bleeding);
            Assert.AreEqual(HealthAlertStackMapper.Severity.None, signals.Dying);
            Assert.AreEqual(HealthAlertStackMapper.Severity.None, signals.CardiacArrest);
            Assert.AreEqual(HealthAlertStackMapper.Severity.None, signals.LowOxygen);
        }

        [Test]
        public void BleedingWarningWhenRateBelowCriticalThreshold()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.IsBleeding = true;
            snapshot.TotalBleedingRate = 0.5f;

            Assert.AreEqual(
                HealthAlertStackMapper.Severity.Warning,
                HealthAlertStackMapper.Compute(snapshot).Bleeding);
        }

        [Test]
        public void BleedingCriticalWhenRateAtOrAboveThreshold()
        {
            HealthSnapshot at = HealthSnapshot.Default;
            at.IsBleeding = true;
            at.TotalBleedingRate = HealthAlertStackMapper.BleedingCriticalRate;

            HealthSnapshot above = HealthSnapshot.Default;
            above.IsBleeding = true;
            above.TotalBleedingRate = 2f;

            Assert.AreEqual(
                HealthAlertStackMapper.Severity.Critical,
                HealthAlertStackMapper.Compute(at).Bleeding);
            Assert.AreEqual(
                HealthAlertStackMapper.Severity.Critical,
                HealthAlertStackMapper.Compute(above).Bleeding);
        }

        [Test]
        public void DyingIsCriticalOnlyWhenStateIsCritical()
        {
            HealthSnapshot critical = HealthSnapshot.Default;
            critical.State = HealthState.Critical;

            HealthSnapshot healthy = HealthSnapshot.Default;
            healthy.State = HealthState.Healthy;

            Assert.AreEqual(
                HealthAlertStackMapper.Severity.Critical,
                HealthAlertStackMapper.Compute(critical).Dying);
            Assert.AreEqual(
                HealthAlertStackMapper.Severity.None,
                HealthAlertStackMapper.Compute(healthy).Dying);
        }

        [Test]
        public void CardiacArrestIsCriticalIndependentOfDying()
        {
            HealthSnapshot snapshot = HealthSnapshot.Default;
            snapshot.State = HealthState.Healthy;
            snapshot.IsCardiacArrest = true;

            HealthAlertStackMapper.HealthAlertSignals signals =
                HealthAlertStackMapper.Compute(snapshot);

            Assert.AreEqual(HealthAlertStackMapper.Severity.Critical, signals.CardiacArrest);
            Assert.AreEqual(HealthAlertStackMapper.Severity.None, signals.Dying);
        }

        [Test]
        public void LowOxygenWarningThenCriticalByDebtAndFlag()
        {
            HealthSnapshot warning = HealthSnapshot.Default;
            warning.Pools.OxyDebt = HealthConstants.CriticalOxyDebt * 0.5f;

            HealthSnapshot byDebt = HealthSnapshot.Default;
            byDebt.Pools.OxyDebt = HealthConstants.CriticalOxyDebt;

            HealthSnapshot byFlag = HealthSnapshot.Default;
            byFlag.Pools.OxyDebt = 0f;
            byFlag.CriticalFlags = HealthCriticalFlags.HighOxyDebt;

            Assert.AreEqual(
                HealthAlertStackMapper.Severity.Warning,
                HealthAlertStackMapper.Compute(warning).LowOxygen);
            Assert.AreEqual(
                HealthAlertStackMapper.Severity.Critical,
                HealthAlertStackMapper.Compute(byDebt).LowOxygen);
            Assert.AreEqual(
                HealthAlertStackMapper.Severity.Critical,
                HealthAlertStackMapper.Compute(byFlag).LowOxygen);
        }

        [Test]
        public void MicroOxyDebtDoesNotRaiseLowOxygenWarning()
        {
            HealthSnapshot micro = HealthSnapshot.Default;
            micro.Pools.OxyDebt = HealthConstants.LowOxygenAlertSoftStart * 0.5f;

            HealthSnapshot softStart = HealthSnapshot.Default;
            softStart.Pools.OxyDebt = HealthConstants.LowOxygenAlertSoftStart;

            Assert.AreEqual(
                HealthAlertStackMapper.Severity.None,
                HealthAlertStackMapper.Compute(micro).LowOxygen);
            Assert.AreEqual(
                HealthAlertStackMapper.Severity.Warning,
                HealthAlertStackMapper.Compute(softStart).LowOxygen);
        }
    }
}
