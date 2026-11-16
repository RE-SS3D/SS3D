using NUnit.Framework;
using SS3D.Systems.Combat;

namespace EditorTests
{
    public class MeleeWeaponProfileStructuralForceTests
    {
        [Test]
        public void StaticProfiles_HavePositiveStructuralForce()
        {
            Assert.Greater(MeleeWeaponProfile.Fists.StructuralForce, 0f);
            Assert.Greater(MeleeWeaponProfile.Improvised.StructuralForce, 0f);
            Assert.Greater(MeleeWeaponProfile.Crowbar.StructuralForce, 0f);
            Assert.Greater(MeleeWeaponProfile.Hatchet.StructuralForce, 0f);
            Assert.Greater(MeleeWeaponProfile.KitchenKnife.StructuralForce, 0f);
        }

        [Test]
        public void Crowbar_HitsHarderThanFists()
        {
            Assert.Greater(MeleeWeaponProfile.Crowbar.StructuralForce, MeleeWeaponProfile.Fists.StructuralForce);
        }

        [Test]
        public void ResolveStructuralForce_FallsBackWhenUnset()
        {
            var profile = new MeleeWeaponProfile { BruteDamage = 20f, StructuralForce = 0f };
            Assert.AreEqual(15f, profile.ResolveStructuralForce(), 0.01f);
        }
    }
}
