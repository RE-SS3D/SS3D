using NUnit.Framework;
using SS3D.Systems.StructuralDamage;
using UnityEngine;

namespace EditorTests
{
    public class BlastVfxFalloffTests
    {
        [Test]
        public void DistanceAttenuation_FullInsideNearRange()
        {
            Assert.AreEqual(1f, BlastVfxFalloff.DistanceAttenuation(0f, 4f, 18f));
            Assert.AreEqual(1f, BlastVfxFalloff.DistanceAttenuation(4f, 4f, 18f));
        }

        [Test]
        public void DistanceAttenuation_ZeroBeyondMax()
        {
            Assert.AreEqual(0f, BlastVfxFalloff.DistanceAttenuation(18f, 4f, 18f));
            Assert.AreEqual(0f, BlastVfxFalloff.DistanceAttenuation(40f, 4f, 18f));
        }

        [Test]
        public void DistanceAttenuation_LinearBetween()
        {
            float mid = BlastVfxFalloff.DistanceAttenuation(11f, 4f, 18f);
            Assert.AreEqual(0.5f, mid, 0.001f);
        }

        [Test]
        public void YieldScale_ClampsAroundReference()
        {
            Assert.AreEqual(1f, BlastVfxFalloff.YieldScale(120f), 0.001f);
            Assert.AreEqual(0.35f, BlastVfxFalloff.YieldScale(1f), 0.001f);
            Assert.AreEqual(2.5f, BlastVfxFalloff.YieldScale(1000f), 0.001f);
            Assert.Greater(BlastVfxFalloff.YieldScale(60f), 0.35f);
            Assert.Less(BlastVfxFalloff.YieldScale(60f), 1f);
        }

        [Test]
        public void FireCoreColor_MatchesAtmosWarmOrange()
        {
            Color c = BlastVfxCatalog.FireCoreColor;
            Assert.AreEqual(1f, c.r, 0.001f);
            Assert.AreEqual(0.55f, c.g, 0.001f);
            Assert.AreEqual(0.15f, c.b, 0.001f);
        }
    }
}
