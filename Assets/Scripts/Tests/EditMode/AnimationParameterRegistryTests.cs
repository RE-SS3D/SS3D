using NUnit.Framework;
using SS3D.Systems.Entities.Data;
using SS3D.Systems.Entities.Humanoid.Body;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Validates animator parameter hashes match expected names in HumanCharacterAnimator.controller.
    /// </summary>
    public class AnimationParameterRegistryTests
    {
        [Test]
        public void HumanoidParameterHashes_AreNonZero()
        {
            Assert.AreNotEqual(0, Animations.Humanoid.MovementSpeed);
            Assert.AreNotEqual(0, Animations.Humanoid.Floating);
            Assert.AreNotEqual(0, Animations.Humanoid.LimpSide);
            Assert.AreNotEqual(0, Animations.Humanoid.ArmHold);
            Assert.AreNotEqual(0, Animations.Humanoid.CombatMode);
            Assert.AreNotEqual(0, Animations.Humanoid.AttackSwing);
            Assert.AreNotEqual(0, Animations.Humanoid.AttackStab);
            Assert.AreNotEqual(0, Animations.Humanoid.Throw);
            Assert.AreNotEqual(0, Animations.Humanoid.Emote);
            Assert.AreNotEqual(0, Animations.Humanoid.Flinch);
        }

        [Test]
        public void TriggerHashMapping_CoversAllTriggerIds()
        {
            Assert.AreNotEqual(0, Animations.Humanoid.GetTriggerHash(AnimationTriggerId.AttackSwing));
            Assert.AreNotEqual(0, Animations.Humanoid.GetTriggerHash(AnimationTriggerId.AttackStab));
            Assert.AreNotEqual(0, Animations.Humanoid.GetTriggerHash(AnimationTriggerId.Throw));
            Assert.AreNotEqual(0, Animations.Humanoid.GetTriggerHash(AnimationTriggerId.Emote));
            Assert.AreNotEqual(0, Animations.Humanoid.GetTriggerHash(AnimationTriggerId.Flinch));
            Assert.AreEqual(0, Animations.Humanoid.GetTriggerHash(AnimationTriggerId.None));
        }
    }
}
