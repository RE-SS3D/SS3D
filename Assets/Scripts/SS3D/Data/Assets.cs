using System.Collections.Generic;
using Coimbra;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using SS3D.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Assets is used to load assets using enums or ints, with the addition of an enum generator for easy "id" of items.
    /// </summary>
    public static class Assets
    {
        /// <summary>
        /// All loaded databases.
        /// </summary>
        private static readonly Dictionary<string, AssetDatabase> Databases = new();

        // IMPORTANT: All database getters have to be added manually. For now.

        public static Sprite Get(InteractionIcons icon)
        {
            return GetDatabase(nameof(InteractionIcons)).Get<Sprite>((int)icon);
        }

        public static GameObject Get(ItemIds itemIdId)
        {
             return GetDatabase(nameof(ItemIds)).Get<GameObject>((int)itemIdId);
        }

        /// <summary>
        /// Gets something by ID only, useful for adding stuff on databases at runtime, as in modded versions of the game.
        /// </summary>
        /// <param name="id"></param>
        /// <typeparam name="TAssetDatabase">The asset database you want to get.</typeparam>
        /// <typeparam name="TAssetType">The asset type you want returned.</typeparam>
        /// <returns>The loaded asset in the TAssetType type.</returns>
        public static TAssetType GetById<TAssetType>(string databaseName, int id)
            where TAssetType : Object
        {
            return GetDatabase(databaseName).Get<TAssetType>(id);
        }

        /// <summary>
        /// Loads the all asset databases on the project, for cache reasons.
        /// </summary>
        public static void LoadAssetDatabases()
        {
            List<AssetDatabase> assetDatabases = ScriptableSettings.GetOrFind<AssetDatabaseSettings>().IncludedAssetDatabases;

            foreach (AssetDatabase database in assetDatabases)
            {
                Databases.Add(database.EnumName, database);
            }
        }

        /// <summary>
        /// Helper function to find a database in the database list. Used to link the enum to which database to find.
        /// </summary>
        /// <param name="key">The enum name used to identify which database to load.</param>
        /// <returns></returns>
        public static AssetDatabase GetDatabase(string key)
        {
            bool databaseExists = Databases.TryGetValue(key, out AssetDatabase database);

            if (!databaseExists)
            {
                Punpun.Yell(typeof(Assets), $"Database of type {key} not found", Logs.Important);
            }

            return database;
        }
    }
}