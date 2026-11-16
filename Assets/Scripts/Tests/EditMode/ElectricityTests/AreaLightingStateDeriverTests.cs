using NUnit.Framework;
using SS3D.Systems.Area;
using SS3D.Systems.Electricity;

namespace EditorTests
{
    public class AreaLightingStateDeriverTests
    {
        [Test]
        public void Derive_ReturnsNormalWhenGridMeetsLoad()
        {
            var stats = new CircuitStats { GridMeetsLoad = true, ApcBatteryCharge = 0f };

            Assert.AreEqual(AreaLightingState.Normal, AreaLightingStateDeriver.Derive(stats, ApcControlFlags.All));
        }

        [Test]
        public void Derive_ReturnsNormalWhenLightingChannelCoversLightingLoad()
        {
            var stats = new CircuitStats
            {
                GridMeetsLoad = false,
                TotalSupplyKw = 3f,
                LightingLoadKw = 2f,
                ApcBatteryCharge = 0f,
            };

            Assert.AreEqual(
                AreaLightingState.Normal,
                AreaLightingStateDeriver.Derive(stats, ApcControlFlags.Lighting));
        }

        [Test]
        public void Derive_ReturnsEmergencyWhenGridFailsWithBatteryRemaining()
        {
            var stats = new CircuitStats
            {
                GridMeetsLoad = false,
                TotalSupplyKw = 0f,
                LightingLoadKw = 2f,
                ApcBatteryCharge = 0.5f,
            };

            Assert.AreEqual(
                AreaLightingState.Emergency,
                AreaLightingStateDeriver.Derive(stats, ApcControlFlags.All));
        }

        [Test]
        public void Derive_ReturnsDarkWhenLightingChannelIsOff()
        {
            var stats = new CircuitStats
            {
                GridMeetsLoad = false,
                TotalSupplyKw = 10f,
                LightingLoadKw = 0f,
                ApcBatteryCharge = 1f,
            };

            Assert.AreEqual(
                AreaLightingState.Dark,
                AreaLightingStateDeriver.Derive(stats, ApcControlFlags.Equipment));
        }

        [Test]
        public void Derive_ReturnsDarkWhenLightingSwitchIsOff()
        {
            var stats = new CircuitStats { GridMeetsLoad = true, ApcBatteryCharge = 1f };

            Assert.AreEqual(
                AreaLightingState.Dark,
                AreaLightingStateDeriver.Derive(stats, ApcControlFlags.All, lightingSwitchOn: false));
        }

        [Test]
        public void Derive_ReturnsDarkWhenLightingChannelOffDuringEmergency()
        {
            var stats = new CircuitStats
            {
                GridMeetsLoad = false,
                TotalSupplyKw = 0f,
                LightingLoadKw = 0f,
                ApcBatteryCharge = 0.75f,
            };

            Assert.AreEqual(
                AreaLightingState.Dark,
                AreaLightingStateDeriver.Derive(stats, ApcControlFlags.Equipment));
        }

        [Test]
        public void Derive_ReturnsDarkWhenGridFailsAndBatteryEmpty()
        {
            var stats = new CircuitStats
            {
                GridMeetsLoad = false,
                TotalSupplyKw = 0f,
                LightingLoadKw = 2f,
                ApcBatteryCharge = 0f,
            };

            Assert.AreEqual(
                AreaLightingState.Dark,
                AreaLightingStateDeriver.Derive(stats, ApcControlFlags.All));
        }
    }
}
