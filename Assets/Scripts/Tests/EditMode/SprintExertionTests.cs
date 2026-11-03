using NUnit.Framework;
using SS3D.Systems.Stamina;

namespace SS3D.Tests.EditMode
{
    /// <summary>Sprint drain and exhaustion slowdown rules (M1m, Documents/design/stamina.md §2–3).</summary>
    public class SprintExertionTests
    {
        [Test]
        [TestCase(0f, 0f)]
        [TestCase(0.3f, 0f)]
        [TestCase(0.65f, 0.5f)]
        [TestCase(1f, 1f)]
        public void DrainFraction_OnlyAboveWalkGait(float gait, float expected)
        {
            Assert.AreEqual(expected, SprintExertion.DrainFraction(gait, 0.3f, 1f), 0.001f);
        }

        [Test]
        [TestCase(0f, 1f)]
        [TestCase(0.5f, 1f)]
        [TestCase(0.75f, 1f)]
        [TestCase(1f, 0.55f)]
        public void MovementSpeedFactor_FullSpeedUntilLowStamina(float exertionPenalty, float expected)
        {
            Assert.AreEqual(expected, SprintExertion.MovementSpeedFactor(exertionPenalty), 0.001f);
        }

        [Test]
        public void MovementSpeedFactor_RampsBelowThreshold()
        {
            float halfway = SprintExertion.MovementSpeedFactor(1f - (SprintExertion.SlowdownThreshold * 0.5f));
            Assert.Greater(halfway, SprintExertion.ExhaustedSpeedFactor);
            Assert.Less(halfway, 1f);
        }

        [Test]
        public void FullPool_LastsAboutTenSecondsOfSprint()
        {
            float seconds = StaminaFactory.DefaultMax / SprintExertion.SprintDrainPerSecond;
            Assert.AreEqual(10f, seconds, 0.5f);
        }

        [Test]
        public void Regen_WaitsAfterSprint()
        {
            Assert.IsFalse(SprintExertion.ShouldRegenerate(0.5f));
            Assert.IsTrue(SprintExertion.ShouldRegenerate(SprintExertion.RegenDelayAfterSprint));
        }
    }
}
