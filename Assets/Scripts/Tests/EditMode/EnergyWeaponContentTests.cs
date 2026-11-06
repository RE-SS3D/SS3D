using NUnit.Framework;
using SS3D.Systems.Combat;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Tests.EditMode
{
    /// <summary>Imported weapons must be spawnable content, with their own mesh and live muzzle.</summary>
    public class EnergyWeaponContentTests
    {
        [TestCase("IonRifle", RangedVisualStyle.Ion)]
        [TestCase("LaserRifle", RangedVisualStyle.Laser)]
        [TestCase("Taser", RangedVisualStyle.Taser)]
        public void Content_IsRegisteredAndHasOwnPresentation(string name, RangedVisualStyle style)
        {
            string path = "Assets/Content/WorldObjects/Items/Weapons/" + name + ".prefab";
            GameObject prefab = DB.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);
            var database = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/Items.asset");
            Assert.AreSame(prefab, database.Assets[DB.AssetPathToGUID(path)]);
            RangedWeaponItemExtension weapon = prefab.GetComponent<RangedWeaponItemExtension>();
            Assert.AreEqual(style, weapon.Profile.VisualStyle);
            Assert.IsNotNull(weapon.Muzzle);
            Assert.AreEqual(Vector3.forward, weapon.Muzzle.localRotation * Vector3.forward);
            Assert.IsNull(weapon.Magazine, "Energy weapons must not inherit the M4 magazine handoff.");
            Mesh mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
            Assert.AreEqual(name, mesh.name);
            Assert.Less(mesh.bounds.size.z, 1.2f, "Studio geometry or source units leaked into the export.");
            Assert.Greater(mesh.bounds.size.z, 0.25f);
            Assert.AreEqual(mesh.bounds.max.z, weapon.Muzzle.localPosition.z, 0.001f);
            Assert.IsNotNull(prefab.GetComponent<SimpleExaminable>().GetData());
        }

        [TestCase("IonRifle")]
        [TestCase("LaserRifle")]
        [TestCase("Taser")]
        public void HeldBarrelBasis_MatchesM4(string name)
        {
            GameObject reference = DB.LoadAssetAtPath<GameObject>("Assets/Content/WorldObjects/Items/Weapons/M4.prefab");
            GameObject weapon = DB.LoadAssetAtPath<GameObject>("Assets/Content/WorldObjects/Items/Weapons/" + name + ".prefab");
            // ContainerItemDisplay rotates the pivot by character-facing * Attachment.localRotation,
            // then leaves the item at identity under that pivot. Compare both forward and roll.
            Quaternion expected = reference.GetComponent<Item>().AttachmentPoint.localRotation
                * reference.GetComponent<RangedWeaponItemExtension>().Muzzle.localRotation;
            Quaternion actual = weapon.GetComponent<Item>().AttachmentPoint.localRotation
                * weapon.GetComponent<RangedWeaponItemExtension>().Muzzle.localRotation;
            Assert.Less(Quaternion.Angle(expected, actual), 0.01f,
                "The barrel must match M4 in the hand display, including its up direction.");
        }

        [Test]
        public void Taser_IsStaminaOnlyAndCannotDamageStructures()
        {
            RangedWeaponProfile profile = RangedWeaponProfile.Taser;
            Assert.Zero(profile.BruteDamage);
            Assert.Zero(profile.BurnDamage);
            Assert.Zero(profile.ResolveStructuralForce());
            Assert.Greater(profile.StaminaDamage, 0f);
            Assert.IsFalse(profile.RequiresBothHands);
        }

        [Test]
        public void EnergyAudio_ResolvesToClipsInTheSoundDatabase()
        {
            var sounds = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/Sounds.asset");
            foreach (RangedVisualStyle style in new[] { RangedVisualStyle.Laser, RangedVisualStyle.Ion, RangedVisualStyle.Taser })
            {
                foreach (string id in CombatAudioTrackIds.FireFor(style)) Assert.IsInstanceOf<AudioClip>(sounds.Assets[id]);
            }
            foreach (string id in CombatAudioTrackIds.EnergyHit) Assert.IsInstanceOf<AudioClip>(sounds.Assets[id]);
            foreach (string id in CombatAudioTrackIds.EnergyFleshHit) Assert.IsInstanceOf<AudioClip>(sounds.Assets[id]);
            Assert.IsInstanceOf<AudioClip>(sounds.Assets[CombatAudioTrackIds.EnergyReady]);
        }
    }
}
