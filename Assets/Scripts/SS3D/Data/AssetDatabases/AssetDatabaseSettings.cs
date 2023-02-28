using System.Collections.Generic;
using Coimbra;
using SS3D.Attributes;
using SS3D.Logging;

namespace SS3D.Data.AssetDatabases
{
    [ProjectSettings("SS3D")]
    public class AssetDatabaseSettings : ScriptableSettings
    {
        /// <summary>
        /// Included databases on the game.
        /// </summary>
#if UNITY_EDITOR
        [ReadOnly]
#endif
        public List<AssetDatabase> IncludedAssetDatabases;

#if UNITY_EDITOR
        /// <summary>
        /// Initializes all asset databases in the project and adds to the databases list.
        /// </summary>
        public static List<AssetDatabase> FindAssetDatabases()
        {
            string[] assets = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(AssetDatabase)}");

            List<AssetDatabase> databases = new();

            foreach (string database in assets)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(database);
                AssetDatabase assetDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<AssetDatabase>(assetPath);

                databases.Add(assetDatabase);
            }

            Punpun.Say(typeof(Assets), $"{assets.Length} Asset Databases initialized", Logs.Important);

            return databases;
        }
#endif
    }
}