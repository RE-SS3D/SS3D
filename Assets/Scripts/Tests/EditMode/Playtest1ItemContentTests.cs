using System.Linq;
using NUnit.Framework;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Combat;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Construction;
using SS3D.Systems.Examine;
using SS3D.Systems.Health.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Roles;
using SS3D.Systems.Tile;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// The Playtest 1 items imported from SS3D-Art (Tools/export_item_meshes.py and their prefab recipes) are
    /// registered content with their own art, and the job kits hand them out.
    /// </summary>
    public class Playtest1ItemContentTests
    {
        private const string Items = "Assets/Content/WorldObjects/Items/";

        private static GameObject Prefab(string path)
        {
            GameObject prefab = DB.LoadAssetAtPath<GameObject>(Items + path + ".prefab");
            Assert.IsNotNull(prefab, path);
            return prefab;
        }

        [TestCase("Weapons/C20R", "Weapons")]
        [TestCase("Weapons/Stechkin", "Weapons")]
        [TestCase("Weapons/SecurityPistol", "Weapons")]
        [TestCase("Weapons/StunBaton", "Weapons")]
        [TestCase("Consumable/Chemical/Medications/Gauze", "Consumables/Chemical")]
        [TestCase("Functional/Tools/Medical/PortableDefibrillator", "Functional/Medical")]
        public void Item_IsRegisteredWithItsOwnArt(string path, string mapFolder)
        {
            GameObject prefab = Prefab(path);
            string name = path.Substring(path.LastIndexOf('/') + 1);
            string guid = DB.AssetPathToGUID(Items + path + ".prefab");
            var database = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/Items.asset");
            Assert.AreSame(prefab, database.Assets[guid], "Items database row");
            Item item = prefab.GetComponent<Item>();
            Assert.AreEqual(guid, item.Asset.Id, "The item's own reference, not its template's");
            ItemObjectSo mapItem = DB.LoadAssetAtPath<ItemObjectSo>("Assets/Content/Data/TileMap/Resources/Items/" + mapFolder + "/" + name + ".asset");
            Assert.IsNotNull(mapItem, "Map editor entry");
            Assert.AreEqual(guid, mapItem.PrefabAsset.Id);
            Mesh mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
            StringAssert.StartsWith(name, mesh.name);
            StringAssert.StartsWith("Assets/Art/Models/", DB.GetAssetPath(mesh));
            Assert.That(mesh.bounds.size.magnitude, Is.InRange(0.15f, 1.2f), "Source units or studio geometry leaked into the export");
            Assert.Less(Quaternion.Angle(item.WorldRestRotation, prefab.transform.rotation), 0.01f, "Root rotation equals the rest pose");
            Assert.IsNotNull(item.AttachmentPoint);
            Assert.IsNotNull(prefab.GetComponent<SimpleExaminable>().GetData());
        }

        [TestCase("C20R", WeaponHoldStyle.Rifle)]
        [TestCase("Stechkin", WeaponHoldStyle.Pistol)]
        [TestCase("SecurityPistol", WeaponHoldStyle.Pistol)]
        public void Gun_CarriesItsProfileMagazineAndM4Grip(string name, WeaponHoldStyle hold)
        {
            GameObject prefab = Prefab("Weapons/" + name);
            RangedWeaponItemExtension weapon = prefab.GetComponent<RangedWeaponItemExtension>();
            RangedWeaponProfile code = (RangedWeaponProfile)typeof(RangedWeaponProfile).GetProperty(name).GetValue(null);
            Assert.AreEqual(hold, weapon.Profile.HoldStyle);
            Assert.AreEqual(code.MagazineSize, weapon.Profile.MagazineSize);
            Assert.AreEqual(code.BruteDamage, weapon.Profile.BruteDamage);
            Assert.AreEqual(code.RequiresBothHands, weapon.Profile.RequiresBothHands);

            Assert.IsNotNull(weapon.Magazine, "The reload hands a magazine to the left hand");
            Assert.AreEqual(name + "Magazine", weapon.Magazine.name);
            Bounds gun = prefab.GetComponent<MeshFilter>().sharedMesh.bounds;
            Bounds magazine = weapon.Magazine.GetComponent<MeshFilter>().sharedMesh.bounds;
            magazine.center += weapon.Magazine.localPosition;
            Assert.IsTrue(gun.Intersects(magazine), "The magazine is seated in the gun, where the source puts it");

            Assert.AreEqual(gun.max.z, weapon.Muzzle.localPosition.z, 0.001f, "Muzzle at the barrel tip");
            GameObject m4 = Prefab("Weapons/M4");
            Quaternion expected = m4.GetComponent<Item>().AttachmentPoint.localRotation
                * m4.GetComponent<RangedWeaponItemExtension>().Muzzle.localRotation;
            Quaternion actual = prefab.GetComponent<Item>().AttachmentPoint.localRotation * weapon.Muzzle.localRotation;
            Assert.Less(Quaternion.Angle(expected, actual), 0.01f, "The barrel matches the M4 in the hand display");
        }

        [TestCase("M4", FirearmSoundSet.Rifle)]
        [TestCase("C20R", FirearmSoundSet.Smg)]
        [TestCase("Stechkin", FirearmSoundSet.Pistol)]
        [TestCase("SecurityPistol", FirearmSoundSet.ServicePistol)]
        public void Gun_PlaysItsOwnSoundsFromTheSoundDatabase(string name, FirearmSoundSet set)
        {
            Assert.AreEqual(set, Prefab("Weapons/" + name).GetComponent<RangedWeaponItemExtension>().Profile.SoundSet);
            var sounds = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/Sounds.asset");
            FirearmSounds clips = CombatAudioTrackIds.SoundsFor(set);
            foreach (string id in clips.Fire) Assert.IsInstanceOf<AudioClip>(sounds.Assets[id], name + " fire");
            Assert.IsInstanceOf<AudioClip>(sounds.Assets[clips.MagazineOut], name + " magazine out");
            Assert.IsInstanceOf<AudioClip>(sounds.Assets[clips.MagazineIn], name + " magazine in");
            Assert.IsInstanceOf<AudioClip>(sounds.Assets[clips.Cock], name + " cock");
        }

        [Test]
        public void EachNewGun_SoundsDifferentFromTheRifle()
        {
            string[] rifle = CombatAudioTrackIds.SoundsFor(FirearmSoundSet.Rifle).Fire;
            foreach (FirearmSoundSet set in new[] { FirearmSoundSet.Smg, FirearmSoundSet.Pistol, FirearmSoundSet.ServicePistol })
                CollectionAssert.IsEmpty(CombatAudioTrackIds.SoundsFor(set).Fire.Intersect(rifle), set.ToString());
        }

        [Test]
        public void Pistols_AreOneHanded_AndTheC20RIsNot()
        {
            Assert.IsFalse(RangedWeaponProfile.Stechkin.RequiresBothHands);
            Assert.IsFalse(RangedWeaponProfile.SecurityPistol.RequiresBothHands);
            Assert.IsTrue(RangedWeaponProfile.C20R.RequiresBothHands);
            Assert.Greater(RangedWeaponProfile.C20R.MagazineSize, RangedWeaponProfile.SecurityPistol.MagazineSize);
        }

        [Test]
        public void StunBaton_IsAContactStun_AndNotAPryingTool()
        {
            GameObject prefab = Prefab("Weapons/StunBaton");
            Assert.IsNull(prefab.GetComponent<ConstructionTool>(), "Copied from the crowbar, but it must not pry");
            MeleeWeaponProfile profile = prefab.GetComponent<MeleeWeaponItemExtension>().Profile;
            MeleeWeaponProfile code = MeleeWeaponProfile.StunBaton;
            Assert.AreEqual(code.KnockdownSeconds, profile.KnockdownSeconds);
            Assert.AreEqual(code.StaminaDamage, profile.StaminaDamage);
            Assert.That(profile.KnockdownSeconds, Is.InRange(2f, 3f), "A stun baton hit is a knockdown (combat.md §5)");
            Assert.Zero(MeleeWeaponProfile.Crowbar.KnockdownSeconds);
            Assert.Zero(MeleeWeaponProfile.Fists.KnockdownSeconds);
        }

        [Test]
        public void MedicalItems_CarryTheirTreatment()
        {
            Assert.IsNotNull(Prefab("Consumable/Chemical/Medications/Gauze").GetComponent<BandageItemExtension>());
            GameObject defibrillator = Prefab("Functional/Tools/Medical/PortableDefibrillator");
            Assert.IsNotNull(defibrillator.GetComponent<DefibrillatorItemExtension>());
            Assert.IsNull(Prefab("Functional/Tools/Medical/HealthScanner").GetComponent<DefibrillatorItemExtension>(),
                "The template keeps its own behaviour");
        }

        [Test]
        public void JobKits_HandOutThePlaytestGear()
        {
            RoleLoadout operative = Loadout("Operative");
            Assert.AreEqual("C20R", operative.HandRightAsset.name);
            Assert.That(Storage(operative, ContainerType.Bag), Does.Contain("Stechkin"));
            RoleLoadout security = Loadout("Security");
            Assert.AreEqual("SecurityPistol", security.HandRightAsset.name);
            Assert.That(Storage(security, ContainerType.Bag), Does.Contain("StunBaton"));
            RoleLoadout doctor = Loadout("Doctor");
            Assert.That(Storage(doctor, ContainerType.Bag), Does.Contain("PortableDefibrillator"));
            Assert.That(doctor.HandLeftContents.Select(r => r.name), Does.Contain("Gauze"));
        }

        private static RoleLoadout Loadout(string job) =>
            DB.LoadAssetAtPath<RoleLoadout>("Assets/Content/Data/Roles/Loadouts/" + job + "Loadout.asset");

        private static string[] Storage(RoleLoadout loadout, ContainerType slot) =>
            loadout.EquipmentContents.Single(c => c.Slot == slot).Items.Select(r => r.name).ToArray();
    }
}
