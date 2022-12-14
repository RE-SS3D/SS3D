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
        public static IconDatabase Icons;
        public static TileObjectsDatabase TileObjects;
        public static EntitiesDatabase Entities;

        public static Sprite Get(InteractionIcons icon) => Icons.Get(icon);
        public static GameObject Get(TileObjects tileObject) => TileObjects.Get((int)tileObject);
        public static GameObject Get(Entities entity) => Entities.Get((int)entity);

        /// <summary>
        /// Preloads all assets
        /// </summary>
        public static void InitializeDatabases()
        {
            ScriptableSettings.TryGet(out Icons);
            ScriptableSettings.TryGet(out TileObjects);
            ScriptableSettings.TryGet(out Entities);

            Icons.PreloadAssets();
            TileObjects.PreloadAssets();
            Entities.PreloadAssets();
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