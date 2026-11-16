using NUnit.Framework;
using SS3D.Systems.Health;

namespace EditorTests
{
    /// <summary>Flinch, stagger and the sprint trip (combat.md §5, health.md §5).</summary>
    public class HitReactionTests
    {
        private const float M4Round = 18f;

        [Test]
        public void AnM4RoundFlinchesAndAPointBlankBlastStaggers()
        {
            Assert.IsFalse(HitReaction.IsStagger(M4Round));
            Assert.IsTrue(HitReaction.IsStagger(HealthConstants.HeavyHitStaggerDamage));
        }

        [Test]
        public void FlinchAndViewKickScaleWithDamageBeforeArmor()
        {
            Assert.Less(HitReaction.FlinchStrength(5f), HitReaction.FlinchStrength(M4Round));
            Assert.AreEqual(3, HitReaction.FlinchStrength(HealthConstants.HeavyHitStaggerDamage * 2f));
            Assert.Less(HitReaction.ViewKickAmplitude(5f), HitReaction.ViewKickAmplitude(M4Round));
            Assert.AreEqual(HealthConstants.ViewKickMaxAmplitude, HitReaction.ViewKickAmplitude(100f), 1e-5f);
            Assert.Greater(HitReaction.ViewKickAmplitude(1f), 0f, "even a soaked hit kicks the view");
        }

        [TestCase(BodyZone.LeftLeg, WoundSeverity.Wound, 1f, true)]
        [TestCase(BodyZone.RightLeg, WoundSeverity.Severe, 0.9f, true)]
        [TestCase(BodyZone.LeftLeg, WoundSeverity.Bruised, 1f, false)]
        [TestCase(BodyZone.LeftLeg, WoundSeverity.Wound, 0.3f, false)]
        [TestCase(BodyZone.Chest, WoundSeverity.Severe, 1f, false)]
        public void AWoundedLegHitTripsOnlyASprintingBody(BodyZone zone, WoundSeverity before, float gait, bool trips)
        {
            Assert.AreEqual(trips, HitReaction.ShouldTrip(zone, before, gait));
        }
    }
}
