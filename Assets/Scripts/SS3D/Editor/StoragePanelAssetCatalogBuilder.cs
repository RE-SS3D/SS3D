#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using SS3D.UI.StoragePanel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Editor
{
    /// <summary>
    /// Rebuilds the committed <see cref="StoragePanelAssetCatalog"/> from <see cref="StoragePanelAssetPaths"/>.
    /// </summary>
    public static class StoragePanelAssetCatalogBuilder
    {
        [MenuItem("SS3D/Storage Panel/Rebuild Asset Catalog")]
        public static void RebuildCatalogMenu()
        {
            if (!TryRebuildCatalog(out string error))
            {
                Debug.LogError(error);
                EditorUtility.DisplayDialog("Storage Panel Asset Catalog", error, "OK");
                return;
            }

            Debug.Log($"Rebuilt Storage Panel asset catalog at {StoragePanelAssetPaths.CatalogAssetPath}");
        }

        public static bool TryRebuildCatalog(out string error)
        {
            error = null;
            List<string> missing = new();

            PanelSettings panelSettings = LoadRequired<PanelSettings>(StoragePanelAssetPaths.PanelSettings, missing);
            StyleSheet storagePanelStyle = LoadRequired<StyleSheet>(StoragePanelAssetPaths.StoragePanelStyle, missing);
            StyleSheet inventorySlotStyle = LoadRequired<StyleSheet>(StoragePanelAssetPaths.InventorySlotStyle, missing);

            if (missing.Count > 0)
            {
                error = "Storage Panel asset catalog rebuild failed. Missing assets:\n- "
                    + string.Join("\n- ", missing);
                return false;
            }

            string directory = Path.GetDirectoryName(StoragePanelAssetPaths.CatalogAssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            StoragePanelAssetCatalog catalog =
                AssetDatabase.LoadAssetAtPath<StoragePanelAssetCatalog>(StoragePanelAssetPaths.CatalogAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StoragePanelAssetCatalog>();
                AssetDatabase.CreateAsset(catalog, StoragePanelAssetPaths.CatalogAssetPath);
            }

            catalog.EditorAssign(panelSettings, storagePanelStyle, inventorySlotStyle);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static T LoadRequired<T>(string path, List<string> missing) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                missing.Add($"{typeof(T).Name}: {path}");
            }

            return asset;
        }
    }
}
#endif
