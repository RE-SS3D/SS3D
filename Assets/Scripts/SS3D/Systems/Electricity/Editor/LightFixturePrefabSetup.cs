#if UNITY_EDITOR
using System;
using System.Linq;
using FishNet.Object;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Containers.Editor;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Selection;
using SS3D.Systems.Tile;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Electricity.Editor
{
    /// <summary>
    /// Breakable, replaceable fixture bulbs (tier B): builds the loose bulb/tube items from <c>Lights.fbx</c> and
    /// wires <see cref="LightFixtureBulb"/> onto the bulb and tube fixtures.
    /// </summary>
    public static class LightFixturePrefabSetup
    {
        private const string ModelPath = "Assets/Art/Models/Structures/WallMounts/Lights.fbx";
        private const string FixtureFolder = "Assets/Content/WorldObjects/Structures/WallMounts/Lights/";
        private const string ItemFolder = "Assets/Content/WorldObjects/Items/Functional/Parts/Lights/";
        private const string ItemTablePath = "Functional/Parts/Lights/";
        private const string TemplatePath = "Assets/Content/WorldObjects/Items/Functional/Materials/GlassShard.prefab";
        private const string ExamineFolder = "Assets/Content/Data/Examine/String/Items/Functional/Parts/Lights/";
        private const string ItemObjectFolder = "Assets/Content/Data/TileMap/Resources/Items/Functional/Parts/Lights/";
        private const string ItemsDatabaseGuid = "c2644d7b34e956743a0bc485e7ca5e37";
        private const string VendorPath = "Assets/Content/WorldObjects/Furniture/Machines/Vendors/VendorYouTool.prefab";
        private const int VendorStock = 10;
        private const string ReachVolumeName = "ReachVolume";

        private readonly struct BulbItem
        {
            public readonly string Name;
            public readonly string Title;
            public readonly string Description;
            public readonly LightBulbKind Kind;
            public readonly bool Broken;

            public BulbItem(string name, string title, string description, LightBulbKind kind, bool broken)
            {
                Name = name;
                Title = title;
                Description = description;
                Kind = kind;
                Broken = broken;
            }
        }

        private static readonly BulbItem[] Items =
        {
            new("LightBulb", "Light bulb", "A replacement bulb for a small light fixture.", LightBulbKind.Bulb, false),
            new("LightBulbBroken", "Broken light bulb", "The shattered remains of a light bulb. Mind your fingers.", LightBulbKind.Bulb, true),
            new("LightTube", "Light tube", "A replacement fluorescent tube for a light fixture.", LightBulbKind.Tube, false),
            new("LightTubeBroken", "Broken light tube", "What's left of a fluorescent tube.", LightBulbKind.Tube, true),
        };

        public static int SetupAll()
        {
            GameObject model = DB.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new InvalidOperationException("Missing " + ModelPath);

            System.Collections.Generic.Dictionary<string, ObjectAssetReference> references = Items.ToDictionary(item => item.Name, item => BuildItem(model, item));
            SetupFixture("LightBulbFixture", LightBulbKind.Bulb, "LightBulb", "LightBulbBroken",
                references["LightBulb"], references["LightBulbBroken"]);
            SetupFixture("LightTubeFixture", LightBulbKind.Tube, "LightTube", "LightTubeBroken",
                references["LightTube"], references["LightTubeBroken"]);
            StockVendor("LightBulb", "LightTube");
            DB.SaveAssets();
            return Items.Length + 3;
        }

        private static ObjectAssetReference BuildItem(GameObject model, BulbItem spec)
        {
            MeshFilter source = model.GetComponentsInChildren<MeshFilter>(true).Single(filter => filter.name == spec.Name);
            string path = ItemFolder + spec.Name + ".prefab";
            EnsureFolder(ItemFolder.TrimEnd('/'));
            if (DB.LoadAssetAtPath<GameObject>(path) == null && !DB.CopyAsset(TemplatePath, path))
                throw new InvalidOperationException("Cannot create " + path);

            ObjectAssetReference reference = EnsureAsset<ObjectAssetReference>("Assets/Content/Data/ObjectAssetReferences/" + spec.Name + ".asset");
            reference.Id = DB.AssetPathToGUID(path);
            reference.Database = ItemsDatabaseGuid;
            EditorUtility.SetDirty(reference);

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = spec.Name;
                // The template's single mesh child takes the FBX's loose part, centred on the root.
                Transform shape = root.GetComponentsInChildren<MeshFilter>(true).Single().transform;
                shape.name = "Model";
                shape.GetComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                shape.GetComponent<MeshRenderer>().sharedMaterials = source.GetComponent<MeshRenderer>().sharedMaterials;
                shape.localRotation = Quaternion.identity;
                shape.localScale = Vector3.one;
                shape.localPosition = -source.sharedMesh.bounds.center;
                MeshCollider collider = shape.GetComponent<MeshCollider>();
                collider.sharedMesh = source.sharedMesh;
                collider.convex = true;

                SerializedObject item = new(root.GetComponent<Item>());
                item.FindProperty("_name").stringValue = spec.Title;
                item.FindProperty("_asset").objectReferenceValue = reference;
                item.FindProperty("_weight").floatValue = ItemSizeWeightTable.WeightOf(ItemTablePath + spec.Name);
                item.FindProperty("_sizeClass").enumValueIndex = (int)ItemSizeWeightTable.SizeOf(ItemTablePath + spec.Name);
                item.ApplyModifiedPropertiesWithoutUndo();

                if (!root.TryGetComponent(out LightBulbItem bulb)) bulb = root.AddComponent<LightBulbItem>();
                SerializedObject bulbSo = new(bulb);
                bulbSo.FindProperty("_kind").enumValueIndex = (int)spec.Kind;
                bulbSo.FindProperty("_broken").boolValue = spec.Broken;
                bulbSo.ApplyModifiedPropertiesWithoutUndo();

                WireExamine(root, spec);
                StorageContainerPrefabSetup.RebuildNetworkBehaviours(root.GetComponent<NetworkObject>());
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Register(reference, path, spec.Name);
            ItemObjectSo placeable = EnsureAsset<ItemObjectSo>(ItemObjectFolder + spec.Name + ".asset");
            placeable.PrefabAsset = reference;
            EditorUtility.SetDirty(placeable);
            return reference;
        }

        private static void SetupFixture(string name, LightBulbKind kind, string intactName, string brokenName,
            ObjectAssetReference intactItem, ObjectAssetReference brokenItem)
        {
            string path = FixtureFolder + name + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Transform intact = FindDeep(root.transform, intactName);
                Transform broken = FindDeep(root.transform, brokenName);
                if (intact == null || broken == null) throw new InvalidOperationException($"{path} lacks {intactName}/{brokenName}");
                broken.gameObject.SetActive(false);

                LightPower power = root.GetComponent<LightPower>();
                if (!root.TryGetComponent(out LightFixtureBulb bulb)) bulb = root.AddComponent<LightFixtureBulb>();
                SerializedObject bulbSo = new(bulb);
                bulbSo.FindProperty("_kind").enumValueIndex = (int)kind;
                bulbSo.FindProperty("_lightPower").objectReferenceValue = power;
                bulbSo.FindProperty("_intactBulb").objectReferenceValue = intact.gameObject;
                bulbSo.FindProperty("_brokenBulb").objectReferenceValue = broken.gameObject;
                bulbSo.FindProperty("_intactItem").objectReferenceValue = intactItem;
                bulbSo.FindProperty("_brokenItem").objectReferenceValue = brokenItem;
                bulbSo.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject powerSo = new(power);
                powerSo.FindProperty("_bulb").objectReferenceValue = bulb;
                powerSo.ApplyModifiedPropertiesWithoutUndo();

                if (!root.TryGetComponent(out Selectable _)) root.AddComponent<Selectable>();

                // Range checks and the interaction point need a collider, but the housing must not block movement,
                // line of sight, or the rounds that break its glass: a trigger on Ignore Raycast, which plain
                // raycasts (LineOfSight) would otherwise still hit.
                if (root.TryGetComponent(out BoxCollider stray)) UnityEngine.Object.DestroyImmediate(stray);
                Transform volume = root.transform.Find(ReachVolumeName);
                if (volume == null)
                {
                    volume = new GameObject(ReachVolumeName).transform;
                    volume.SetParent(root.transform, false);
                }

                volume.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                if (!volume.TryGetComponent(out BoxCollider box)) box = volume.gameObject.AddComponent<BoxCollider>();
                // Inactive renderers (the broken glass) report empty bounds at the origin.
                Bounds bounds = root.GetComponentsInChildren<MeshRenderer>()
                    .Select(renderer => renderer.bounds)
                    .Aggregate((all, next) => { all.Encapsulate(next); return all; });
                box.isTrigger = true;
                box.center = volume.InverseTransformPoint(bounds.center);
                box.size = bounds.size;

                if (!root.TryGetComponent(out SimpleExaminable examinable)) examinable = root.AddComponent<SimpleExaminable>();
                SerializedObject examine = new(examinable);
                if (examine.FindProperty("key").objectReferenceValue == null)
                {
                    examine.FindProperty("key").objectReferenceValue =
                        DB.LoadAssetAtPath<ExamineData>("Assets/Content/Data/Examine/String/Structures/WallMounts/Lights/" + name + ".asset");
                    examine.ApplyModifiedPropertiesWithoutUndo();
                }

                StorageContainerPrefabSetup.RebuildNetworkBehaviours(root.GetComponent<NetworkObject>());
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>The YouTool vendor sells spares, so a crew can relight a room after a firefight.</summary>
        private static void StockVendor(params string[] names)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(VendorPath);
            try
            {
                Component vendor = root.GetComponents<MonoBehaviour>().Single(c => c.GetType().Name == "VendingMachineController");
                SerializedObject so = new(vendor);
                SerializedProperty products = so.FindProperty("_productsToDispense");
                foreach (string name in names)
                {
                    ItemObjectSo product = DB.LoadAssetAtPath<ItemObjectSo>(ItemObjectFolder + name + ".asset");
                    bool listed = false;
                    for (int i = 0; i < products.arraySize; i++)
                    {
                        listed |= products.GetArrayElementAtIndex(i).FindPropertyRelative("Product").objectReferenceValue == product;
                    }

                    if (listed) continue;
                    products.arraySize++;
                    SerializedProperty entry = products.GetArrayElementAtIndex(products.arraySize - 1);
                    entry.FindPropertyRelative("Product").objectReferenceValue = product;
                    entry.FindPropertyRelative("Stock").intValue = VendorStock;
                    entry.FindPropertyRelative("RequiresId").boolValue = false;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, VendorPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Transform FindDeep(Transform root, string name)
        {
            return Array.Find(root.GetComponentsInChildren<Transform>(true), child => child.name == name);
        }

        private static void Register(ObjectAssetReference reference, string path, string address)
        {
            SS3D.Data.AssetDatabases.AssetDatabase db = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>("Assets/Content/Data/Databases/Items.asset");
            AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(reference.Id, db.AssetGroup).address = address;
            db.Assets[reference.Id] = DB.LoadAssetAtPath<GameObject>(path);
            EditorUtility.SetDirty(db);
        }

        private static void WireExamine(GameObject root, BulbItem spec)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection("Examine");
            if (collection == null || collection.GetTable(new LocaleIdentifier("en")) is not StringTable table)
                throw new InvalidOperationException("Missing Examine English table");
            string key = "items.parts." + ToSnake(spec.Name);
            table.AddEntry(key + ".name", spec.Title);
            table.AddEntry(key + ".desc", spec.Description);
            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(table.SharedData);

            ExamineData data = EnsureAsset<ExamineData>(ExamineFolder + spec.Name + ".asset");
            data.Name = new LocalizedString("Examine", key + ".name");
            data.Description = new LocalizedString("Examine", key + ".desc");
            EditorUtility.SetDirty(data);
            if (!root.TryGetComponent(out SimpleExaminable examinable)) examinable = root.AddComponent<SimpleExaminable>();
            SerializedObject examine = new(examinable);
            examine.FindProperty("key").objectReferenceValue = data;
            examine.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string ToSnake(string name)
        {
            return string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            T asset = DB.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                DB.CreateAsset(asset, path);
            }

            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            if (DB.IsValidFolder(folder)) return;
            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            DB.CreateFolder(parent, System.IO.Path.GetFileName(folder));
        }
    }
}
#endif
