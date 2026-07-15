using NUnit.Framework;
using SS3D.Systems.Health;

namespace EditorTests
{
    public class ZoneTargetResolverTests
    {
        [Test]
        public void ResolveGroinBandMapsLowerChestHitsToGroin()
        {
            BodyZone zone = ZoneTargetResolver.ResolveGroinBand(BodyZone.Chest, 0.2f);
            Assert.AreEqual(BodyZone.Groin, zone);
        }

        [Test]
        public void ResolveGroinBandKeepsUpperChestHitsOnChest()
        {
            BodyZone zone = ZoneTargetResolver.ResolveGroinBand(BodyZone.Chest, 0.6f);
            Assert.AreEqual(BodyZone.Chest, zone);
        }

        [Test]
        public void ResolveGroinBandIgnoresNonChestZones()
        {
            BodyZone zone = ZoneTargetResolver.ResolveGroinBand(BodyZone.Head, 0.1f);
            Assert.AreEqual(BodyZone.Head, zone);
        }
    }
}
