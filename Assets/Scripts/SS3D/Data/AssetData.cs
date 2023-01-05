using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Coimbra;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using UnityEngine;

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

        /// <summary>
        /// Initializes all asset databases in the project.
        /// </summary>
        public static void InitializeAssetDatabases()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            IEnumerable<Type> types = assemblies.SelectMany(assembly => assembly.GetTypes()).ToList();
            IEnumerable<Type> genericDatabaseInheritors = types.Where(type => type.IsSubclassOf(typeof(GenericAssetDatabase)));

            foreach (Type genericDatabase in genericDatabaseInheritors)
            {
                ScriptableSettings.TryGet(genericDatabase, out ScriptableSettings databaseAsset);
                ((GenericAssetDatabase)databaseAsset).PreloadAssets();

                Databases.Add(genericDatabase, (GenericAssetDatabase)databaseAsset);
            }
        }

        /// <summary>
        /// Helper function to find a database of type T in the database list. Used to link the enum to which database to find.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static T FindDatabase<T>() where T : GenericAssetDatabase
        {
            Databases.TryGetValue(typeof(T), out GenericAssetDatabase database);

            return (T)database;
        }
    }
}