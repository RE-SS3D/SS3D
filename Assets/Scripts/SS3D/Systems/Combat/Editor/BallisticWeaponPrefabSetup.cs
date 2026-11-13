#if UNITY_EDITOR
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Containers.Editor;
using UnityEditor;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Combat.Editor
{
    /// <summary>
    /// Repeatable import of the M1c ballistic guns (C20R, Stechkin, security pistol) from the FBXs written by
    /// Tools/export_item_meshes.py. Each copies the M4's behaviour, takes its own profile, and keeps its
    /// magazine as a child the reload hands to the left hand. Registered by <see cref="RangedPrefabSetup"/>.
    /// </summary>
    public static class BallisticWeaponPrefabSetup
    {
        public const string C20RPath = Weapons + "C20R.prefab";
        public const string StechkinPath = Weapons + "Stechkin.prefab";
        public const string SecurityPistolPath = Weapons + "SecurityPistol.prefab";

        private const string Weapons = "Assets/Content/WorldObjects/Items/Weapons/";
        private const string Art = "Assets/Art/Models/Items/Weapons/Ranged/Physical/";

        /// <summary>SS14 clips for the C-20r, Stechkin and security pistol (<see cref="CombatAudioTrackIds.SoundsFor"/>).</summary>
        private static readonly string[] Sounds =
        {
            "C20RFire", "SmgMagOut", "SmgMagIn", "SmgCock", "PistolFire", "Mk58Fire", "PistolMagOut", "PistolMagIn", "PistolCock",
        };

        public static int SetupAll()
        {
            foreach (string sound in Sounds)
                ImportedItemPrefab.RegisterSound("Assets/Art/Sound/Items/Weapons/Firearms/SS14/" + sound + ".ogg");
            Build("C20R", "C-20r SMG", "Rifles/", RangedWeaponProfile.C20R, 2.4f,
                "A compact Syndicate submachine gun. Fast and light-hitting, it needs both hands. Use to reload.");
            Build("Stechkin", "Stechkin Pistol", "Handguns/", RangedWeaponProfile.Stechkin, 0.8f,
                "A Syndicate pistol: small, one-handed and hard-hitting. Use to reload.");
            Build("SecurityPistol", "Security Pistol", "Handguns/", RangedWeaponProfile.SecurityPistol, 0.8f,
                "The standard Security sidearm. One-handed, with a ten-round magazine. Use to reload.");
            DB.SaveAssets();
            return 3;
        }

        private static void Build(string name, string displayName, string category, RangedWeaponProfile profile,
            float weight, string description)
        {
            string model = Art + category + name + ".fbx";
            Mesh mesh = ImportedItemPrefab.BakeMesh(model, name, Art + category + name + "WeaponMesh.asset", false,
                out _, out Material[] sourceMaterials);
            Mesh magazineMesh = ImportedItemPrefab.BakeMesh(model, name + "Magazine",
                Art + category + name + "MagazineMesh.asset", true, out Vector3 magazineOffset, out Material[] magazineMaterials);
            string path = Weapons + name + ".prefab";
            ObjectAssetReference reference = ImportedItemPrefab.EnsurePrefab(Weapons + "M4.prefab", path, name);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = name;
                ImportedItemPrefab.ReplaceVisual(root, mesh, ImportedItemPrefab.PaletteMaterials(sourceMaterials),
                    "Attachment", "Muzzle", name + "Magazine");
                ImportedItemPrefab.EnsureMeshChild(root, name + "Magazine", magazineMesh,
                    ImportedItemPrefab.PaletteMaterials(magazineMaterials), magazineOffset);
                Bounds bounds = mesh.bounds;
                Transform grip = ImportedItemPrefab.EnsureChild(root, "Attachment");
                grip.localPosition = new Vector3(0f, bounds.min.y + bounds.size.y * 0.25f, bounds.min.z + bounds.size.z * 0.30f);
                Transform muzzle = ImportedItemPrefab.EnsureChild(root, "Muzzle");
                // The source barrel points down Blender -Y, which the FBX export turns into +Z.
                muzzle.localPosition = new Vector3(0f, bounds.max.y - bounds.size.y * 0.2f, bounds.max.z);
                muzzle.localRotation = Quaternion.identity;
                // ContainerItemDisplay applies the authored grip directly; match the M4's barrel basis in the hand.
                GameObject m4 = DB.LoadAssetAtPath<GameObject>(Weapons + "M4.prefab");
                Transform m4Grip = m4.GetComponent<Item>().AttachmentPoint;
                Transform m4Muzzle = m4.GetComponent<RangedWeaponItemExtension>().Muzzle;
                grip.localRotation = m4Grip.localRotation * m4Muzzle.localRotation * Quaternion.Inverse(muzzle.localRotation);
                // Lying on its side: the thin X axis ends up vertical. The icon shows the side profile.
                ImportedItemPrefab.SetItem(root, reference, displayName, weight, grip, new Vector3(0f, 0f, 90f), false, Vector3.zero);
                SerializedObject extension = new(root.GetComponent<RangedWeaponItemExtension>());
                extension.FindProperty("_magazine").objectReferenceValue = root.transform.Find(name + "Magazine");
                extension.ApplyModifiedPropertiesWithoutUndo();
                RangedPrefabSetup.Configure(root, profile);
                ImportedItemPrefab.WireExamine(root, "items.weapons." + name.ToLowerInvariant(),
                    "Assets/Content/Data/Examine/String/Items/Weapons/" + name + ".asset", displayName, description);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            ImportedItemPrefab.Register(reference, path, name, "Assets/Content/Data/TileMap/Resources/Items/Weapons");
        }
    }
}
#endif
