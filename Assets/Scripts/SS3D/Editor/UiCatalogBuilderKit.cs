#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using SS3D.UI.Shell.Catalog;

namespace SS3D.Editor
{
    /// <summary>
    /// Shared load/create/save helpers for per-surface <c>*AssetCatalogBuilder</c> Editor menu items
    /// (machine interface, UI shell, ...), so each one only needs to enumerate its own asset paths and
    /// call <c>EditorAssign</c>.
    /// </summary>
    public static class UiCatalogBuilderKit
    {
        public static T LoadRequired<T>(string path, List<string> missing)
            where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                missing.Add(path);
            }

            return asset;
        }

        public static StyleSheet[] LoadStyleSheets(string[] paths, List<string> missing)
        {
            StyleSheet[] sheets = new StyleSheet[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                sheets[i] = LoadRequired<StyleSheet>(paths[i], missing);
            }

            return sheets;
        }

        public static TCatalog LoadOrCreateCatalogAsset<TCatalog>(string assetPath)
            where TCatalog : UiAssetCatalogBase
        {
            TCatalog catalog = AssetDatabase.LoadAssetAtPath<TCatalog>(assetPath);
            if (catalog != null)
            {
                return catalog;
            }

            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            catalog = ScriptableObject.CreateInstance<TCatalog>();
            AssetDatabase.CreateAsset(catalog, assetPath);
            return catalog;
        }

        public static bool FinalizeSave(List<string> missing, string surfaceName, UiAssetCatalogBase catalog, out string error)
        {
            if (missing.Count > 0)
            {
                error = $"{surfaceName} asset catalog rebuild failed. Missing assets:\n- " + string.Join("\n- ", missing);
                return false;
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            error = null;
            return true;
        }
    }
}
#endif
