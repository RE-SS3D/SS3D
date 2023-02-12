using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Coimbra;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using SS3D.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Asset data is used to load stuff via Addressables, with the addition of an enum generator for easy "id" of items.
    /// </summary>
    public static class AssetData
    {
        /// <summary>
        /// All loaded databases.
        /// </summary>
        private static readonly Dictionary<Type, GenericAssetDatabase> Databases = new();

        // IMPORTANT: All database getters have to be added manually. For now.
        public static Sprite Get(InteractionIcons icon) => FindDatabase<InteractionIconsAssetDatabase>().Get(icon);
        public static GameObject Get(ItemIDs itemId) => FindDatabase<ItemsAssetDatabase>().Get(itemId);

        /// <summary>
        /// Gets something by ID only, useful for adding stuff on databases at runtime, as in modded versions of the game.
        /// </summary>
        /// <param name="id"></param>
        /// <typeparam name="TAssetDatabase">The asset database you want to get.</typeparam>
        /// <typeparam name="TAssetType">The asset type you want returned.</typeparam>
        /// <returns>The loaded asset in the TAssetType type.</returns>
        public static TAssetType GetById<TAssetDatabase, TAssetType>(int id) where TAssetDatabase : GenericAssetDatabase where TAssetType : Object
        {
            return FindDatabase<TAssetDatabase>().Get<TAssetType>(id);
        }

        /// <summary>
        /// Initializes all asset databases in the project.
        /// </summary>
        public static void InitializeAssetDatabases()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            List<Type> types = assemblies.SelectMany(assembly => assembly.GetTypes()).ToList();
            List<Type> genericDatabaseInheritors = types.Where(type => type.IsSubclassOf(typeof(GenericAssetDatabase))).ToList();

            foreach (Type genericDatabase in genericDatabaseInheritors)
            {
                ScriptableSettings.TryGet(genericDatabase, out ScriptableSettings databaseAsset);
                ((GenericAssetDatabase)databaseAsset).PreloadAssets();

                Databases.Add(genericDatabase, (GenericAssetDatabase)databaseAsset);
            }

            Punpun.Say(typeof(AssetData), $"{genericDatabaseInheritors.Count} Asset Databases initialized", Logs.Important);
        }

        /// <summary>
        /// Helper function to find a database of type T in the database list. Used to link the enum to which database to find.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T FindDatabase<T>() where T : GenericAssetDatabase
        {
            bool databaseExists = Databases.TryGetValue(typeof(T), out GenericAssetDatabase database);

            if (!databaseExists)
            {
                Punpun.Yell(typeof(AssetData), $"Database of type {typeof(T)} not found", Logs.Important);
            }

            return (T)database;
        }
    }
}