#if UNITY_EDITOR
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Combat.Interactions;
using SS3D.Systems.Examine;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Containers.Editor;
using UnityEditor;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Combat.Editor
{
    /// <summary>Tier-B flamethrower recipe, registered on the combat content aggregator.</summary>
    public static class FlamethrowerPrefabSetup
    {
        public const string PrefabPath = "Assets/Content/WorldObjects/Items/Weapons/Flamethrower.prefab";
        private const string Art = "Assets/Art/Models/Items/Weapons/Ranged/Energy/Flamethrower";

        public static int SetupAll()
        {
            ImportedItemPrefab.RegisterSound("Assets/Art/Sound/Items/Weapons/Flamethrower/FlameJet.ogg");
            Mesh mesh = ImportedItemPrefab.BakeMesh(Art + ".fbx", "Flamethrower", Art + "WeaponMesh.asset", false,
                out _, out Material[] materials);
            mesh.name = "Flamethrower";
            EditorUtility.SetDirty(mesh);
            const string template = "Assets/Content/WorldObjects/Items/Weapons/M4.prefab";
            ObjectAssetReference reference = ImportedItemPrefab.EnsurePrefab(template, PrefabPath, "Flamethrower");
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                root.name = "Flamethrower";
                ImportedItemPrefab.ReplaceVisual(root, mesh, ImportedItemPrefab.PaletteMaterials(materials), "Attachment", "Muzzle");
                Bounds bounds = mesh.bounds;
                Transform grip = ImportedItemPrefab.EnsureChild(root, "Attachment");
                grip.localPosition = new Vector3(0f, bounds.min.y + bounds.size.y * 0.25f, bounds.min.z + bounds.size.z * 0.3f);
                Transform muzzle = ImportedItemPrefab.EnsureChild(root, "Muzzle");
                muzzle.localPosition = new Vector3(0f, bounds.max.y - bounds.size.y * 0.2f, bounds.max.z);
                muzzle.localRotation = Quaternion.identity;
                GameObject m4 = DB.LoadAssetAtPath<GameObject>(template);
                grip.localRotation = m4.GetComponent<Item>().AttachmentPoint.localRotation
                    * m4.GetComponent<RangedWeaponItemExtension>().Muzzle.localRotation;
                ImportedItemPrefab.SetItem(root, reference, "Flamethrower", 3.5f, grip, new Vector3(0f, 0f, 90f), false, Vector3.zero);
                RangedPrefabSetup.Configure(root, RangedWeaponProfile.Flamethrower);
                SerializedObject weapon = new(root.GetComponent<RangedWeaponItemExtension>());
                weapon.FindProperty("_magazine").objectReferenceValue = null;
                weapon.ApplyModifiedPropertiesWithoutUndo();
                ImportedItemPrefab.WireExamine(root, "items.weapons.flamethrower",
                    "Assets/Content/Data/Examine/String/Items/Weapons/Flamethrower.asset", "Flamethrower",
                    "A two-handed flame projector. Hold primary in Harm to spray a wide fan of burning fuel up to four metres. Fuel sticks to people and surfaces, burns for six seconds and blackens surfaces. Holds sixty fuel pulses. Use to refill in four seconds.");
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            ImportedItemPrefab.Register(reference, PrefabPath, "Flamethrower", "Assets/Content/Data/TileMap/Resources/Items/Weapons");
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection("Examine");
            StringTable table = collection.GetTable(new LocaleIdentifier("en")) as StringTable;
            table.AddEntry(ExamineWeaponKeys.Fuel, ExamineWeaponKeys.FuelFallback);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
            DB.SaveAssets();
            return 1;
        }
    }
}
#endif
