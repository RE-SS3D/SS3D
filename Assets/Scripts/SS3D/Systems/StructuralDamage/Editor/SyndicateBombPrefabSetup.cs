#if UNITY_EDITOR
using System.Linq;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.StructuralDamage.Editor
{
    /// <summary>
    /// Repeatable recipe for the Syndicate bomb and its delivery beacon, run by <see cref="ExplosivePrefabSetup.SetupAll"/>
    /// on the structural damage aggregator. Meshes come from <c>Tools/export_syndicate_bomb.py</c>.
    /// </summary>
    public static class SyndicateBombPrefabSetup
    {
        public const string BombPrefab = "Assets/Content/WorldObjects/Items/Weapons/SyndicateBomb.prefab";
        public const string BeaconPrefab = "Assets/Content/WorldObjects/Items/Weapons/SyndicateBombBeacon.prefab";
        private const string BasePrefab = "Assets/Content/WorldObjects/Items/Functional/Tools/Engineering/Wirecutters.prefab";

        public static void SetupAll()
        {
            string ratchet = ExplosivePrefabSetup.RegisterSound("WrenchRatchet");
            string arrival = ExplosivePrefabSetup.RegisterSound("BeaconArrival");
            ObjectAssetReference bomb = Build("SyndicateBomb", BombPrefab, root =>
            {
                SetItem(root, "Syndicate bomb", 100f, SizeClass.Huge, tooHeavy: true, Vector3.zero);
                root.GetComponent<Rigidbody>().mass = 100f;
                if (!root.TryGetComponent(out Draggable _)) root.AddComponent<Draggable>();
                if (!root.TryGetComponent(out SyndicateBomb component)) component = root.AddComponent<SyndicateBomb>();
                SerializedObject so = new(component);
                so.FindProperty("_wrenchSoundId").stringValue = ratchet;
                so.FindProperty("_lamp").objectReferenceValue = Lamp(root);
                so.ApplyModifiedPropertiesWithoutUndo();
                WireExamine(root, "SyndicateBomb", "Syndicate bomb",
                    "A huge, heavy Syndicate explosive: too big to lift, so it has to be dragged into place. Set its timer "
                    + "(90 seconds at least) and arm it, then bolt it to the floor with a wrench; once armed the bolts "
                    + "lock, so nobody can drag it off. It beeps loudly enough to hear from the next room. Defusing takes "
                    + "a screwdriver to open its wiring panel, then wirecutters.");
            });
            Build("SyndicateBombBeacon", BeaconPrefab, root =>
            {
                // Lies on its broad side, like the other long items.
                SetItem(root, "Syndicate bomb beacon", 0.3f, SizeClass.Small, tooHeavy: false, new Vector3(-90f, 0f, 0f));
                if (!root.TryGetComponent(out SyndicateBombBeacon component)) component = root.AddComponent<SyndicateBombBeacon>();
                SerializedObject so = new(component);
                so.FindProperty("_bombAsset").objectReferenceValue = bomb;
                so.FindProperty("_arrivalSoundId").stringValue = arrival;
                so.ApplyModifiedPropertiesWithoutUndo();
                WireExamine(root, "SyndicateBombBeacon", "Syndicate bomb beacon",
                    "A one-use Syndicate beacon. Use it in hand and a Syndicate bomb lands in front of you; the bomb is too "
                    + "heavy to carry, so only call it down where you mean to use it.");
            });
            DB.SaveAssets();
        }

        /// <summary>
        /// BatchMode: <c>-executeMethod SS3D.Systems.StructuralDamage.Editor.SyndicateBombPrefabSetup.SetupBatch</c>.
        /// Builds both prefabs and their collision sounds. Then run <c>JobKitSetup.Apply</c> (operative kit) and
        /// <c>ExamineIdentificationKeySetup.AddExamineFeedbackKeys</c> (examine and action-line strings) in their own assemblies.
        /// </summary>
        public static void SetupBatch()
        {
            SetupAll();
            Audio.Editor.AudioContentPrefabRecipes.SetupItemCollisions();
            DB.SaveAssets();
            Debug.Log("[SyndicateBombPrefabSetup] done");
            if (UnityEngine.Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static ObjectAssetReference Build(string name, string prefabPath, System.Action<GameObject> configure)
        {
            Mesh mesh = ExplosivePrefabSetup.BakeMesh(name);
            if (DB.LoadAssetAtPath<GameObject>(prefabPath) == null) DB.CopyAsset(BasePrefab, prefabPath);
            ObjectAssetReference reference = ExplosivePrefabSetup.Reference(name, prefabPath, "Items", "c2644d7b34e956743a0bc485e7ca5e37");
            // The map editor library and saved maps only know items with an ItemObjectSo.
            Tile.ItemObjectSo mapItem = ExplosivePrefabSetup.Ensure<Tile.ItemObjectSo>(
                "Assets/Content/Data/TileMap/Resources/Items/Weapons/" + name + ".asset");
            mapItem.PrefabAsset = reference;
            EditorUtility.SetDirty(mapItem);
            Material palette = DB.LoadAssetAtPath<Material>("Assets/Content/WorldObjects/World/Materials/Palette.mat");
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                root.name = name;
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                    if (child.name != "Attachment" && child.name != "Lamp") Object.DestroyImmediate(child.gameObject);
                root.GetComponent<MeshFilter>().sharedMesh = mesh;
                root.GetComponent<MeshRenderer>().sharedMaterials = Enumerable.Repeat(palette, mesh.subMeshCount).ToArray();
                BoxCollider collider = root.GetComponent<BoxCollider>();
                collider.center = mesh.bounds.center;
                collider.size = mesh.bounds.size;
                Transform grip = root.transform.Find("Attachment");
                if (grip != null) { grip.localPosition = Vector3.zero; grip.localRotation = Quaternion.identity; }
                SerializedObject item = new(root.GetComponent<Item>());
                item.FindProperty("_asset").objectReferenceValue = reference;
                item.ApplyModifiedPropertiesWithoutUndo();
                configure(root);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return reference;
        }

        private static void SetItem(GameObject root, string displayName, float weight, SizeClass size, bool tooHeavy, Vector3 rest)
        {
            SerializedObject item = new(root.GetComponent<Item>());
            item.FindProperty("_name").stringValue = displayName;
            item.FindProperty("_weight").floatValue = weight;
            item.FindProperty("_sizeClass").enumValueIndex = (int)size;
            item.FindProperty("_tooHeavyToLift").boolValue = tooHeavy;
            item.FindProperty("_restRotation").vector3Value = rest;
            item.FindProperty("_restsUpright").boolValue = false;
            item.ApplyModifiedPropertiesWithoutUndo();
            // The prefab root carries the rest pose (ItemPoseTests).
            root.transform.localRotation = Quaternion.Euler(rest);
        }

        /// <summary>A small red light over the core that flashes with each countdown beep.</summary>
        private static Light Lamp(GameObject root)
        {
            Transform existing = root.transform.Find("Lamp");
            GameObject lamp = existing != null ? existing.gameObject : new GameObject("Lamp");
            lamp.transform.SetParent(root.transform, false);
            Bounds bounds = root.GetComponent<MeshFilter>().sharedMesh.bounds;
            lamp.transform.localPosition = new Vector3(bounds.center.x, bounds.max.y + 0.12f, bounds.center.z);
            if (!lamp.TryGetComponent(out Light light)) light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.78f, 0f, 0.1f);
            light.range = 2.5f;
            light.intensity = 3f;
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }

        private static void WireExamine(GameObject root, string name, string title, string description)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection("Examine");
            StringTable table = (StringTable)collection.GetTable(new LocaleIdentifier("en"));
            string key = "items.weapons." + name.ToLowerInvariant();
            if (table.GetEntry(key + ".name") is { } nameEntry) nameEntry.Value = title; else table.AddEntry(key + ".name", title);
            if (table.GetEntry(key + ".desc") is { } descEntry) descEntry.Value = description; else table.AddEntry(key + ".desc", description);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);
            ExamineData data = ExplosivePrefabSetup.Ensure<ExamineData>("Assets/Content/Data/Examine/String/Items/Weapons/" + name + ".asset");
            data.Name = new LocalizedString("Examine", key + ".name");
            data.Description = new LocalizedString("Examine", key + ".desc");
            EditorUtility.SetDirty(data);
            SerializedObject so = new(root.GetComponent<SimpleExaminable>());
            so.FindProperty("key").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
