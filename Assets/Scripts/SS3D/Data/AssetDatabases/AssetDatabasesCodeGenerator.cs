#if UNITY_EDITOR
using Coimbra;
using JetBrains.Annotations;
using Serilog;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly string WorldObjectAssetPath = "Assets/Content/Data/WorldObjectAssetReferences/";

        private static readonly Dictionary<string, WorldObjectAssetReference> SavedAssetReferences = new Dictionary<string, WorldObjectAssetReference>();

        private static bool HasModifiedAssetsWhenGenerating;

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

            SavedAssetReferences.Clear();

            settings.CreateDatabaseCode();

            HasModifiedAssetsWhenGenerating = false;

            LoadAllWorldObjectAssetReferences();

            foreach (AssetDatabase includedAssetDatabase in settings.IncludedAssetDatabases)
            {
                CreateDatabaseCode(includedAssetDatabase);

                CreateWorldObjectAssetReferences(includedAssetDatabase);
            }

            CleanupWorldObjectAssetReferences(settings.IncludedAssetDatabases);
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
            int createdAssets = 0;
            int modifiedAssets = 0;

            foreach (Object asset in assetDatabase.Assets.Values)
            {
                if (asset is not GameObject gameObject)
                {
                    continue;
                }

                if (SavedAssetReferences.Values.ToList().Exists(reference => reference.Id == gameObject.name && reference.Database == assetDatabase.DatabaseName))
                {
                    WorldObjectAssetReference worldObjectAssetReference = SavedAssetReferences.Values.ToList().Find(reference => reference.Id == gameObject.name && reference.Database == assetDatabase.DatabaseName);

                    UpdateWorldObjectAssetReference(worldObjectAssetReference, gameObject, ref modifiedAssets);
                }
                else
                {
                    WorldObjectAssetReference worldObjectAssetReference = CreateWorldObjectAssetReference(gameObject.name, assetDatabase.DatabaseName);

                    string key = $"{WorldObjectAssetPath}{worldObjectAssetReference.Id}.asset";

                    if (SavedAssetReferences.ContainsKey(key))
                    {
                        Debug.LogError($"[{nameof(AssetDatabasesCodeGenerator)}] - {key} is already on the dictionary");
                        continue;
                    }

                    SavedAssetReferences.Add(key, worldObjectAssetReference);

                    UpdateWorldObjectAssetReference(worldObjectAssetReference, gameObject, ref createdAssets);
                }
            }

            if (createdAssets > 0)
            {
                Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - {createdAssets} {nameof(WorldObjectAssetReference)} created for {assetDatabase.DatabaseName}.");
            }

            if (modifiedAssets > 0)
            {
                Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - {modifiedAssets} {nameof(WorldObjectAssetReference)} modified for {assetDatabase.DatabaseName}.");
            }

            if (modifiedAssets == 0 && createdAssets == 0)
            {
                Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - No {nameof(WorldObjectAssetReference)} were modified or created for {assetDatabase.DatabaseName}.");
            }
        }

        private static WorldObjectAssetReference CreateWorldObjectAssetReference(string gameObjectName, string assetDatabaseName)
        {
            WorldObjectAssetReference worldObjectAssetReference = ScriptableObject.CreateInstance<WorldObjectAssetReference>();

            worldObjectAssetReference.Id = gameObjectName;
            worldObjectAssetReference.Database = assetDatabaseName;

            UnityEditor.AssetDatabase.CreateAsset(worldObjectAssetReference, $"{WorldObjectAssetPath}{worldObjectAssetReference.Id}.asset");

            HasModifiedAssetsWhenGenerating = true;

            Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - Creating {worldObjectAssetReference.Id} WorldObjectReferenceAsset as it was missing.");

            return worldObjectAssetReference;
        }

        private static void CleanupWorldObjectAssetReferences(List<AssetDatabase> assetDatabases)
        {
            List<KeyValuePair<string, WorldObjectAssetReference>> assetsToDestroy = new List<KeyValuePair<string, WorldObjectAssetReference>>();
            List<KeyValuePair<string, WorldObjectAssetReference>> nullSavedAssetReferences = SavedAssetReferences.Where(pair => pair.Value == null).ToList();

            foreach (KeyValuePair<string, WorldObjectAssetReference> pair in nullSavedAssetReferences)
            {
                SavedAssetReferences.Remove(pair.Key);
            }

            List<string> assetDatabaseAssets = new List<string>();

            foreach (AssetDatabase database in assetDatabases)
            {
                assetDatabaseAssets.AddRange(database.Assets.Values.Select(o => o.name));
            }

            foreach (KeyValuePair<string, WorldObjectAssetReference> savedAsset in SavedAssetReferences)
            {
                if (assetDatabaseAssets.Exists(assetName => assetName == savedAsset.Value.Id))
                {
                    continue;
                }

                assetsToDestroy.Add(savedAsset);
            }

            foreach (KeyValuePair<string, WorldObjectAssetReference> asset in assetsToDestroy)
            {
                Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - Destroying {asset.Value.name} as there's no prefab associated with it.");

                HasModifiedAssetsWhenGenerating = true;

                UnityEditor.AssetDatabase.DeleteAsset(asset.Key);
            }
        }

        private static void LoadAllWorldObjectAssetReferences()
        {
            SavedAssetReferences.Clear();

            Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - Loading all {nameof(WorldObjectAssetReference)} assets under {WorldObjectAssetPath}");

            string[] loadAllAssetsAtPath = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(WorldObjectAssetReference)}");

            foreach (string assetGuid in loadAllAssetsAtPath)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
                WorldObjectAssetReference worldObjectAssetReference = UnityEditor.AssetDatabase.LoadAssetAtPath<WorldObjectAssetReference>(assetPath);

                SavedAssetReferences.Add(assetPath, worldObjectAssetReference);
            }
        }

        /// <summary>
        /// Creates the WorldObjectAssetReference asset in the correct path.
        /// </summary>
        /// <param name = "worldObjectAssetReference"></param>
        /// <param name="gameObject">The asset to include in this WorldObjectAsset.</param>
        /// <param name = "assetDatabaseName">The related database.</param>
        private static void UpdateWorldObjectAssetReference(WorldObjectAssetReference worldObjectAssetReference, GameObject gameObject, ref int modifiedCount)
        {
            if (!gameObject.TryGetComponent(out IWorldObjectAsset worldObjectAsset) || worldObjectAsset.Asset == worldObjectAssetReference)
            {
                return;
            }

            worldObjectAsset.Asset = worldObjectAssetReference;

            modifiedCount++;

            SetAssetAndPrefabDirty(worldObjectAssetReference, gameObject);
            
            Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - WorldObjectReferenceAsset reference on {gameObject.name}'s prefab was missing. Fixed.");
        }

        private static void SetAssetAndPrefabDirty(WorldObjectAssetReference worldObjectAssetReference, GameObject gameObject)
        {
            HasModifiedAssetsWhenGenerating = true;
            
            EditorUtility.SetDirty(gameObject);
            EditorUtility.SetDirty(worldObjectAssetReference);
        }
    }
}
#endif