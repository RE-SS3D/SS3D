using System.Collections.Generic;
using Coimbra;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using UnityEngine;
using SS3D.Logging;
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
        private static readonly Dictionary<int, AssetDatabase> Databases = new();

        // TODO: Find a way to automate the getters, this is a nightmare way of doing it.

        /// <summary>
        /// Generic getter, supports all databases and all asset database enums.
        /// </summary>
        /// <param name="databaseId"></param>
        /// <param name="assetId"></param>
        /// <typeparam name="TAsset"></typeparam>
        /// <returns></returns>
        public static TAsset Get<TAsset>(int databaseId, int assetId) where TAsset : Object
        {
            return GetDatabase(databaseId).Get<TAsset>(assetId);
        }

        /// <summary>
        /// Generic getter, supports all databases and all asset database enums.
        /// </summary>
        /// <param name="databaseId"></param>
        /// <param name="assetId"></param>
        /// <typeparam name="TAsset"></typeparam>
        /// <returns></returns>
        public static TAsset Get<TAsset>(Enums.AssetDatabases databaseId, int assetId) where TAsset : Object
        {
            return GetDatabase(databaseId).Get<TAsset>(assetId);
        }

        /// <summary>
        /// Gets an interaction icon as a Sprite.
        /// </summary>
        /// <param name="icon"></param>
        /// <returns></returns>
        public static Sprite Get(InteractionIcons icon)
        {
            return GetDatabase(Enums.AssetDatabases.InteractionIcons).Get<Sprite>((int)icon);
        }

        /// <summary>
        /// Gets an Item prefab based on an ItemID.
        /// </summary>
        /// <param name="itemIdId"></param>
        /// <returns></returns>
        public static GameObject Get(ItemId itemIdId)
        {
             return GetDatabase(Enums.AssetDatabases.Items).Get<GameObject>((int)itemIdId);
        }
        
        /// <summary>
        /// Adds an asset to a database, useful for adding stuff on databases at runtime, as in modded versions of the game.
        /// </summary>
        /// <param name="asset"></param>
        /// <typeparam name="databaseName">The asset database you want to get.</typeparam>
        /// <typeparam name="TAsset">The asset type you want returned.</typeparam>
        /// <returns>The loaded asset in the TAssetType type.</returns>
        public static void AddAsset<TAsset>(int databaseId, Object asset) where TAsset : Object
        {
            GetDatabase(databaseId).Add<TAsset>(asset);
        }

        /// <summary>
        /// Loads the databases in the project from the AssetDatabaseSettings, saves it in a Dictionary for easy & performant access.
        /// </summary>
        public static void LoadAssetDatabases()
        {
            List<AssetDatabase> assetDatabases = ScriptableSettings.GetOrFind<AssetDatabaseSettings>().IncludedAssetDatabases;

            for (int index = 0; index < assetDatabases.Count; index++)
            {
                AssetDatabase database = assetDatabases[index];
                Databases.Add(index, database);
            }

            Log.Information(typeof(Assets), "{assetDatabasesCount} Asset Databases initialized", Logs.Important, assetDatabases.Count);
        }

        /// <summary>
        /// Helper function to find a database in the database list. Used to link the enum to which database to find.
        /// </summary>
        /// <param name="key">The enum name used to identify which database to load.</param>
        /// <returns></returns>
        public static AssetDatabase GetDatabase(int key)
        {
            bool databaseExists = Databases.TryGetValue(key, out AssetDatabase database);

            if (!databaseExists)
            {
                Log.Warning(typeof(Assets), "Database of type {key} not found", Logs.Important, key);
            }

            return database;
        }

        /// <summary>
        /// Helper function to find a database in the database list. Used to link the enum to which database to find.
        /// </summary>
        /// <param name="key">The enum name used to identify which database to load.</param>
        /// <returns></returns>
        public static AssetDatabase GetDatabase(Enums.AssetDatabases key)
        {
            bool databaseExists = Databases.TryGetValue((int)key, out AssetDatabase database);

            if (!databaseExists)
            {
                Log.Warning(typeof(Assets), "Database of type {key} not found", Logs.Important, key);
            }

            return database;
        }
    }
}