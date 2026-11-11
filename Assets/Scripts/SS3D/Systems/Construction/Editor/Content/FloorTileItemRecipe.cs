#if UNITY_EDITOR
using System.Collections.Generic;
using FishNet.Object;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Construction.Editor
{
    /// <summary>
    /// Tier-B recipe for one floor tile item per steel floor (D17 of 2026-11_construction-mvp1), from
    /// <see cref="ConstructionFloorTiles.Kinds"/>. Each non-grey kind is a copy of <c>FloorTile.prefab</c>
    /// wearing its floor's material, with its own prefab id, reference, item asset, Addressables entry and
    /// Items database row; the map editor catalog is regenerated. Idempotent.
    /// </summary>
    public static class FloorTileItemRecipe
    {
        public const string FloorTilePrefab = ConstructionItemRecipe.FloorTilePath;
        public const string ItemFolder = "Assets/Content/WorldObjects/Items/Functional/Generic/";
        public const string ItemAssetFolder = "Assets/Content/Data/TileMap/Resources/Items/Functional/Materials/";
        public const string FloorFolder = "Assets/Content/WorldObjects/Structures/Floors/Tiles/Steel/";

        public static int SetupAll()
        {
            GameObject source = DB.LoadAssetAtPath<GameObject>(FloorTilePrefab);
            ItemObjectSo sourceItem = DB.LoadAssetAtPath<ItemObjectSo>(ItemAssetFolder + ConstructionNames.FloorTileItem + ".asset");
            SS3D.Data.AssetDatabases.AssetDatabase database = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>(GirderStageRecipe.ItemsDatabase);
            if (source == null || sourceItem == null || database == null || database.AssetGroup == null)
            {
                Debug.LogError("[FloorTileItemRecipe] Missing FloorTile prefab, its item asset, or the Items database");
                return 0;
            }

            int changed = 0;
            foreach ((string turf, string itemName, string displayName) in ConstructionFloorTiles.Kinds)
            {
                string path = ItemFolder + itemName + ".prefab";
                if (path == FloorTilePrefab)
                {
                    continue;
                }

                Material material = FloorMaterial(turf);
                if (material == null)
                {
                    Debug.LogError($"[FloorTileItemRecipe] {FloorFolder}{turf}.prefab has no material");
                    continue;
                }

                bool created = false;
                if (DB.LoadAssetAtPath<GameObject>(path) == null)
                {
                    if (!DB.CopyAsset(FloorTilePrefab, path))
                    {
                        Debug.LogError($"[FloorTileItemRecipe] Could not copy {FloorTilePrefab} to {path}");
                        continue;
                    }

                    created = true;
                    changed++;
                }

                ObjectAssetReference reference = Register(path, itemName, sourceItem, database);
                if (ApplyPrefab(path, itemName, displayName, reference, material, created))
                {
                    changed++;
                }
            }

            DB.SaveAssets();
            if (changed > 0)
            {
                MapEditorCatalogSo catalog = DB.LoadAssetAtPath<MapEditorCatalogSo>(MapEditorCatalogGeneration.CatalogPath);
                if (catalog != null)
                {
                    catalog.Entries = MapEditorCatalogGeneration.BuildEntries();
                    EditorUtility.SetDirty(catalog);
                    DB.SaveAssets();
                }
            }

            return changed;
        }

        private static Material FloorMaterial(string turf)
        {
            GameObject floor = DB.LoadAssetAtPath<GameObject>(FloorFolder + turf + ".prefab");
            MeshRenderer renderer = floor != null ? floor.GetComponentInChildren<MeshRenderer>(true) : null;
            return renderer != null ? renderer.sharedMaterial : null;
        }

        /// <summary>Reference, item asset, Addressables entry and Items database row for one tile item.</summary>
        private static ObjectAssetReference Register(string path, string itemName, ItemObjectSo sourceItem,
            SS3D.Data.AssetDatabases.AssetDatabase database)
        {
            string guid = DB.AssetPathToGUID(path);

            ObjectAssetReference reference = EnsureAsset<ObjectAssetReference>(GirderStageRecipe.ReferenceFolder + itemName + ".asset");
            reference.Id = guid;
            reference.Database = sourceItem.PrefabAsset != null ? sourceItem.PrefabAsset.Database : database.DatabaseID;
            EditorUtility.SetDirty(reference);

            ItemObjectSo item = EnsureAsset<ItemObjectSo>(ItemAssetFolder + itemName + ".asset");
            item.PrefabAsset = reference;
            item.icon = sourceItem.icon;
            EditorUtility.SetDirty(item);

            AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, database.AssetGroup).address = path;
            database.Assets[guid] = DB.LoadAssetAtPath<GameObject>(path);
            EditorUtility.SetDirty(database);
            return reference;
        }

        private static bool ApplyPrefab(string path, string itemName, string displayName, ObjectAssetReference reference,
            Material material, bool created)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = created;
                if (root.name != itemName)
                {
                    root.name = itemName;
                    changed = true;
                }

                SerializedObject item = new(root.GetComponent<Item>());
                SerializedProperty asset = item.FindProperty("_asset");
                SerializedProperty name = item.FindProperty("_name");
                if (asset.objectReferenceValue != reference || name.stringValue != displayName)
                {
                    asset.objectReferenceValue = reference;
                    name.stringValue = displayName;
                    item.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer.gameObject.name == InteractionOutlineName || renderer.sharedMaterial == material)
                    {
                        continue;
                    }

                    renderer.sharedMaterials = Fill(material, renderer.sharedMaterials.Length);
                    changed = true;
                }

                if (!changed)
                {
                    return false;
                }

                if (created && root.TryGetComponent(out NetworkObject networkObject))
                {
                    // A copy carries the source's prefab id; let FishNet assign the copy its own.
                    FishNet.Managing.Object.ManagedObjects.InitializePrefab(networkObject, -1);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private const string InteractionOutlineName = "InteractionOutline";

        private static Material[] Fill(Material material, int count)
        {
            List<Material> materials = new();
            for (int i = 0; i < Mathf.Max(1, count); i++)
            {
                materials.Add(material);
            }

            return materials.ToArray();
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            T asset = DB.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            DB.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
