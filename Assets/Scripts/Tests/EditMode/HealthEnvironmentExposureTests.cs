using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Health;
using UnityEngine;

namespace EditorTests
{
    public class HealthEnvironmentExposureTests
    {
        [Test]
        public void StationAirHasFullBreathability()
        {
            float station = HealthEnvironmentState.StationOxygenMoleFraction;
            float breathability = HealthEnvironmentExposure.NormalizeBreathability(
                station,
                AtmosConstants.StandardPressure,
                isVacuum: false);

            Assert.AreEqual(1f, breathability, 0.001f);
            float po2 = HealthEnvironmentExposure.OxygenPartialPressureKpa(
                station,
                AtmosConstants.StandardPressure);
            Assert.Greater(po2, HealthConstants.OxygenPartialPressureComfortableKpa);
        }

        [Test]
        public void SlightlyLeanMixAtStationPressureStaysFullBreathability()
        {
            // ~18% O₂ at 101 kPa → PO₂ ≈ 18.2 kPa — still at/above comfortable band.
            float breathability = HealthEnvironmentExposure.NormalizeBreathability(
                0.18f,
                AtmosConstants.StandardPressure,
                isVacuum: false);

            Assert.AreEqual(1f, breathability, 0.001f);
        }

        [Test]
        public void VacuumNormalizesToZeroBreathability()
        {
            float breathability = HealthEnvironmentExposure.NormalizeBreathability(
                0.2f,
                0f,
                isVacuum: true);

            Assert.AreEqual(0f, breathability, 0.001f);
        }

        [Test]
        public void LowPartialPressureReducesBreathability()
        {
            // 10% O₂ at 101 kPa → PO₂ ≈ 10.1 kPa — between unbreathable and comfortable.
            float breathability = HealthEnvironmentExposure.NormalizeBreathability(
                0.10f,
                AtmosConstants.StandardPressure,
                isVacuum: false);

            Assert.Greater(breathability, 0f);
            Assert.Less(breathability, 1f);
        }

        [Test]
        public void UnbreathablePartialPressureIsZero()
        {
            float breathability = HealthEnvironmentExposure.NormalizeBreathability(
                0.04f,
                AtmosConstants.StandardPressure,
                isVacuum: false);

            Assert.AreEqual(0f, breathability, 0.001f);
        }

        [Test]
        public void SealStopsColdBurn()
        {
            float burn = HealthEnvironmentExposure.EnvironmentalBurnDamage(
                HealthConstants.ColdDamageTemperatureKelvin - 20f,
                burnIntensity: 0f,
                isSealed: true,
                heatProtection: 0.7f);

            Assert.AreEqual(0f, burn);
        }

        [Test]
        public void SealScalesFireDown()
        {
            float exposed = HealthEnvironmentExposure.EnvironmentalBurnDamage(
                AtmosConstants.StandardTemperature,
                burnIntensity: 1f);
            float sealedBurn = HealthEnvironmentExposure.EnvironmentalBurnDamage(
                AtmosConstants.StandardTemperature,
                burnIntensity: 1f,
                isSealed: true,
                heatProtection: 0.7f);

            Assert.AreEqual(exposed * 0.3f, sealedBurn, 0.0001f);
        }

        [Test]
        public void HeatProtectionNeedsASeal()
        {
            float exposed = HealthEnvironmentExposure.EnvironmentalBurnDamage(
                AtmosConstants.StandardTemperature,
                burnIntensity: 1f);
            float unsealed = HealthEnvironmentExposure.EnvironmentalBurnDamage(
                AtmosConstants.StandardTemperature,
                burnIntensity: 1f,
                isSealed: false,
                heatProtection: 0.7f);

            Assert.AreEqual(exposed, unsealed);
        }

        [Test]
        public void SealStopsPressureLungDamage()
        {
            Assert.AreEqual(0f, HealthEnvironmentExposure.PressureLungDamage(0f, isVacuum: true, isSealed: true));
        }

        [Test]
        public void HotTemperatureProducesBurnDamage()
        {
            float burn = HealthEnvironmentExposure.EnvironmentalBurnDamage(
                AirAlarmConstants.HighTemperatureKelvin + 25f,
                burnIntensity: 0f);

            Assert.Greater(burn, 0f);
            Assert.LessOrEqual(burn, HealthConstants.MaxEnvironmentalBurnPerZonePerTick);
        }

        [Test]
        public void ColdTemperatureProducesBurnDamage()
        {
            float burn = HealthEnvironmentExposure.EnvironmentalBurnDamage(
                HealthConstants.ColdDamageTemperatureKelvin - 20f,
                burnIntensity: 0f);

            Assert.Greater(burn, 0f);
        }

        [Test]
        public void FireIntensityProducesBurnDamage()
        {
            float burn = HealthEnvironmentExposure.EnvironmentalBurnDamage(
                AtmosConstants.StandardTemperature,
                burnIntensity: 1f);

            Assert.AreEqual(HealthConstants.FireBurnPerIntensity, burn, 0.001f);
        }

        [Test]
        public void StationPressureProducesNoLungDamage()
        {
            float damage = HealthEnvironmentExposure.PressureLungDamage(
                AtmosConstants.StandardPressure,
                isVacuum: false);

            Assert.AreEqual(0f, damage, 0.001f);
        }

        [Test]
        public void VacuumProducesLungBarotrauma()
        {
            float damage = HealthEnvironmentExposure.PressureLungDamage(0f, isVacuum: true);

            Assert.AreEqual(HealthConstants.VacuumLungDamagePerTick, damage, 0.001f);
        }

