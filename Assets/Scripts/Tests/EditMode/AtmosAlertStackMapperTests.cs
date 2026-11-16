using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Health;

namespace EditorTests
{
    public class AtmosAlertStackMapperTests
    {
        [Test]
        public void SafeDefaultProducesNoAlerts()
        {
            // SafeDefault has HasSample=false → mapper returns none.
            AtmosAlertStackMapper.AtmosAlertSignals signals =
                AtmosAlertStackMapper.Compute(HealthEnvironmentState.SafeDefault);

            Assert.AreEqual(AtmosAlertStackMapper.Severity.None, signals.Fire);
            Assert.AreEqual(AtmosAlertStackMapper.Severity.None, signals.Hot);
            Assert.AreEqual(AtmosAlertStackMapper.Severity.None, signals.Cold);
            Assert.AreEqual(AtmosAlertStackMapper.Severity.None, signals.LowPressure);
            Assert.AreEqual(AtmosAlertStackMapper.Severity.None, signals.HighPressure);
            Assert.AreEqual(AtmosAlertStackMapper.Severity.None, signals.LowOxygen);
        }

        [Test]
        public void VacuumRaisesCriticalPressureAndOxygen()
        {
            HealthEnvironmentState env = HealthEnvironmentExposure.FromVacuumCell(
                AtmosConstants.SpaceTemperature,
                burnIntensity: 0f);

            AtmosAlertStackMapper.AtmosAlertSignals signals = AtmosAlertStackMapper.Compute(env);

            Assert.AreEqual(AtmosAlertStackMapper.Severity.Critical, signals.LowPressure);
            Assert.AreEqual(AtmosAlertStackMapper.Severity.Critical, signals.LowOxygen);
        }

        [TestCase(70f, AtmosAlertStackMapper.Severity.Warning)]
        [TestCase(20f, AtmosAlertStackMapper.Severity.Warning)]
        [TestCase(19.9f, AtmosAlertStackMapper.Severity.Critical)]
        [TestCase(1f, AtmosAlertStackMapper.Severity.Critical)]
        public void LowPressureTurnsCriticalBelowTwentyKilopascals(float pressureKpa, AtmosAlertStackMapper.Severity expected)
        {
            HealthEnvironmentState env = HealthEnvironmentState.SafeDefault;
            env.HasSample = true;
            env.PressureKpa = pressureKpa;

            Assert.AreEqual(expected, AtmosAlertStackMapper.Compute(env).LowPressure);
        }

        [Test]
        public void BreachInSafeAirIsAWarning()
        {
            HealthEnvironmentState env = HealthEnvironmentState.SafeDefault;
            env.HasSample = true;
            env.IsBreached = true;

            Assert.AreEqual(AtmosAlertStackMapper.Severity.Warning, AtmosAlertStackMapper.Compute(env).SealBreach);
        }

        [Test]
        public void BreachInVacuumIsCritical()
        {
            HealthEnvironmentState env = HealthEnvironmentExposure.FromVacuumCell(
                AtmosConstants.SpaceTemperature,
                burnIntensity: 0f);
            env.IsBreached = true;

            AtmosAlertStackMapper.AtmosAlertSignals signals = AtmosAlertStackMapper.Compute(env);

            Assert.AreEqual(AtmosAlertStackMapper.Severity.Critical, signals.SealBreach);
            Assert.AreEqual(AtmosAlertStackMapper.Severity.Critical, signals.LowPressure);
        }

        [Test]
        public void NoBreachRaisesNoBreachAlert()
        {
            HealthEnvironmentState env = HealthEnvironmentExposure.FromVacuumCell(
                AtmosConstants.SpaceTemperature,
                burnIntensity: 0f);

            Assert.AreEqual(AtmosAlertStackMapper.Severity.None, AtmosAlertStackMapper.Compute(env).SealBreach);
        }

        [Test]
        public void SealedVacuumRaisesOnlyLowOxygen()
        {
            HealthEnvironmentState env = HealthEnvironmentExposure.FromVacuumCell(
                AtmosConstants.SpaceTemperature,
                burnIntensity: 0f);
            env.IsSealed = true;

            AtmosAlertStackMapper.AtmosAlertSignals signals = AtmosAlertStackMapper.Compute(env);

            Assert.AreEqual(AtmosAlertStackMapper.Severity.None, signals.LowPressure);
            Assert.AreEqual(AtmosAlertStackMapper.Severity.None, signals.Cold);
            Assert.AreEqual(AtmosAlertStackMapper.Severity.Critical, signals.LowOxygen);
        }

        [Test]
        public void HotRoomRaisesHotAlert()
        {
            HealthEnvironmentState env = HealthEnvironmentState.SafeDefault;
            env.HasSample = true;
            env.TemperatureKelvin = AirAlarmConstants.HighTemperatureKelvin + 5f;
            env.PressureKpa = AtmosConstants.StandardPressure;
            env.AtmosphereBreathability = 1f;
            env.OxygenMoleFraction = HealthEnvironmentState.StationOxygenMoleFraction;

            AtmosAlertStackMapper.AtmosAlertSignals signals = AtmosAlertStackMapper.Compute(env);

            Assert.AreEqual(AtmosAlertStackMapper.Severity.Warning, signals.Hot);
        }

