using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Loads UI Toolkit vector icons from SVG asset paths.
    /// </summary>
    public static class MapEditorIconLoader
    {
        private static readonly Dictionary<string, VectorImage> Cache = new();

        public static VectorImage Load(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            if (Cache.TryGetValue(assetPath, out VectorImage cached))
                return cached;

#if UNITY_EDITOR
            VectorImage loaded = AssetDatabase.LoadAssetAtPath<VectorImage>(assetPath);
            Cache[assetPath] = loaded;
            return loaded;
#else
            return null;
#endif
        }

        public static void ClearCache() => Cache.Clear();
    }
}
