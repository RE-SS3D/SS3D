using System.IO;
using Coimbra;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SS3D.Data
{
    public static class AssetData
    {                                                                   
        private static IconDatabase _icons;
        private static TileObjectsDatabase _tileObjects;
        private static EntitiesDatabase _entities;

        public static IconDatabase Icons => GetIcons();
        public static TileObjectsDatabase TileObjects => GetTileObjects();
        public static EntitiesDatabase Entities => GetEntities();

        public static Sprite Get(InteractionIcons icon) => Icons.Get(icon);
        public static GameObject Get(TileObjects tileObject) => TileObjects.Get((int)tileObject);
        public static GameObject Get(Entities entity) => Entities.Get((int)entity);

        /// <summary>
        /// Preloads all assets
        /// </summary>
        public static void InitializeDatabases()
        {
            Icons.PreloadAssets();
            TileObjects.PreloadAssets();
            Entities.PreloadAssets();
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

        private static TileObjectsDatabase GetTileObjects()
        {
            if (_tileObjects == null)
            {
                ScriptableSettings.TryGet(out TileObjectsDatabase tileObjects);
                _tileObjects = tileObjects;
            }

            return _tileObjects;
        }

        private static EntitiesDatabase GetEntities()
        {
            if (_entities == null)
            {
                ScriptableSettings.TryGet(out EntitiesDatabase entities);
                _entities = entities;
            }

            return _entities;
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