        [Test]
        public void LowPressureProducesLungDamage()
        {
            float pressure = AirAlarmConstants.LowPressureKpa - 40f;
            float damage = HealthEnvironmentExposure.PressureLungDamage(pressure, isVacuum: false);

            Assert.Greater(damage, 0f);
            Assert.AreEqual(
                40f * HealthConstants.LowPressureLungDamagePerKpa,
                damage,
                0.001f);
        }

        [Test]
        public void HighPressureProducesLungDamage()
        {
            float pressure = AirAlarmConstants.HighPressureKpa + 50f;
            float damage = HealthEnvironmentExposure.PressureLungDamage(pressure, isVacuum: false);

            Assert.Greater(damage, 0f);
            Assert.AreEqual(
                50f * HealthConstants.HighPressureLungDamagePerKpa,
                damage,
                0.001f);
        }

        [Test]
        public void BreathMolesScaleWithBreathabilityAndLungs()
        {
            float full = HealthEnvironmentExposure.BreathOxygenMoles(1f, 1f);
            float half = HealthEnvironmentExposure.BreathOxygenMoles(0.5f, 1f);
            float none = HealthEnvironmentExposure.BreathOxygenMoles(0f, 1f);

            Assert.AreEqual(HealthConstants.BreathOxygenMolesPerTick, full, 0.001f);
            Assert.AreEqual(HealthConstants.BreathOxygenMolesPerTick * 0.5f, half, 0.001f);
            Assert.AreEqual(0f, none, 0.001f);
            Assert.Less(Mathf.Abs(half - full * 0.5f), 0.001f);
        }

        private static readonly GasTankMix OxygenTank = new() { OxygenMoles = 45f };
        private static readonly GasTankMix PlasmaTank = new() { PlasmaMoles = 45f };

        [Test]
        public void OxygenTankGivesAFullBreathInVacuumButLeavesThePressure()
        {
            HealthEnvironmentState env = HealthEnvironmentExposure.BreatheFromTank(
                HealthEnvironmentExposure.FromVacuumCell(3f, burnIntensity: 0f),
                OxygenTank,
                tankFill: 1f);

            Assert.IsTrue(env.OnInternals);
            Assert.AreEqual(1f, env.AtmosphereBreathability);
            Assert.IsTrue(env.IsVacuum);
            Assert.Greater(HealthEnvironmentExposure.PressureLungDamage(env.PressureKpa, env.IsVacuum, env.IsSealed), 0f);
        }

        [Test]
        public void PlasmaTankSuppliesNoBreathAndPoisons()
        {
            HealthEnvironmentState env = HealthEnvironmentExposure.BreatheFromTank(
                HealthEnvironmentState.SafeDefault,
                PlasmaTank,
                tankFill: 1f);

            Assert.AreEqual(0f, env.AtmosphereBreathability);
            Assert.Greater(HealthEnvironmentExposure.ToxinIntakeFromPlasma(env.PlasmaMoleFraction), 0f);
        }

        [Test]
        public void TankAirKeepsRoomPlasmaOut()
        {
            HealthEnvironmentState room = HealthEnvironmentState.SafeDefault;
            room.PlasmaMoleFraction = 0.5f;

            HealthEnvironmentState env = HealthEnvironmentExposure.BreatheFromTank(room, OxygenTank, tankFill: 1f);

            Assert.AreEqual(0f, HealthEnvironmentExposure.ToxinIntakeFromPlasma(env.PlasmaMoleFraction));
        }

        [Test]
        public void EmptyTankStopsTheBreathEvenInGoodAir()
        {
            HealthEnvironmentState env = HealthEnvironmentExposure.BreatheFromTank(
                HealthEnvironmentState.SafeDefault,
                default,
                tankFill: 0f);

            Assert.AreEqual(0f, env.AtmosphereBreathability);
            Assert.AreEqual(0f, HealthEnvironmentExposure.ToxinIntakeFromPlasma(env.PlasmaMoleFraction));
        }

        [Test]
        public void InternalsLeaveTheSealAlone()
        {
            HealthEnvironmentState sealedRoom = HealthEnvironmentExposure.FromVacuumCell(3f, burnIntensity: 0f);
            sealedRoom.IsSealed = true;
            sealedRoom.HeatProtection = 0.7f;

            HealthEnvironmentState env = HealthEnvironmentExposure.BreatheFromTank(sealedRoom, OxygenTank, tankFill: 1f);

            Assert.IsTrue(env.IsSealed);
            Assert.AreEqual(0.7f, env.HeatProtection);
        }

        [Test]
        public void DrawingFromATankKeepsItsMix()
        {
            GasTankMix mix = new GasTankMix { OxygenMoles = 30f, PlasmaMoles = 10f }.Without(20f);

            Assert.AreEqual(15f, mix.OxygenMoles, 0.001f);
            Assert.AreEqual(5f, mix.PlasmaMoles, 0.001f);
            Assert.AreEqual(0.25f, mix.PlasmaFraction, 0.001f);
        }

        [Test]
        public void DrawingMoreThanATankHoldsEmptiesIt()
        {
            GasTankMix mix = OxygenTank.Without(100f);

            Assert.AreEqual(0f, mix.TotalMoles);
            Assert.AreEqual(0f, mix.OxygenFraction);
        }

        [Test]
        public void FullTankLastsFifteenMinutesOfHealthyBreathing()
        {
            float secondsOfBreath = OxygenTank.TotalMoles
                / HealthEnvironmentExposure.BreathOxygenMoles(1f, 1f)
                * HealthConstants.TickIntervalSeconds;

            Assert.AreEqual(15f * 60f, secondsOfBreath, 0.5f);
        }
    }
}
