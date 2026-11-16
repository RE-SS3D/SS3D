using NUnit.Framework;
using SS3D.Systems.Entities.Humanoid.Body;

namespace SS3D.Tests.EditMode
{
    /// <summary>Tech-debt P1.6: a client can't make its body play combat one-shots the server never accepted.</summary>
    public sealed class ClientTriggerRulesTests
    {
        private sealed class Gate : IClientTriggerGate
        {
            public bool Accepts;
            public bool ServerAcceptsClientTrigger(AnimationTriggerId trigger) => Accepts;
        }

        [Test]
        public void Emote_IsAlwaysAllowed()
        {
            Assert.IsTrue(ClientTriggerRules.MayClientStart(AnimationTriggerId.Emote, null));
        }

        [TestCase(AnimationTriggerId.Flinch)]
        [TestCase(AnimationTriggerId.None)]
        public void ServerOnlyTriggers_AreRefused(AnimationTriggerId trigger)
        {
            Assert.IsFalse(ClientTriggerRules.MayClientStart(trigger, new Gate { Accepts = true }));
        }

        [TestCase(AnimationTriggerId.AttackSwing)]
        [TestCase(AnimationTriggerId.AttackStab)]
        [TestCase(AnimationTriggerId.Throw)]
        [TestCase(AnimationTriggerId.FireRifle)]
        [TestCase(AnimationTriggerId.Reload)]
        public void CombatTriggers_NeedTheServersAction(AnimationTriggerId trigger)
        {
            Assert.IsTrue(ClientTriggerRules.MayClientStart(trigger, new Gate { Accepts = true }));
            Assert.IsFalse(ClientTriggerRules.MayClientStart(trigger, new Gate { Accepts = false }));
            Assert.IsFalse(ClientTriggerRules.MayClientStart(trigger, null));
        }
    }
}
