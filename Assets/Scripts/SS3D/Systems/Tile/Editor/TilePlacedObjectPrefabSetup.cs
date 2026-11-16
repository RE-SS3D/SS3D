#if UNITY_EDITOR
using FishNet.Object;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Tile.Editor
{
    /// <summary>
    /// Ensures every networked tile prefab referenced by a <see cref="TileObjectSo"/> carries
    /// <see cref="PlacedTileObject"/> on its root. <see cref="PlacedTileObject.Create"/> falls back to
    /// AddComponent at runtime, but a NetworkBehaviour added after instantiation is not part of the
    /// NetworkObject: pure clients never receive it, so the tile never enters the client tilemap
    /// (missing-plenum build checks, layer hide and delete ignore it).
    /// Tier B recipe — no MenuItem.
    /// BatchMode: <c>-executeMethod SS3D.Systems.Tile.Editor.TilePlacedObjectPrefabSetup.SetupAll</c>
    /// </summary>
    public static class TilePlacedObjectPrefabSetup
    {
        /// <summary>
        /// Adds the component where missing and refreshes the NetworkObject behaviour list where it is
        /// not yet registered. Returns the number of prefabs changed.
        /// </summary>
        public static int SetupAll()
        {
            int updated = 0;

            // Base prefabs first so variants inherit the component instead of getting an override.
            foreach (string prefabPath in FindTilePrefabPaths()
                         .OrderBy(path => IsVariant(path) ? 1 : 0)
                         .ThenBy(path => path))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                    continue;

                if (!prefab.TryGetComponent(out NetworkObject prefabNob))
                {
                    Debug.LogWarning($"[TilePlacedObjectPrefabSetup] {prefabPath} has no NetworkObject; skipped.");
                    continue;
                }

                PlacedTileObject existing = prefab.GetComponent<PlacedTileObject>();
                if (existing != null && IsRegistered(prefabNob, existing))
                    continue;

                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    if (root.GetComponent<PlacedTileObject>() == null)
                        root.AddComponent<PlacedTileObject>();

                    // Same refresh FishNet runs on prefab registration; keeps the serialized
                    // NetworkBehaviour list and component indices canonical so Play does not churn them.
                    FishNet.Managing.Object.ManagedObjects.InitializePrefab(root.GetComponent<NetworkObject>(), -1);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    updated++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[TilePlacedObjectPrefabSetup] Added PlacedTileObject to {updated} tile prefab(s).");
            return updated;
        }

        /// <summary>Distinct prefab paths referenced by every <see cref="TileObjectSo"/> in the project.</summary>
        public static List<string> FindTilePrefabPaths()
        {
            HashSet<string> paths = new();
            foreach (string soGuid in AssetDatabase.FindAssets($"t:{nameof(TileObjectSo)}"))
            {
                TileObjectSo tileSo = AssetDatabase.LoadAssetAtPath<TileObjectSo>(AssetDatabase.GUIDToAssetPath(soGuid));
                if (tileSo == null || tileSo.PrefabAsset == null)
                    continue;

                GameObject prefab = Data.Assets.Get<GameObject>(tileSo.PrefabAsset);
                string prefabPath = prefab != null ? AssetDatabase.GetAssetPath(prefab) : null;
                if (!string.IsNullOrEmpty(prefabPath))
                    paths.Add(prefabPath);
            }

            return paths.ToList();
        }

        private static bool IsRegistered(NetworkObject nob, NetworkBehaviour behaviour) =>
            nob.NetworkBehaviours != null && System.Array.IndexOf(nob.NetworkBehaviours, behaviour) >= 0;

        private static bool IsVariant(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            return prefab != null && PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Variant;
        }
    }
}
#endif
