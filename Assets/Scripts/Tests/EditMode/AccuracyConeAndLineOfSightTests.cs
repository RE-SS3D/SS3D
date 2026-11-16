using NUnit.Framework;
using SS3D.Systems.Combat;
using SS3D.Systems.Inventory.Containers;
using SS3D.Utils;
using UnityEngine;

namespace EditorTests
{
    public class AccuracyConeTests
    {
        [Test]
        public void SampleDirection_ZeroSpread_ReturnsAim()
        {
            Vector3 aim = new Vector3(0f, 0f, 1f);
            Vector3 sample = AccuracyCone.SampleDirection(aim, 0f, new System.Random(1));
            Assert.AreEqual(1f, sample.z, 0.0001f);
            Assert.AreEqual(0f, sample.x, 0.0001f);
            Assert.AreEqual(0f, sample.y, 0.0001f);
        }

        [Test]
        public void SampleDirection_StaysWithinCone()
        {
            Vector3 aim = Vector3.forward;
            float spread = 5f;
            var rng = new System.Random(42);
            for (int i = 0; i < 200; i++)
            {
                Vector3 sample = AccuracyCone.SampleDirection(aim, spread, rng);
                float angle = Vector3.Angle(aim, sample);
                Assert.LessOrEqual(angle, spread + 0.05f);
            }
        }

        [Test]
        public void ComputeSpread_AddsRecoilAndMovement()
        {
            RangedWeaponProfile profile = RangedWeaponProfile.M4;
            float baseOnly = AccuracyCone.ComputeSpreadDegrees(profile, 0f, 0f, 5f);
            float withRecoil = AccuracyCone.ComputeSpreadDegrees(profile, 2f, 0f, 5f);
            float withMove = AccuracyCone.ComputeSpreadDegrees(profile, 0f, 3f, 5f);
            Assert.Greater(withRecoil, baseOnly);
            Assert.Greater(withMove, baseOnly);
        }

        [Test]
        public void ComputeSpread_RangeFalloffWidensPastStart()
        {
            RangedWeaponProfile profile = RangedWeaponProfile.M4;
            float near = AccuracyCone.ComputeSpreadDegrees(profile, 0f, 0f, profile.FalloffStartMeters - 1f);
            float far = AccuracyCone.ComputeSpreadDegrees(profile, 0f, 0f, profile.FalloffEndMeters);
            Assert.Greater(far, near);
        }

        [Test]
        public void ComputeSpread_ExertionWidensCone()
        {
            RangedWeaponProfile profile = RangedWeaponProfile.M4;
            float rested = AccuracyCone.ComputeSpreadDegrees(profile, 0f, 0f, 5f, 0f);
            float winded = AccuracyCone.ComputeSpreadDegrees(profile, 0f, 0f, 5f, 0.5f);
            float exhausted = AccuracyCone.ComputeSpreadDegrees(profile, 0f, 0f, 5f, 1f);
            Assert.Greater(winded, rested);
            Assert.Greater(exhausted, winded);
        }
    }

    public class LineOfSightTests
    {
        [Test]
        public void HasLineOfSight_ZeroDistance_IsClear()
        {
            Vector3 p = Vector3.zero;
            bool clear = LineOfSight.HasLineOfSight(p, p, ~0, out RaycastHit hit);
            Assert.IsTrue(clear);
            Assert.AreEqual(default(RaycastHit), hit);
        }
    }

    public class RangedWeaponProfileTests
    {
        [Test]
        public void M4_HasPositiveMagAndCooldown()
        {
            RangedWeaponProfile m4 = RangedWeaponProfile.M4;
            Assert.Greater(m4.MagazineSize, 0);
            Assert.Greater(m4.FireCooldownSeconds, 0f);
            Assert.Greater(m4.ReloadSeconds, 0f);
            Assert.Greater(m4.MaxRangeMeters, ZoneTargetResolverDefaultRay());
        }

        [Test]
        public void M4_HasCombatStaminaCosts()
        {
            RangedWeaponProfile m4 = RangedWeaponProfile.M4;
            Assert.Greater(m4.StaminaCost, 0f);
            Assert.Greater(m4.ExhaustionSpreadDegrees, 0f);
        }

        [Test]
        public void M4_RequiresBothHandsOnRight()
        {
            RangedWeaponProfile m4 = RangedWeaponProfile.M4;
            Assert.IsTrue(m4.RequiresBothHands);
            Assert.AreEqual(HandSide.Right, m4.RequiredHand);
        }

        private static float ZoneTargetResolverDefaultRay() => 8f;
    }
}
