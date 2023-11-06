using Coimbra;
using JetBrains.Annotations;
using Serilog;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Calls all the methods for generating code for the AssetData system.
    /// </summary>
    public static class AssetDatabasesCodeGenerator
    {
        /// <summary>
        /// The path in the project where to put the WorldObjectAssetReference assets.
        /// </summary>
        private const string WorldObjectAssetPath = "Assets/Content/Data/WorldObjectAssetReferences/";

        /// <summary>
        /// Generates all the code needed for the asset data system to work.
        /// </summary>
        public static void GenerateAssetDatabasesCode()
        {
            AssetDatabaseSettings settings = ScriptableSettings.GetOrFind<AssetDatabaseSettings>(); 

            if (settings == null)
            {
                Log.Error($"{nameof(AssetDatabasesCodeGenerator)} - Asset database settings has not be found");
                return;
            }

            if (settings.IncludedAssetDatabases == null || settings.IncludedAssetDatabases.Count == 0)
            {
                Log.Error($"{nameof(AssetDatabasesCodeGenerator)} - No Databases have been found");
                return;
            }

            settings.CreateDatabaseCode();

            foreach (AssetDatabase includedAssetDatabase in settings.IncludedAssetDatabases)
            {
                CreateDatabaseCode(includedAssetDatabase);
                CreateWorldObjectAssetReferences(includedAssetDatabase);
            }
        }

        /// <summary>
        /// Calls the method to generate all the code for a database.
        /// </summary>
        private static void CreateDatabaseCode(AssetDatabase assetDatabase)
        {
            assetDatabase.GenerateDatabaseCode();
        }

        /// <summary>
        /// Creates all the WorldObjectAssetReferences for a database. 
        /// </summary>
        private static void CreateWorldObjectAssetReferences(AssetDatabase assetDatabase)
        {
            List<Object> worldObjectAssetsToDestroy = new List<Object>();

            foreach (Object asset in assetDatabase.Assets.Values)
            {
                if (asset is not GameObject gameObject)
                {
                    continue;
                }

                WorldObjectAssetReference worldObjectAssetReference = CreateWorldObjectAssetReference(gameObject, assetDatabase.DatabaseName);

                if (!UnityEditor.AssetDatabase.Contains(worldObjectAssetReference))
                {
                    UnityEditor.AssetDatabase.CreateAsset(worldObjectAssetReference, $"{WorldObjectAssetPath}{worldObjectAssetReference.Id}.asset");
                }
                else
                {
                    worldObjectAssetsToDestroy.Add(worldObjectAssetReference);
                }
            }

            foreach (Object asset in worldObjectAssetsToDestroy)
            {
                Object.DestroyImmediate(asset);
            }
        }

        /// <summary>
        /// Creates the WorldObjectAssetReference asset in the correct path.
        /// </summary>
        /// <param name="gameObject">The asset to include in this WorldObjectAsset.</param>
        /// <param name = "assetDatabaseName">The related database.</param>
        [NotNull]
        private static WorldObjectAssetReference CreateWorldObjectAssetReference(GameObject gameObject, string assetDatabaseName)
        {
            WorldObjectAssetReference worldObjectAssetReference = ScriptableObject.CreateInstance<WorldObjectAssetReference>();

            worldObjectAssetReference.Id = gameObject.name;
            worldObjectAssetReference.Database = assetDatabaseName;

            if (gameObject.TryGetComponent(out IWorldObjectAsset worldObjectAsset))
            {
                worldObjectAsset.Asset = worldObjectAssetReference;
            }

            EditorUtility.SetDirty(gameObject);
            EditorUtility.SetDirty(worldObjectAssetReference);

            return worldObjectAssetReference;
        }
    }
}