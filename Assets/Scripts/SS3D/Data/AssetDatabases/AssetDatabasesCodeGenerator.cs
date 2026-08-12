#if UNITY_EDITOR
using Coimbra;
using JetBrains.Annotations;
using SS3D.Data;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using UnityAssetDatabase = UnityEditor.AssetDatabase;
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

            List<AssetDatabase> allDatabases = settings.AllDatabases.ToList();

            if (allDatabases.Count == 0)
            {
                Log.Error("{AssetDatabasesCodeGeneratorName} - No Databases have been found", nameof(AssetDatabasesCodeGenerator));

                return;
            }

            SavedAssetReferences.Clear();

            settings.CreateDatabaseCode();

            HasModifiedAssetsWhenGenerating = false;

            LoadAllObjectAssetReferences();

            foreach (AssetDatabase includedAssetDatabase in allDatabases)
            {
                CreateDatabaseCode(includedAssetDatabase);

                CreateObjectAssetReferences(includedAssetDatabase);
            }

            CleanupObjectAssetReferences(allDatabases);
        }

        /// <summary>
        /// Calls the method to generate all the code for a database.
        /// </summary>
        private static void CreateDatabaseCode(AssetDatabase database)
        {
            database.GenerateDatabaseCode();
        }

        /// <summary>
        /// Creates all the ObjectAssetReferences for a database.
        /// </summary>
        private static void CreateObjectAssetReferences(AssetDatabase database)
        {
            int createdAssets = 0;
            int modifiedAssets = 0;

            foreach (string guid in database.AssetGuids)
            {
                string path = UnityAssetDatabase.GUIDToAssetPath(guid);
                Object asset = UnityAssetDatabase.LoadAssetAtPath<Object>(path);
                
                if (asset is not GameObject gameObject)
                {
                    continue;
                }

                if (AssetPrefabStamper.StampAssetPrefab(gameObject, guid))
                {
                    HasModifiedAssetsWhenGenerating = true;
                }

                ObjectAssetReference objectAssetReference =
                    SavedAssetReferences.Values.ToList().Find(reference => reference.Id == guid);

                if (!objectAssetReference)
                {
                    objectAssetReference = CreateObjectAssetReference(gameObject);

                    string key = $"{ObjectAssetReference.ObjectAssetPath}{gameObject.name}.asset";

                    if (!SavedAssetReferences.TryAdd(key, objectAssetReference))
                    {
                        Log.Error("[{AssetDatabasesCodeGeneratorName}] - {Key} is already on the dictionary", nameof(AssetDatabasesCodeGenerator), key);

                        continue;
                    }

                    UpdateObjectAssetReference(objectAssetReference, gameObject, ref createdAssets);
                }
                else
                {
                    UpdateObjectAssetReference(objectAssetReference, gameObject, ref modifiedAssets);
                }
            }

            if (createdAssets > 0)
            {
                Log.Information("[{AssetDatabasesCodeGeneratorName}] - {CreatedAssets} {ObjectAssetReferenceName} created for {AssetDatabaseDatabaseName}.",
                    nameof(AssetDatabasesCodeGenerator), createdAssets, nameof(ObjectAssetReference), database.name);
            }

            if (modifiedAssets > 0)
            {
                Log.Information("[{AssetDatabasesCodeGeneratorName}] - {ModifiedAssets} {ObjectAssetReferenceName} modified for {AssetDatabaseDatabaseName}.",
                    nameof(AssetDatabasesCodeGenerator), modifiedAssets, nameof(ObjectAssetReference), database.name);
            }

            if (modifiedAssets == 0 && createdAssets == 0)
            {
                Log.Information("[{AssetDatabasesCodeGeneratorName}] - No {ObjectAssetReferenceName} were modified or created for {AssetDatabaseDatabaseName}.",
                    nameof(AssetDatabasesCodeGenerator), nameof(ObjectAssetReference), database.name);
            }
        }

        [NotNull]
        private static ObjectAssetReference CreateObjectAssetReference([NotNull] GameObject gameObject)
        {
            ObjectAssetReference objectAssetReference = ObjectAssetReference.Create(gameObject);

            HasModifiedAssetsWhenGenerating = true;

            Log.Information(
                "[{AssetDatabasesCodeGeneratorName}] - Creating {FileName} ObjectReferenceAsset as it was missing.", 
                nameof(AssetDatabasesCodeGenerator),
                gameObject.name);

            return objectAssetReference;
        }

        private static void CleanupObjectAssetReferences(IEnumerable<AssetDatabase> assetDatabases)
        {
            List<KeyValuePair<string, ObjectAssetReference>> nullSavedAssetReferences = SavedAssetReferences.Where(pair => !pair.Value).ToList();

            foreach (KeyValuePair<string, ObjectAssetReference> pair in nullSavedAssetReferences)
            {
                SavedAssetReferences.Remove(pair.Key);
            }

            HashSet<string> databaseAssetGuids = new();

            foreach (string key in assetDatabases.SelectMany(database => database.AssetGuids))
            {
                databaseAssetGuids.Add(key);
            }

            List<KeyValuePair<string, ObjectAssetReference>> assetsToDestroy = 
                SavedAssetReferences.Where(savedAsset => !databaseAssetGuids.Contains(savedAsset.Value.Id)).ToList();

            foreach (KeyValuePair<string, ObjectAssetReference> asset in assetsToDestroy)
            {
                Log.Information("[{AssetDatabasesCodeGeneratorName}] - Destroying {ValueName} as there's no prefab associated with it.", nameof(AssetDatabasesCodeGenerator),
                    asset.Value.name);

                HasModifiedAssetsWhenGenerating = true;

                UnityAssetDatabase.DeleteAsset(asset.Key);
            }
        }

        private static void LoadAllObjectAssetReferences()
        {
            SavedAssetReferences.Clear();

            Log.Information("[{AssetDatabasesCodeGeneratorName}] - Loading all {ObjectAssetReferenceName} assets under {Path}", nameof(AssetDatabasesCodeGenerator),
                nameof(ObjectAssetReference), ObjectAssetReference.ObjectAssetPath);

            string[] loadAllAssetsAtPath = UnityAssetDatabase.FindAssets($"t:{nameof(ObjectAssetReference)}");

            foreach (string assetGuid in loadAllAssetsAtPath)
            {
                string assetPath = UnityAssetDatabase.GUIDToAssetPath(assetGuid);
                ObjectAssetReference objectAssetReference = UnityAssetDatabase.LoadAssetAtPath<ObjectAssetReference>(assetPath);

                SavedAssetReferences.Add(assetPath, objectAssetReference);
            }
        }

        /// <summary>
        /// Creates the ObjectAssetReference asset in the correct path.
        /// </summary>
        /// <param name = "objectAssetReference"></param>
        /// <param name="gameObject">The asset to include in this WorldObjectAsset.</param>
        /// <param name="modifiedCount">reference for modified objects count</param>
        private static void UpdateObjectAssetReference(ObjectAssetReference objectAssetReference, GameObject gameObject, ref int modifiedCount)
        {
            if (!gameObject.TryGetComponent(out IWorldObjectAsset objectAsset) || objectAsset.Asset == objectAssetReference)
            {
                return;
            }

            objectAsset.Asset = objectAssetReference;

            modifiedCount++;

            SetAssetAndPrefabDirty(objectAssetReference, gameObject);

            Log.Information("[{AssetDatabasesCodeGeneratorName}] - ObjectReferenceAsset reference on {GameObjectName}'s prefab was missing. Fixed.",
                nameof(AssetDatabasesCodeGenerator), gameObject.name);
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