#if UNITY_EDITOR
using Coimbra;
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
                        Debug.LogError($"[{nameof(AssetDatabasesCodeGenerator)}] - {key} is already on the dictionary");
                        continue;
                    }

                    UpdateWorldObjectAssetReference(objectAssetReference, gameObject, ref createdAssets);
                }
            }

            if (createdAssets > 0)
            {
                Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - {createdAssets} {nameof(ObjectAssetReference)} created for {assetDatabase.DatabaseName}.");
            }

            if (modifiedAssets > 0)
            {
                Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - {modifiedAssets} {nameof(ObjectAssetReference)} modified for {assetDatabase.DatabaseName}.");
            }

            if (modifiedAssets == 0 && createdAssets == 0)
            {
                Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - No {nameof(ObjectAssetReference)} were modified or created for {assetDatabase.DatabaseName}.");
            }
        }

        private static ObjectAssetReference CreateWorldObjectAssetReference(string fileName, string gameObjectID, string assetDatabaseName)
        {
            ObjectAssetReference objectAssetReference = ScriptableObject.CreateInstance<ObjectAssetReference>();

            objectAssetReference.Id = gameObjectID;
            objectAssetReference.Database = assetDatabaseName;

            UnityEditor.AssetDatabase.CreateAsset(objectAssetReference, $"{ObjectAssetReference.ObjectAssetPath}{fileName}.asset");

            HasModifiedAssetsWhenGenerating = true;

            Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - Creating {fileName} WorldObjectReferenceAsset as it was missing.");

            return objectAssetReference;
        }

        private static void CleanupWorldObjectAssetReferences(List<AssetDatabase> assetDatabases)
        {
            List<KeyValuePair<string, ObjectAssetReference>> assetsToDestroy = new();
            List<KeyValuePair<string, ObjectAssetReference>> nullSavedAssetReferences = SavedAssetReferences.Where(pair => pair.Value == null).ToList();

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
                Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - Destroying {asset.Value.name} as there's no prefab associated with it.");

                HasModifiedAssetsWhenGenerating = true;

                UnityEditor.AssetDatabase.DeleteAsset(asset.Key);
            }
        }

        private static void LoadAllWorldObjectAssetReferences()
        {
            SavedAssetReferences.Clear();

            Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - Loading all {nameof(ObjectAssetReference)} assets under {ObjectAssetReference.ObjectAssetPath}");

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
        /// <param name = "assetDatabaseName">The related database.</param>
        private static void UpdateWorldObjectAssetReference(ObjectAssetReference objectAssetReference, GameObject gameObject, ref int modifiedCount)
        {
            if (!gameObject.TryGetComponent(out IWorldObjectAsset worldObjectAsset) || worldObjectAsset.Asset == objectAssetReference)
            {
                return;
            }

            worldObjectAsset.Asset = objectAssetReference;

            modifiedCount++;

            SetAssetAndPrefabDirty(objectAssetReference, gameObject);
            
            Debug.Log($"[{nameof(AssetDatabasesCodeGenerator)}] - WorldObjectReferenceAsset reference on {gameObject.name}'s prefab was missing. Fixed.");
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