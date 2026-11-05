using NUnit.Framework;
using SS3D.Systems.Combat;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;

namespace EditorTests
{
    public class ArmorSimulationTests
    {
        private static ArmorProfile TestProfile => new()
        {
            BruteAbsorption = 5f,
            BurnAbsorption = 3f,
            MaxIntegrity = 10f,
            CoveredZones = BodyZoneMask.Chest,
        };

        [Test]
        public void FullyAbsorbsDamageBelowAbsorptionValue()
        {
            (float remainingBrute, float remainingBurn, float integrityLoss) =
                ArmorSimulation.ResolveAbsorption(TestProfile, currentIntegrity: 10f, incomingBrute: 3f, incomingBurn: 1f);

            Assert.AreEqual(0f, remainingBrute);
            Assert.AreEqual(0f, remainingBurn);
            Assert.AreEqual(4f, integrityLoss);
        }

        [Test]
        public void OverflowDamagePassesThroughAfterAbsorption()
        {
            (float remainingBrute, float remainingBurn, float integrityLoss) =
                ArmorSimulation.ResolveAbsorption(TestProfile, currentIntegrity: 10f, incomingBrute: 12f, incomingBurn: 5f);

            Assert.AreEqual(7f, remainingBrute);
            Assert.AreEqual(2f, remainingBurn);
            Assert.AreEqual(8f, integrityLoss);
        }

        [Test]
        public void IntegrityDepletesByAbsorbedAmountNotIncomingAmount()
        {
            (_, _, float integrityLoss) =
                ArmorSimulation.ResolveAbsorption(TestProfile, currentIntegrity: 10f, incomingBrute: 100f, incomingBurn: 0f);

            Assert.AreEqual(5f, integrityLoss);
        }

        [Test]
        public void FinalHitScalesAbsorptionDownToRemainingIntegrityInsteadOfGoingNegative()
        {
            (float remainingBrute, float remainingBurn, float integrityLoss) =
                ArmorSimulation.ResolveAbsorption(TestProfile, currentIntegrity: 6f, incomingBrute: 10f, incomingBurn: 10f);

            Assert.AreEqual(6.25f, remainingBrute, 0.001f);
            Assert.AreEqual(7.75f, remainingBurn, 0.001f);
            Assert.AreEqual(6f, integrityLoss, 0.001f);
        }

        [Test]
        public void DepletedArmorPassesDamageThroughUntouched()
        {
            (float remainingBrute, float remainingBurn, float integrityLoss) =
                ArmorSimulation.ResolveAbsorption(TestProfile, currentIntegrity: 0f, incomingBrute: 8f, incomingBurn: 2f);

            Assert.AreEqual(8f, remainingBrute);
            Assert.AreEqual(2f, remainingBurn);
            Assert.AreEqual(0f, integrityLoss);
        }

        [Test]
        public void LayerRankPutsTheSuitOutsideTheJumpsuit()
        {
            Assert.Less(ArmorSimulation.LayerRank(ContainerType.ExoSuit), ArmorSimulation.LayerRank(ContainerType.Jumpsuit));
            Assert.Less(ArmorSimulation.LayerRank(ContainerType.Head), ArmorSimulation.LayerRank(ContainerType.Jumpsuit));
        }

        [TestCase(1f, 0f, true)]
        [TestCase(0f, 1f, true)]
        [TestCase(0f, 0f, false)]
        public void OnlyDamageLeftAfterEveryLayerReachesTheLimb(float brute, float burn, bool expected)
        {
            Assert.AreEqual(expected, ArmorSimulation.ReachesLimb(brute, burn));
        }
    }
}
