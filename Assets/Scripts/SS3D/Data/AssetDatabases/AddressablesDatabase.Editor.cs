#if UNITY_EDITOR
using JetBrains.Annotations;
using SS3D.CodeGeneration.Creators;
using SS3D.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    public partial class AddressablesDatabase
    {
        /// <summary>
        /// The path that the enum will be generated to.
        /// </summary>
        public const string DatabaseAssetPath = @"\Scripts\SS3D\Data\Generated";

        /// <summary>
        ///  The namespace that will be included on the generated Enum.
        /// </summary>
        public const string DatabaseAssetNamespaceName = "SS3D.Data.Generated";

        /// <summary>
        /// The asset group that constitutes this AddressablesDatabase, the system gets every asset from it and adds to an asset list.
        /// </summary>
        public AddressableAssetGroup AssetGroup;

        /// <summary>
        /// Loads all the assets from the asset group to the Assets list.
        /// </summary>
        public void LoadAssetsFromAssetGroup()
        {
            Assets = new();
            AssetReferences = new();

            foreach (AddressableAssetEntry entry in AssetGroup.entries)
            {
                Assets.TryAdd(entry.guid, entry.MainAsset);
                AssetReferences.TryAdd(entry.guid, new(entry.guid));
            }

            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Adds abd asset to the asset database. Should be used only for additional content or runtime stuff.
        /// </summary>
        /// <param name="asset"></param>
        /// <typeparam name="TAsset"></typeparam>
        public void Add<TAsset>([NotNull] TAsset asset)
            where TAsset : Object
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(asset.name);
            string guid = UnityEditor.AssetDatabase.GUIDFromAssetPath(path).ToString();

            Assets.Add(guid, asset);
            AssetReferences.Add(guid, new(guid));
        }

        /// <summary>
        /// Initializes all asset databases in the project and adds to the databases list.
        /// </summary>
        public static List<AddressablesDatabase> FindAllAssetDatabases()
        {
            string[] assets = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(AddressablesDatabase)}");

            List<AddressablesDatabase> databases = new();

            for (int index = 0; index < assets.Length; index++)
            {
                string database = assets[index];
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(database);
                AddressablesDatabase addressablesDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<AddressablesDatabase>(assetPath);

                databases.Add(addressablesDatabase);
            }

            return databases;
        }

        /// <summary>
        /// Generates a script with the data of this database for easy access.
        /// </summary>
        public void GenerateDatabaseCode()
        {
            if (AssetDatabaseSettings.SkipCodeGeneration)
            {
                return;
            }

            DatabaseScriptCreator.CreateAtPath(DatabaseAssetPath, DatabaseName, Assets.Values.ToList(), DatabaseAssetNamespaceName);
        }

        public bool AddToAddressables([NotNull] Object asset)
        {
            string path = UnityEditor.AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(path))
            {
                Log.Error(this, $"Asset {asset.name} does not have a valid path, cannot add to addressables.");

                return false;
            }

            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);

            if (!AssetGroup)
            {
                Log.Error(this, $"Addressable Asset Group {name} not found, cannot add asset {asset.name} to addressables.");

                return false;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.FindAssetEntry(guid);

            if (entry != null)
            {
                Log.Warning(this, $"Asset {asset.name} is already in Addressable Group {entry.parentGroup.name}.");

                return false;
            }

            settings.CreateOrMoveEntry(guid, AssetGroup);

            Add(asset);

            EditorUtility.SetDirty(asset);
            EditorUtility.SetDirty(this);

            UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);

            return true;
        }
    }
}
  #endif