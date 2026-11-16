using NUnit.Framework;
using SS3D.Systems.Inventory.Containers;

namespace SS3D.Tests.EditMode
{
    /// <summary>Tech-debt P2.8: a body can't shoot, swing or interact during the stand-up after a knockdown (combat §5).</summary>
    public sealed class HandUseRuleTests
    {
        [Test]
        public void StandingUp_BlocksTheHand()
        {
            Assert.IsFalse(Hand.CanUse(armsWork: true, knockedDown: false, gettingUp: true));
        }

        [Test]
        public void KnockedDownOrDisabledArms_BlockTheHand()
        {
            Assert.IsFalse(Hand.CanUse(armsWork: true, knockedDown: true, gettingUp: false));
            Assert.IsFalse(Hand.CanUse(armsWork: false, knockedDown: false, gettingUp: false));
        }

        [Test]
        public void Upright_WithWorkingArms_CanUseTheHand()
        {
            Assert.IsTrue(Hand.CanUse(armsWork: true, knockedDown: false, gettingUp: false));
        }
    }
}
