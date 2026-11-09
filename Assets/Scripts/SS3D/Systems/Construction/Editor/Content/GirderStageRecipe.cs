#if UNITY_EDITOR
using System.Collections.Generic;
using FishNet.Object;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.MapEditor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using DB = UnityEditor.AssetDatabase;

namespace SS3D.Systems.Construction.Editor
{
    /// <summary>
    /// Tier-B recipe for the wall ladder's stage tiles (D1/D2 of 2026-11_construction-mvp1). A stage
    /// prefab contains only the meshes it shows, so <c>PlacedTileObject.EnableAllChildRenderers</c> cannot
    /// reveal hidden layers:
    /// <list type="bullet">
    /// <item><c>SteelGirderPlated</c>: <c>GirderShape</c> + <c>SteelWallSheets</c>, copied from the girder
    /// before it is stripped, with its own reference, tile asset, Addressables entry and database row.</item>
    /// <item><c>SteelGirder</c>: <c>GirderShape</c> only. The reinforced, window-sheet, support and strut
    /// layers stay in <c>SteelWalls.fbx</c> for reinforced building later.</item>
    /// </list>
    /// Both tile assets are <c>airtight = false</c>; the map editor catalog is regenerated. Idempotent.
    /// </summary>
    public static class GirderStageRecipe
    {
        public const string GirderPrefab = "Assets/Content/WorldObjects/Structures/Walls/SteelGirder.prefab";
        public const string PlatedPrefab = "Assets/Content/WorldObjects/Structures/Walls/SteelGirderPlated.prefab";
        public const string GirderTile = "Assets/Content/Data/TileMap/Resources/Structures/Walls/SteelGirder.asset";
        public const string PlatedTile = "Assets/Content/Data/TileMap/Resources/Structures/Walls/SteelGirderPlated.asset";
        public const string ReferenceFolder = "Assets/Content/Data/ObjectAssetReferences/";
        public const string ItemsDatabase = "Assets/Content/Data/Databases/Items.asset";

        public const string GirderShape = "GirderShape";
        public const string WallSheets = "SteelWallSheets";

        public static int SetupAll()
        {
            int changed = 0;

            if (DB.LoadAssetAtPath<GameObject>(PlatedPrefab) == null)
            {
                if (!PrefabHasChild(GirderPrefab, WallSheets))
                {
                    Debug.LogError($"[GirderStageRecipe] {GirderPrefab} no longer has {WallSheets}; restore it from git to build the plated stage.");
                    return 0;
                }

                if (!DB.CopyAsset(GirderPrefab, PlatedPrefab))
                {
                    Debug.LogError($"[GirderStageRecipe] Could not copy {GirderPrefab} to {PlatedPrefab}");
                    return 0;
                }

                changed++;
            }

            TileObjectSo girderTile = DB.LoadAssetAtPath<TileObjectSo>(GirderTile);
            if (girderTile == null)
            {
                Debug.LogError($"[GirderStageRecipe] Missing {GirderTile}");
                return changed;
            }

            TileObjectSo platedTile = RegisterPlatedTile(girderTile);
            changed += KeepOnly(PlatedPrefab, "SteelGirderPlated", new[] { GirderShape, WallSheets }, platedTile) ? 1 : 0;
            changed += KeepOnly(GirderPrefab, null, new[] { GirderShape }, null) ? 1 : 0;

            if (girderTile.airtight)
            {
                girderTile.airtight = false;
                EditorUtility.SetDirty(girderTile);
                changed++;
            }

            DB.SaveAssets();
            RegenerateMapEditorCatalog();
            return changed;
        }

        private static bool PrefabHasChild(string path, string child)
        {
            GameObject prefab = DB.LoadAssetAtPath<GameObject>(path);
            return prefab != null && prefab.transform.Find(child) != null;
        }

        /// <summary>
        /// Strips the prefab to <paramref name="keep"/> children, turns their renderers on, points the
        /// multi-connector at them and rewrites the NetworkObject behaviour list.
        /// </summary>
        private static bool KeepOnly(string path, string rootName, string[] keep, TileObjectSo tile)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[GirderStageRecipe] Missing {path}");
                return false;
            }

            try
            {
                HashSet<string> keepNames = new(keep);
                bool changed = false;

                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = root.transform.GetChild(i);
                    if (!keepNames.Contains(child.name))
                    {
                        Object.DestroyImmediate(child.gameObject);
                        changed = true;
                    }
                }

                List<GameObject> connectors = new();
                foreach (string name in keep)
                {
                    Transform child = root.transform.Find(name);
                    if (child == null)
                    {
                        Debug.LogError($"[GirderStageRecipe] {path} has no {name}");
                        return false;
                    }

                    foreach (MeshRenderer renderer in child.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if (!renderer.enabled)
                        {
                            renderer.enabled = true;
                            changed = true;
                        }
                    }

                    connectors.Add(child.gameObject);
                }

