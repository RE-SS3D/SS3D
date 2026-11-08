#if UNITY_EDITOR
using SS3D.Systems.Tile.MapEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityAssetDatabase = UnityEditor.AssetDatabase;

namespace SS3D.Editor.MapEditor
{
    /// <summary>
    /// Regenerates <c>MapEditorCatalog.asset</c> from the tile assets under <c>Resources</c> (tier B, editor-tooling-tiers).
    /// </summary>
    public static class MapEditorCatalogRegenerator
    {
        private const string DefaultCatalogPath = MapEditorCatalogGeneration.CatalogPath;

        [MenuItem("SS3D/Map Editor/Regenerate Catalog")]
        public static void RegenerateCatalog()
        {
            MapEditorCatalogSo catalog = UnityAssetDatabase.LoadAssetAtPath<MapEditorCatalogSo>(DefaultCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MapEditorCatalogSo>();
                string dir = Path.GetDirectoryName(DefaultCatalogPath);
                if (!string.IsNullOrEmpty(dir) && !UnityAssetDatabase.IsValidFolder(dir))
                {
                    Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", dir).Replace('\\', '/'));
                    UnityAssetDatabase.Refresh();
                }

                UnityAssetDatabase.CreateAsset(catalog, DefaultCatalogPath);
            }

            List<MapEditorCatalogEntry> entries = MapEditorCatalogGeneration.BuildEntries();
            catalog.Entries = entries;
            EditorUtility.SetDirty(catalog);
            UnityAssetDatabase.SaveAssets();
            Debug.Log($"Map editor catalog regenerated with {entries.Count} entries at {DefaultCatalogPath}.");
        }
    }
}
#endif
