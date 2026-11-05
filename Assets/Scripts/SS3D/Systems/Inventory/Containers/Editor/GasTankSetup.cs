#if UNITY_EDITOR
using System.Linq;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Roles;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>Repeatable content recipe for gas tanks, the breathing mask and crew loadouts (armor.md §3).</summary>
    public static class GasTankSetup
    {
        private const string Tanks = "Assets/Content/WorldObjects/Items/Functional/Generic/Tanks/";
        private const string References = "Assets/Content/Data/ObjectAssetReferences/";
        private const string Loadouts = "Assets/Content/Data/Roles/Loadouts/";
        private const string GasMaskPath = "Assets/Content/WorldObjects/Items/Clothing/FacewearGasMask.prefab";
        private const string ItemsDatabase = "c2644d7b34e956743a0bc485e7ca5e37";
        private const float FullTankMoles = 45f;
        private const float EmergencyTankMoles = 9f;
        private const float FullTankWeight = 5f;
        private const float EmergencyTankWeight = 1f;

        public static int SetupAll()
        {
            Configure(Tanks + "OxygenTank.prefab", SizeClass.Huge, FullTankWeight, new GasTankMix { OxygenMoles = FullTankMoles });
            Configure(Tanks + "PlasmaTank.prefab", SizeClass.Huge, FullTankWeight, new GasTankMix { PlasmaMoles = FullTankMoles });
            ObjectAssetReference emergency = BuildEmergencyTank();

            GameObject mask = PrefabUtility.LoadPrefabContents(GasMaskPath);
            try
            {
                if (mask.GetComponent<BreathMaskItemExtension>() == null) mask.AddComponent<BreathMaskItemExtension>();
                SetSize(mask, SizeClass.Small);
                PrefabUtility.SaveAsPrefabAsset(mask, GasMaskPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(mask); }

            var fullTank = DB.LoadAssetAtPath<ObjectAssetReference>(References + "OxygenTank.asset");
            var gasMask = DB.LoadAssetAtPath<ObjectAssetReference>(References + "FacewearGasMask.asset");
            var operative = DB.LoadAssetAtPath<RoleLoadout>(Loadouts + "OperativeLoadout.asset");
            operative.EquipmentAssets[ContainerType.SuitStorage] = fullTank;
            EditorUtility.SetDirty(operative);
            foreach (string crew in new[] { "AssistantLoadout.asset", "SecurityLoadout.asset" })
            {
                var loadout = DB.LoadAssetAtPath<RoleLoadout>(Loadouts + crew);
                loadout.PocketAssets = new() { emergency, gasMask };
                EditorUtility.SetDirty(loadout);
            }

            DB.SaveAssets();
            return 4;
        }

        private static void Configure(string path, SizeClass size, float weight, GasTankMix contents, string name = null)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                SetSize(root, size);
                var item = new SerializedObject(root.GetComponent<Item>());
                item.FindProperty("_weight").floatValue = weight;
                if (name != null) item.FindProperty("_name").stringValue = name;
                item.ApplyModifiedPropertiesWithoutUndo();

                var tank = root.GetComponent<GasTankItemExtension>();
                if (tank == null) tank = root.AddComponent<GasTankItemExtension>();
                var so = new SerializedObject(tank);
                so.FindProperty("_contents.OxygenMoles").floatValue = contents.OxygenMoles;
                so.FindProperty("_contents.PlasmaMoles").floatValue = contents.PlasmaMoles;
                so.ApplyModifiedPropertiesWithoutUndo();

                StorageContainerPrefabSetup.RebuildNetworkBehaviours(root.GetComponent<FishNet.Object.NetworkObject>());
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void SetSize(GameObject root, SizeClass size)
        {
            var item = new SerializedObject(root.GetComponent<Item>());
            item.FindProperty("_sizeClass").enumValueIndex = (int)size;
            item.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ObjectAssetReference BuildEmergencyTank()
        {
            const string name = "EmergencyOxygenTank";
            string path = Tanks + name + ".prefab";
            if (DB.LoadAssetAtPath<GameObject>(path) == null) DB.CopyAsset(Tanks + "OxygenTank.prefab", path);

            string referencePath = References + name + ".asset";
            var reference = DB.LoadAssetAtPath<ObjectAssetReference>(referencePath);
            if (reference == null)
            {
                reference = ScriptableObject.CreateInstance<ObjectAssetReference>();
                DB.CreateAsset(reference, referencePath);
            }

            reference.Id = DB.AssetPathToGUID(path);
            reference.Database = ItemsDatabase;
            EditorUtility.SetDirty(reference);

            Mesh mesh = DB.LoadAllAssetsAtPath("Assets/Art/Models/Items/Functional/Generic/AirTanks.fbx")
                .OfType<Mesh>()
                .First(candidate => candidate.name == "TankOxygenEmergency");
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = name;
                root.GetComponent<MeshFilter>().sharedMesh = mesh;
                var box = root.GetComponent<BoxCollider>();
                box.center = mesh.bounds.center;
                box.size = mesh.bounds.size;
                var item = new SerializedObject(root.GetComponent<Item>());
                item.FindProperty("_asset").objectReferenceValue = reference;
                item.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            Configure(path, SizeClass.Small, EmergencyTankWeight, new GasTankMix { OxygenMoles = EmergencyTankMoles }, "Emergency Oxygen Tank");

            var db = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/Items.asset");
            AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(reference.Id, db.AssetGroup).address = name;
            db.Assets[reference.Id] = DB.LoadAssetAtPath<GameObject>(path);
            EditorUtility.SetDirty(db);
            return reference;
        }
    }
}
#endif