                if (root.TryGetComponent(out MultiAdjacencyConnector multi))
                {
                    SerializedObject so = new(multi);
                    SerializedProperty list = so.FindProperty("_connectors");
                    if (list.arraySize != connectors.Count)
                    {
                        changed = true;
                    }

                    list.arraySize = connectors.Count;
                    for (int i = 0; i < connectors.Count; i++)
                    {
                        list.GetArrayElementAtIndex(i).objectReferenceValue = connectors[i];
                    }

                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                if (rootName != null && root.name != rootName)
                {
                    root.name = rootName;
                    changed = true;
                }

                if (tile != null && root.TryGetComponent(out PlacedTileObject placed))
                {
                    SerializedObject placedSo = new(placed);
                    SerializedProperty asset = placedSo.FindProperty("_asset");
                    SerializedProperty tileSo = placedSo.FindProperty("_tileObjectSo");
                    if (asset.objectReferenceValue != tile.PrefabAsset || tileSo.objectReferenceValue != tile)
                    {
                        asset.objectReferenceValue = tile.PrefabAsset;
                        tileSo.objectReferenceValue = tile;
                        placedSo.ApplyModifiedPropertiesWithoutUndo();
                        changed = true;
                    }
                }

                if (!changed)
                {
                    return false;
                }

                NetworkObject networkObject = root.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    RebuildNetworkBehaviours(networkObject);
                    if (rootName != null)
                    {
                        // A copy carries the source's prefab id; let FishNet assign the copy its own.
                        FishNet.Managing.Object.ManagedObjects.InitializePrefab(networkObject, -1);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Reference, tile asset, Addressables entry and Items database row for the plated prefab.</summary>
        private static TileObjectSo RegisterPlatedTile(TileObjectSo girder)
        {
            string guid = DB.AssetPathToGUID(PlatedPrefab);
            SS3D.Data.AssetDatabases.AssetDatabase database = DB.LoadAssetAtPath<SS3D.Data.AssetDatabases.AssetDatabase>(ItemsDatabase);

            ObjectAssetReference reference = EnsureAsset<ObjectAssetReference>(ReferenceFolder + "SteelGirderPlated.asset");
            reference.Id = guid;
            reference.Database = girder.PrefabAsset != null ? girder.PrefabAsset.Database : database.DatabaseID;
            EditorUtility.SetDirty(reference);

            TileObjectSo tile = EnsureAsset<TileObjectSo>(PlatedTile);
            tile.PrefabAsset = reference;
            tile.icon = girder.icon;
            tile.layer = girder.layer;
            tile.genericType = girder.genericType;
            tile.specificType = girder.specificType;
            tile.width = girder.width;
            tile.height = girder.height;
            tile.structuralMaxIntegrity = girder.structuralMaxIntegrity;
            tile.impactMaterial = girder.impactMaterial;
            tile.airtight = false;
            EditorUtility.SetDirty(tile);

            if (database != null && database.AssetGroup != null)
            {
                AddressableAssetSettingsDefaultObject.Settings.CreateOrMoveEntry(guid, database.AssetGroup).address = PlatedPrefab;
                database.Assets[guid] = DB.LoadAssetAtPath<GameObject>(PlatedPrefab);
                EditorUtility.SetDirty(database);
            }
            else
            {
                Debug.LogError($"[GirderStageRecipe] Missing {ItemsDatabase} or its Addressables group");
            }

            return tile;
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

        private static void RegenerateMapEditorCatalog()
        {
            MapEditorCatalogSo catalog = DB.LoadAssetAtPath<MapEditorCatalogSo>(MapEditorCatalogGeneration.CatalogPath);
            if (catalog == null)
            {
                Debug.LogWarning("[GirderStageRecipe] Map editor catalog missing; run SS3D → Map Editor → Regenerate Catalog.");
                return;
            }

            catalog.Entries = MapEditorCatalogGeneration.BuildEntries();
            EditorUtility.SetDirty(catalog);
            DB.SaveAssets();
        }

        /// <summary>FishNet's UpdateNetworkBehaviours for prefab assets (same as the hub and storage recipes).</summary>
        private static void RebuildNetworkBehaviours(NetworkObject networkObject)
        {
            List<NetworkBehaviour> behaviours = new();
            CollectNetworkBehaviours(networkObject.transform, behaviours);

            SerializedObject nobSo = new(networkObject);
            SerializedProperty listProp = nobSo.FindProperty("_networkBehaviours");
            listProp.arraySize = behaviours.Count;
            for (int i = 0; i < behaviours.Count; i++)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = behaviours[i];

                SerializedObject behaviourSo = new(behaviours[i]);
                behaviourSo.FindProperty("_addedNetworkObject").objectReferenceValue = networkObject;
                behaviourSo.FindProperty("_networkObjectCache").objectReferenceValue = networkObject;
                behaviourSo.FindProperty("_componentIndexCache").intValue = i;
                behaviourSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(behaviours[i]);
            }

            nobSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(networkObject);
        }

        private static void CollectNetworkBehaviours(Transform transform, List<NetworkBehaviour> behaviours)
        {
            behaviours.AddRange(transform.GetComponents<NetworkBehaviour>());
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.TryGetComponent(out NetworkObject _))
                {
                    continue;
                }

                CollectNetworkBehaviours(child, behaviours);
            }
        }
    }
}
#endif
