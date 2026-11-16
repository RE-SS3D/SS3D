#if UNITY_EDITOR
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Health.Interactions;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Repeatable import of the Doctor's gauze and portable defibrillator from the FBXs written by
    /// Tools/export_item_meshes.py. Gauze copies the medical patch (it keeps <see cref="BandageItemExtension"/>);
    /// the defibrillator copies the health scanner and gains <see cref="DefibrillatorItemExtension"/>.
    /// </summary>
    public static class MedicalItemPrefabSetup
    {
        public const string GauzePath = Medications + "Gauze.prefab";
        public const string DefibrillatorPath = MedicalTools + "PortableDefibrillator.prefab";

        private const string Medications = "Assets/Content/WorldObjects/Items/Consumable/Chemical/Medications/";
        private const string MedicalTools = "Assets/Content/WorldObjects/Items/Functional/Tools/Medical/";
        private const string MedicationsArt = "Assets/Art/Models/Items/Consumable/Chemical/Medications/";
        private const string MedicalToolsArt = "Assets/Art/Models/Items/Functional/Tools/Medical/";

        [MenuItem("SS3D/Health/Run Medical Item Recipes")]
        public static void RunMenu()
        {
            int updated = SetupAll();
            EditorUtility.DisplayDialog("Medical Item Recipes", $"Updated {updated} prefab(s).", "OK");
        }

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Inventory.Containers.Editor.MedicalItemPrefabSetup.SetupBatch</c></summary>
        public static void SetupBatch()
        {
            Debug.Log($"[MedicalItemPrefabSetup] Updated {SetupAll()} prefabs.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        public static int SetupAll()
        {
            Build("Gauze", "Gauze", MedicationsArt, Medications + "MedicalPatch.prefab", GauzePath,
                "Items/Consumables/Chemical", "items.medical.gauze",
                "Assets/Content/Data/Examine/String/Items/Consumable/Chemical/Medications/Gauze.asset",
                "A roll of gauze. Use it on a bleeding body part to stop the bleeding.", 0.1f, false);
            Build("PortableDefibrillator", "Defibrillator", MedicalToolsArt, MedicalTools + "HealthScanner.prefab",
                DefibrillatorPath, "Items/Functional/Medical", "items.medical.defibrillator",
                "Assets/Content/Data/Examine/String/Items/Functional/Tools/Medical/PortableDefibrillator.asset",
                "A portable defibrillator. Use it on the chest of a patient whose heart has stopped, before the brain dies. "
                + "Shocking a beating heart burns it.", 2.0f, true);
            DB.SaveAssets();
            return 2;
        }

        private static void Build(string name, string displayName, string art, string template, string path,
            string mapFolder, string key, string examinePath, string description, float weight, bool defibrillator)
        {
            Mesh mesh = ImportedItemPrefab.BakeMesh(art + name + ".fbx", name, art + name + "Mesh.asset", true,
                out _, out Material[] sourceMaterials);
            ObjectAssetReference reference = ImportedItemPrefab.EnsurePrefab(template, path, name);
            Quaternion gripRotation = DB.LoadAssetAtPath<GameObject>(template).GetComponent<Item>().AttachmentPoint.localRotation;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = name;
                ImportedItemPrefab.ReplaceVisual(root, mesh, ImportedItemPrefab.PaletteMaterials(sourceMaterials), "Attachment");
                Transform grip = ImportedItemPrefab.EnsureChild(root, "Attachment");
                // The defibrillator is carried by the handle on its top; the gauze roll is simply held.
                grip.localPosition = defibrillator ? new Vector3(0f, mesh.bounds.max.y, 0f) : Vector3.zero;
                grip.localRotation = gripRotation;
                ImportedItemPrefab.SetItem(root, reference, displayName, weight, grip, Vector3.zero, defibrillator, null);
                if (defibrillator && !root.TryGetComponent(out DefibrillatorItemExtension _))
                    root.AddComponent<DefibrillatorItemExtension>();
                ImportedItemPrefab.WireExamine(root, key, examinePath, displayName, description);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            ImportedItemPrefab.Register(reference, path, name, "Assets/Content/Data/TileMap/Resources/" + mapFolder);
        }
    }
}
#endif
