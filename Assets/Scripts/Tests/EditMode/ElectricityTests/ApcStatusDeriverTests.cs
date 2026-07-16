using NUnit.Framework;
using SS3D.Systems.Electricity;

namespace EditorTests
{
    public class ApcStatusDeriverTests
    {
        [Test]
        public void DerivePowerState_NoGridAndEmptyCell_ReturnsCritical()
        {
            var stats = new CircuitStats
            {
                TotalSupplyKw = 0f,
                TotalDemandKw = 0f,
                ApcBatteryCharge = 0f,
                GridMeetsLoad = true,
            };

            Assert.AreEqual(ApcPowerState.Critical, ApcStatusDeriver.DerivePowerState(stats));
        }

        [Test]
        public void DerivePowerState_LoadExceedsSupply_ReturnsOverload()
        {
            var stats = new CircuitStats
            {
                TotalSupplyKw = 5f,
                TotalDemandKw = 12f,
                ApcBatteryCharge = 0.8f,
                GridMeetsLoad = false,
                BatteryDraining = true,
            };

            Assert.AreEqual(ApcPowerState.Overload, ApcStatusDeriver.DerivePowerState(stats));
        }

        [Test]
        public void DerivePowerState_SupplyMeetsLoad_ReturnsNominal()
        {
            var stats = new CircuitStats
            {
                TotalSupplyKw = 20f,
                TotalDemandKw = 12f,
                ApcBatteryCharge = 0.8f,
                GridMeetsLoad = true,
            };

            Assert.AreEqual(ApcPowerState.Nominal, ApcStatusDeriver.DerivePowerState(stats));
        }
    }
}
