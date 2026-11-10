using NUnit.Framework;
using SS3D.Systems.Combat.Interactions;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Tech-debt P2.1: recoil is a pure function of time, so the HUD reading it every frame can't change a shot's spread
    /// (it used to: each read decayed recoil and dropped the cooldown time).
    /// </summary>
    public sealed class CombatReadoutTests
    {
        private const float Decay = 2f;

        [Test]
        public void Recoil_HoldsDuringTheCooldown()
        {
            Assert.AreEqual(3f, RangedWeaponItemExtension.RecoilAt(3f, since: 10f, cooldownUntil: 10.5f, Decay, now: 10.4f));
        }

        [Test]
        public void Recoil_DecaysFromWhenItWasSet_OnceTheCooldownEnds()
        {
            // Matches the server's old behaviour between shots: the cooldown time counts toward decay.
            Assert.AreEqual(2f, RangedWeaponItemExtension.RecoilAt(3f, since: 10f, cooldownUntil: 10.25f, Decay, now: 10.5f), 1e-5f);
            Assert.AreEqual(0f, RangedWeaponItemExtension.RecoilAt(3f, since: 10f, cooldownUntil: 10.25f, Decay, now: 20f));
        }

        [Test]
        public void ReadingTwice_GivesTheSameAnswer()
        {
            float first = RangedWeaponItemExtension.RecoilAt(3f, 10f, 10.25f, Decay, 11f);
            float second = RangedWeaponItemExtension.RecoilAt(3f, 10f, 10.25f, Decay, 11f);
            Assert.AreEqual(first, second);
        }

        [Test]
        public void NoRecoil_StaysZero()
        {
            Assert.AreEqual(0f, RangedWeaponItemExtension.RecoilAt(0f, 0f, 0f, Decay, 5f));
            Assert.AreEqual(0f, RangedWeaponItemExtension.RecoilAt(-1f, 0f, 10f, Decay, 5f));
        }
    }
}