        [Test]
        public void StationPressureNormalMixDoesNotRaiseLowOxygen()
        {
            HealthEnvironmentState env = HealthEnvironmentState.SafeDefault;
            env.HasSample = true;
            env.PressureKpa = AtmosConstants.StandardPressure;
            env.OxygenMoleFraction = HealthEnvironmentState.StationOxygenMoleFraction;
            env.AtmosphereBreathability = 1f;

            AtmosAlertStackMapper.AtmosAlertSignals signals = AtmosAlertStackMapper.Compute(env);

            Assert.AreEqual(AtmosAlertStackMapper.Severity.None, signals.LowOxygen);
        }

        [Test]
        public void LowPartialPressureRaisesLowOxygenWarning()
        {
            HealthEnvironmentState env = HealthEnvironmentState.SafeDefault;
            env.HasSample = true;
            env.PressureKpa = AtmosConstants.StandardPressure;
            // PO₂ ≈ 14 kPa — below hypoxia onset (16), above critical (10).
            env.OxygenMoleFraction = 0.14f;
            env.AtmosphereBreathability = HealthEnvironmentExposure.NormalizeBreathability(
                env.OxygenMoleFraction,
                env.PressureKpa,
                isVacuum: false);

            AtmosAlertStackMapper.AtmosAlertSignals signals = AtmosAlertStackMapper.Compute(env);

            Assert.AreEqual(AtmosAlertStackMapper.Severity.Warning, signals.LowOxygen);
        }

        [Test]
        public void AtmosphericDamageDisabledClearsAtmosAlerts()
        {
            bool previous = HealthEnvironmentSettings.AtmosphericDamageDisabled;
            try
            {
                HealthEnvironmentSettings.AtmosphericDamageDisabled = true;
                HealthEnvironmentState env = HealthEnvironmentExposure.FromVacuumCell(
                    AtmosConstants.SpaceTemperature,
                    0f);

                AtmosAlertStackMapper.AtmosAlertSignals signals = AtmosAlertStackMapper.Compute(env);

                Assert.AreEqual(AtmosAlertStackMapper.Severity.None, signals.LowPressure);
                Assert.AreEqual(AtmosAlertStackMapper.Severity.None, signals.LowOxygen);
            }
            finally
            {
                HealthEnvironmentSettings.AtmosphericDamageDisabled = previous;
            }
        }

        private static HealthEnvironmentState OnTank(float fill, bool empty = false)
        {
            return HealthEnvironmentExposure.BreatheFromTank(
                HealthEnvironmentExposure.FromVacuumCell(3f, burnIntensity: 0f),
                empty ? default : new GasTankMix { OxygenMoles = 45f },
                fill);
        }

        [Test]
        public void TankBreathClearsLowOxygenButNotThePressureWarning()
        {
            AtmosAlertStackMapper.AtmosAlertSignals signals = AtmosAlertStackMapper.Compute(OnTank(1f));

            Assert.AreEqual(AtmosAlertStackMapper.Severity.None, signals.LowOxygen);
            Assert.AreEqual(AtmosAlertStackMapper.Severity.Critical, signals.LowPressure);
        }

        [Test]
        public void EmptyTankIsCriticalOnBothAlerts()
        {
            AtmosAlertStackMapper.AtmosAlertSignals signals = AtmosAlertStackMapper.Compute(OnTank(0f, empty: true));

            Assert.AreEqual(AtmosAlertStackMapper.Severity.Critical, signals.LowOxygen);
            Assert.AreEqual(AtmosAlertStackMapper.Severity.Critical, signals.TankLow);
        }

        [TestCase(1f, AtmosAlertStackMapper.Severity.None)]
        [TestCase(0.26f, AtmosAlertStackMapper.Severity.None)]
        [TestCase(0.25f, AtmosAlertStackMapper.Severity.Warning)]
        [TestCase(0.11f, AtmosAlertStackMapper.Severity.Warning)]
        [TestCase(0.1f, AtmosAlertStackMapper.Severity.Critical)]
        public void TankLowFollowsTheFillWhileBreathing(float fill, AtmosAlertStackMapper.Severity expected)
        {
            Assert.AreEqual(expected, AtmosAlertStackMapper.Compute(OnTank(fill)).TankLow);
        }

        [Test]
        public void TankLowStaysQuietWithInternalsOff()
        {
            HealthEnvironmentState env = HealthEnvironmentState.SafeDefault;
            env.HasSample = true;

            Assert.AreEqual(AtmosAlertStackMapper.Severity.None, AtmosAlertStackMapper.Compute(env).TankLow);
        }
    }
}
