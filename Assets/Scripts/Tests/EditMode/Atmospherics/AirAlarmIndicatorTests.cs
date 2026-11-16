using NUnit.Framework;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.UI.MachineInterface;

namespace EditorTests.Atmospherics
{
    public class AirAlarmIndicatorTests
    {
        [Test]
        public void Resolve_NormalAirShowsSteadyGreen()
        {
            Assert.AreEqual(AirAlarmIndicator.Nominal,
                AirAlarmIndicatorRules.Resolve(true, AirAlarmState.None, AirAlarmPressureTrend.Steady));
        }

        [Test]
        public void Resolve_UnpoweredIsDark()
        {
            Assert.AreEqual(AirAlarmIndicator.Off,
                AirAlarmIndicatorRules.Resolve(false, AirAlarmState.HighPlasma, AirAlarmPressureTrend.Falling));
        }

        [Test]
        public void Resolve_LowAirThatIsRisingIsARefill()
        {
            Assert.AreEqual(AirAlarmIndicator.Repressurizing, AirAlarmIndicatorRules.Resolve(
                true, AirAlarmState.LowPressure | AirAlarmState.LowOxygen, AirAlarmPressureTrend.Rising));
        }

        [Test]
        public void Resolve_FallingPressureIsALeakEvenInsideTheSafeBand()
        {
            Assert.AreEqual(AirAlarmIndicator.Depressurizing,
                AirAlarmIndicatorRules.Resolve(true, AirAlarmState.None, AirAlarmPressureTrend.Falling));
        }

        [Test]
        public void Resolve_StillVacuumIsDanger()
        {
            Assert.AreEqual(AirAlarmIndicator.Danger,
                AirAlarmIndicatorRules.Resolve(true, AirAlarmState.LowPressure, AirAlarmPressureTrend.Steady));
        }

        [Test]
        public void Resolve_ToxicAirIsDangerWhilePressureMoves()
        {
            Assert.AreEqual(AirAlarmIndicator.Danger,
                AirAlarmIndicatorRules.Resolve(true, AirAlarmState.HighPlasma | AirAlarmState.LowPressure, AirAlarmPressureTrend.Rising));
        }

        [Test]
        public void ResolveTrend_NeedsTheEnterRateToStartAndHoldsUntilTheExitRate()
        {
            float between = (AirAlarmIndicatorRules.TrendEnterKpaPerSecond + AirAlarmIndicatorRules.TrendExitKpaPerSecond) / 2f;

            Assert.AreEqual(AirAlarmPressureTrend.Steady, AirAlarmIndicatorRules.ResolveTrend(-between, AirAlarmPressureTrend.Steady));
            Assert.AreEqual(AirAlarmPressureTrend.Falling, AirAlarmIndicatorRules.ResolveTrend(-between, AirAlarmPressureTrend.Falling));
            Assert.AreEqual(AirAlarmPressureTrend.Rising, AirAlarmIndicatorRules.ResolveTrend(1f, AirAlarmPressureTrend.Steady));
            Assert.AreEqual(AirAlarmPressureTrend.Steady, AirAlarmIndicatorRules.ResolveTrend(0.1f, AirAlarmPressureTrend.Rising));
        }

        [Test]
        public void SmoothRate_ABreachOverAFewTicksReadsAsFalling()
        {
            float rate = 0f;
            float pressure = 101f;
            AirAlarmPressureTrend trend = AirAlarmPressureTrend.Steady;
            for (int i = 0; i < 3; i++)
            {
                float next = pressure - 1f;
                rate = AirAlarmIndicatorRules.SmoothRate(rate, pressure, next, 0.2f);
                trend = AirAlarmIndicatorRules.ResolveTrend(rate, trend);
                pressure = next;
            }

            Assert.AreEqual(AirAlarmPressureTrend.Falling, trend);
        }

        [Test]
        public void StatusText_LowPressureDoesNotClaimCarbonDioxide()
        {
            AirAlarmStatus status = AirAlarmStatusText.Describe(true, true, AirAlarmState.LowPressure, AirAlarmIndicator.Danger);

            Assert.AreEqual("LOW PRESSURE", status.Headline);
            StringAssert.DoesNotContain("CO2", status.Subline);
            Assert.AreEqual(AirAlarmSeverity.Danger, status.Severity);
        }

        [Test]
        public void StatusText_NamesTheOtherConditionsUnderTheHeadline()
        {
            AirAlarmStatus status = AirAlarmStatusText.Describe(
                true, true, AirAlarmState.LowPressure | AirAlarmState.HighCarbonDioxide, AirAlarmIndicator.Danger);

            Assert.AreEqual("LOW PRESSURE", status.Headline);
            StringAssert.EndsWith("Also: CO2.", status.Subline);
        }

        [Test]
        public void StatusText_RefillReadsAsRepressurizing()
        {
            AirAlarmStatus status = AirAlarmStatusText.Describe(true, true, AirAlarmState.LowPressure, AirAlarmIndicator.Repressurizing);

            Assert.AreEqual("REPRESSURIZING", status.Headline);
            Assert.AreEqual(AirAlarmSeverity.Warning, status.Severity);
        }

        [Test]
        public void StatusText_UnpoweredSaysNoPowerNotPlasmaFire()
        {
            AirAlarmStatus status = AirAlarmStatusText.Describe(false, false, AirAlarmState.None, AirAlarmIndicator.Off);

            Assert.AreEqual("NO POWER", status.Headline);
        }

        [Test]
        public void SnapshotMapper_HeadlineComesFromTheAlarmFlags()
        {
            AirAlarmInterfaceSnapshot snapshot = new()
            {
                PowerOk = true,
                HasSample = true,
                Scenario = (byte)AirAlarmScenario.Warning,
                AlarmState = (byte)AirAlarmState.HighPressure,
                Indicator = (byte)AirAlarmIndicator.Danger,
                PressureKpa = 170f,
                TemperatureKelvin = 293f,
                OxygenFraction = 0.21f,
                NitrogenFraction = 0.79f,
            };

            AirAlarmInterfaceViewModel model = AirAlarmInterfaceSnapshotMapper.ToViewModel(snapshot);

            Assert.AreEqual("HIGH PRESSURE", model.StatusHeadline);
            Assert.AreEqual("DANGER", model.StatusBadgeText);
        }
    }
}
