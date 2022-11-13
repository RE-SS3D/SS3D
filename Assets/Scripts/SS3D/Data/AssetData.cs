using System.IO;
using Coimbra;
using UnityEditor;
using UnityEngine;

namespace SS3D.Data
{
    public static class AssetData
    {                                                                   
        public static IconDatabase _icons;
        public static TileObjectDatabase _tileObjects;

        public static IconDatabase Icons => GetIcons();
        public static TileObjectDatabase TileObjects => GetTileObjects();

        public static Sprite Get(InteractionIcons icon) => Icons.Get(icon);
        public static GameObject Get(TileObjects tileObject) => TileObjects.Get(tileObject);

        /// <summary>
        /// Preloads all assets
        /// </summary>
        public static void InitializeDatabases()
        {
            Icons.PreloadAssets();
            TileObjects.PreloadAssets();
        }
        
        private static IconDatabase GetIcons()
        {
            if (_icons == null)
            {
                ScriptableSettings.TryGet(out IconDatabase icons);
                _icons = icons;
            }

            return _icons;
        }

        private static TileObjectDatabase GetTileObjects()
        {
            if (_tileObjects == null)
            {
                ScriptableSettings.TryGet(out TileObjectDatabase tileObjects);
                _tileObjects = tileObjects;
            }

            return _tileObjects;
        }

#if UNITY_EDITOR
        public static string GetAssetPath(ScriptableObject scriptableObject)
        {
            MonoScript ms = MonoScript.FromScriptableObject(scriptableObject);
            string path = AssetDatabase.GetAssetPath(ms);

            string fullPath = Directory.GetParent(path)?.FullName;

            return fullPath;
        }
#endif
    }
}