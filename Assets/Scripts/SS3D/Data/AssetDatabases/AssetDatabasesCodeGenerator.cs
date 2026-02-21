#if UNITY_EDITOR
using Coimbra;
using JetBrains.Annotations;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Calls all the methods for generating code for the AssetData system.
    /// </summary>
    public static class AssetDatabasesCodeGenerator
    {
        private static readonly Dictionary<string, ObjectAssetReference> SavedAssetReferences = new Dictionary<string, ObjectAssetReference>();

        private static bool HasModifiedAssetsWhenGenerating;

        /// <summary>
        /// Generates all the code needed for the asset data system to work.
        /// </summary>
        public static void GenerateAssetDatabasesCode()
        {
            AssetDatabaseSettings settings = ScriptableSettings.GetOrFind<AssetDatabaseSettings>(); 

            if (!settings)
            {
                Log.Error("{AssetDatabasesCodeGeneratorName} - Asset database settings has not be found", nameof(AssetDatabasesCodeGenerator));
                return;
            }

            if (settings.IncludedAssetDatabases == null || settings.IncludedAssetDatabases.Count == 0)
            {
                Log.Error("{AssetDatabasesCodeGeneratorName} - No Databases have been found", nameof(AssetDatabasesCodeGenerator));
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

            foreach ((string guid, Object asset) in assetDatabase.Assets)
            {
                if (asset is not GameObject gameObject)
                {
                    continue;
                }
                
                ObjectAssetReference objectAssetReference = SavedAssetReferences.Values.ToList().Find(reference => reference.Id == guid && reference.Database == assetDatabase.DatabaseID);

                if (objectAssetReference)
                {
                    UpdateWorldObjectAssetReference(objectAssetReference, gameObject, ref modifiedAssets);
                }
                else
                {
                    objectAssetReference = CreateWorldObjectAssetReference(gameObject.name, guid, assetDatabase.DatabaseID);

                    string key = $"{ObjectAssetReference.ObjectAssetPath}{gameObject.name}.asset";

                    if (!SavedAssetReferences.TryAdd(key, objectAssetReference))
                    {
                        Log.Error("[{AssetDatabasesCodeGeneratorName}] - {Key} is already on the dictionary", nameof(AssetDatabasesCodeGenerator), key);
                        continue;
                    }

                    UpdateWorldObjectAssetReference(objectAssetReference, gameObject, ref createdAssets);
                }
            }

            if (createdAssets > 0)
            {
                Log.Information("[{AssetDatabasesCodeGeneratorName}] - {CreatedAssets} {ObjectAssetReferenceName} created for {AssetDatabaseDatabaseName}.", nameof(AssetDatabasesCodeGenerator), createdAssets, nameof(ObjectAssetReference), assetDatabase.DatabaseName);
            }

            if (modifiedAssets > 0)
            {
                Log.Information("[{AssetDatabasesCodeGeneratorName}] - {ModifiedAssets} {ObjectAssetReferenceName} modified for {AssetDatabaseDatabaseName}.", nameof(AssetDatabasesCodeGenerator), modifiedAssets, nameof(ObjectAssetReference), assetDatabase.DatabaseName);
            }

            if (modifiedAssets == 0 && createdAssets == 0)
            {
                Log.Information("[{AssetDatabasesCodeGeneratorName}] - No {ObjectAssetReferenceName} were modified or created for {AssetDatabaseDatabaseName}.", nameof(AssetDatabasesCodeGenerator), nameof(ObjectAssetReference), assetDatabase.DatabaseName);
            }
        }

        [NotNull]
        private static ObjectAssetReference CreateWorldObjectAssetReference(string fileName, string gameObjectID, string assetDatabaseName)
        {
            ObjectAssetReference objectAssetReference = ScriptableObject.CreateInstance<ObjectAssetReference>();

            objectAssetReference.Id = gameObjectID;
            objectAssetReference.Database = assetDatabaseName;

            UnityEditor.AssetDatabase.CreateAsset(objectAssetReference, $"{ObjectAssetReference.ObjectAssetPath}{fileName}.asset");

            HasModifiedAssetsWhenGenerating = true;

            Log.Information("[{AssetDatabasesCodeGeneratorName}] - Creating {FileName} WorldObjectReferenceAsset as it was missing.", nameof(AssetDatabasesCodeGenerator), fileName);

            return objectAssetReference;
        }

        private static void CleanupWorldObjectAssetReferences(List<AssetDatabase> assetDatabases)
        {
            List<KeyValuePair<string, ObjectAssetReference>> assetsToDestroy = new();
            List<KeyValuePair<string, ObjectAssetReference>> nullSavedAssetReferences = SavedAssetReferences.Where(pair => !pair.Value).ToList();

            foreach (KeyValuePair<string, ObjectAssetReference> pair in nullSavedAssetReferences)
            {
                SavedAssetReferences.Remove(pair.Key);
            }

            Dictionary<string, string> assetsInDatabases = new();

            foreach (AssetDatabase database in assetDatabases)
            {
                foreach (string key in database.Assets.Keys)
                {
                    assetsInDatabases.TryAdd(key, database.DatabaseID);
                }
            }

            foreach (KeyValuePair<string, ObjectAssetReference> savedAsset in SavedAssetReferences)
            {
                if (assetsInDatabases.TryGetValue(savedAsset.Value.Id, out string databaseID) && databaseID == savedAsset.Value.Database)
                {
                    continue;
                }

                assetsToDestroy.Add(savedAsset);
            }

            foreach (KeyValuePair<string, ObjectAssetReference> asset in assetsToDestroy)
            {
                Log.Information("[{AssetDatabasesCodeGeneratorName}] - Destroying {ValueName} as there's no prefab associated with it.", nameof(AssetDatabasesCodeGenerator), asset.Value.name);

                HasModifiedAssetsWhenGenerating = true;

                UnityEditor.AssetDatabase.DeleteAsset(asset.Key);
            }
        }

        private static void LoadAllWorldObjectAssetReferences()
        {
            SavedAssetReferences.Clear();

            Log.Information("[{AssetDatabasesCodeGeneratorName}] - Loading all {ObjectAssetReferenceName} assets under {Path}", nameof(AssetDatabasesCodeGenerator), nameof(ObjectAssetReference), ObjectAssetReference.ObjectAssetPath);

            string[] loadAllAssetsAtPath = UnityEditor.AssetDatabase.FindAssets($"t:{nameof(ObjectAssetReference)}");

            foreach (string assetGuid in loadAllAssetsAtPath)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(assetGuid);
                ObjectAssetReference objectAssetReference = UnityEditor.AssetDatabase.LoadAssetAtPath<ObjectAssetReference>(assetPath);

                SavedAssetReferences.Add(assetPath, objectAssetReference);
            }
        }

        /// <summary>
        /// Creates the ObjectAssetReference asset in the correct path.
        /// </summary>
        /// <param name = "objectAssetReference"></param>
        /// <param name="gameObject">The asset to include in this WorldObjectAsset.</param>
        /// <param name="modifiedCount">reference for modified objects count</param>
        private static void UpdateWorldObjectAssetReference(ObjectAssetReference objectAssetReference, GameObject gameObject, ref int modifiedCount)
        {
            if (!gameObject.TryGetComponent(out IWorldObjectAsset worldObjectAsset) || worldObjectAsset.Asset == objectAssetReference)
            {
                return;
            }

            worldObjectAsset.Asset = objectAssetReference;

            modifiedCount++;

            SetAssetAndPrefabDirty(objectAssetReference, gameObject);
            
            Log.Information("[{AssetDatabasesCodeGeneratorName}] - WorldObjectReferenceAsset reference on {GameObjectName}'s prefab was missing. Fixed.", nameof(AssetDatabasesCodeGenerator), gameObject.name);
        }

        private static void SetAssetAndPrefabDirty(ObjectAssetReference objectAssetReference, GameObject gameObject)
        {
            HasModifiedAssetsWhenGenerating = true;
            
            EditorUtility.SetDirty(gameObject);
            EditorUtility.SetDirty(objectAssetReference);
        }
    }
}
#endif