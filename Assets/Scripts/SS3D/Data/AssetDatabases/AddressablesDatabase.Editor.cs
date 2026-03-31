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
            AssetGuids = new();

            foreach (AddressableAssetEntry entry in AssetGroup.entries)
            {
                Assets.TryAdd(entry.guid, entry.MainAsset);

                if (!AssetGuids.Contains(entry.guid))
                {
                    AssetGuids.Add(entry.guid);
                }
            }

            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Adds an asset to the asset database. Should be used only for additional content or runtime stuff.
        /// </summary>
        /// <param name="asset">The asset to add to the database.</param>
        /// <typeparam name="TAsset">The type of the asset to add.</typeparam>
        public void Add<TAsset>([NotNull] TAsset asset)
            where TAsset : Object
        {
            string path = AssetDatabase.GUIDToAssetPath(asset.name);
            string guid = AssetDatabase.GUIDFromAssetPath(path).ToString();

            Assets.Add(guid, asset);
            AssetGuids.Add(guid);
        }

        /// <summary>
        /// Initializes all asset databases in the project and adds to the databases list.
        /// </summary>
        public static List<AddressablesDatabase> FindAllAssetDatabases()
        {
            string[] assets = AssetDatabase.FindAssets($"t:{typeof(AddressablesDatabase)}");

            return assets.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<AddressablesDatabase>).ToList();
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

            DatabaseScriptCreator.CreateAtPath(DatabaseAssetPath, DatabaseName, AssetGuids, DatabaseAssetNamespaceName);
        }

        public bool AddToAddressables([NotNull] Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(path))
            {
                Log.Error(this, $"Asset {asset.name} does not have a valid path, cannot add to addressables.");

                return false;
            }

            string guid = AssetDatabase.AssetPathToGUID(path);

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

            AssetDatabase.SaveAssetIfDirty(asset);
            AssetDatabase.SaveAssetIfDirty(this);

            return true;
        }
    }
}
  #endif