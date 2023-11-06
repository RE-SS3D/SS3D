using Coimbra;
using Serilog;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    public static class AssetDatabasesCodeGenerator
    {
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
                CreateWorldObjectReferences(includedAssetDatabase);
            }
        }

        private static void CreateDatabaseCode(AssetDatabase includedAssetDatabase)
        {
            includedAssetDatabase.GenerateDatabaseCode();
        }

        private static void CreateWorldObjectReferences(AssetDatabase includedAssetDatabase)
        {
            List<Object> worldObjectAssetsToDestroy = new List<Object>();

            foreach (Object asset in includedAssetDatabase.Assets.Values)
            {
                if (asset is not GameObject gameObject)
                {
                    continue;
                }

                WorldObjectAssetReference worldObjectAssetReference = ScriptableObject.CreateInstance<WorldObjectAssetReference>();

                worldObjectAssetReference.Id = gameObject.name;
                worldObjectAssetReference.Database = includedAssetDatabase.name;

                if (!UnityEditor.AssetDatabase.Contains(worldObjectAssetReference))
                {
                    UnityEditor.AssetDatabase.CreateAsset(worldObjectAssetReference, $"{WorldObjectAssetPath}{worldObjectAssetReference.Id}.asset");

                    if (gameObject.TryGetComponent(out IWorldObjectAsset worldObjectAsset))
                    {
                        worldObjectAsset.Asset = worldObjectAssetReference;
                    }
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
    }
}