#if UNITY_EDITOR
using JetBrains.Annotations;
using SS3D.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityAssetDatabase = UnityEditor.AssetDatabase;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    public partial class AddressablesDatabase
    {
        /// <summary>
        /// The asset group that constitutes this AddressablesDatabase. The system gets every asset from it and adds it to the asset list.
        /// </summary>
        public AddressableAssetGroup AssetGroup;

        /// <summary>
        /// Finds all AddressablesDatabase assets in the project.
        /// </summary>
        public static List<AddressablesDatabase> FindAllAssetDatabases()
        {
            string[] assets = UnityAssetDatabase.FindAssets($"t:{typeof(AddressablesDatabase)}");

            return assets.Select(UnityAssetDatabase.GUIDToAssetPath).Select(UnityAssetDatabase.LoadAssetAtPath<AddressablesDatabase>).ToList();
        }

        /// <summary>
        /// Loads all the assets from the asset group into the stored GUID list.
        /// </summary>
        public void LoadAssetsFromAssetGroup()
        {
            _assetGuids = new();

            foreach (AddressableAssetEntry entry in AssetGroup.entries.Where(entry => !_assetGuids.Contains(entry.guid)))
            {
                _assetGuids.Add(entry.guid);
            }

            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Registers an asset with the backing Addressable group and adds its GUID to this database.
        /// </summary>
        public bool AddToAddressables([NotNull] Object asset)
        {
            string path = UnityAssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(path))
            {
                Log.Error(this, $"Asset {asset.name} does not have a valid path, cannot add to addressables.");

                return false;
            }

            string guid = UnityAssetDatabase.AssetPathToGUID(path);

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

            UnityAssetDatabase.SaveAssetIfDirty(asset);
            UnityAssetDatabase.SaveAssetIfDirty(this);

            return true;
        }

        /// <summary>
        /// Records a GUID for the given asset by resolving its Addressables path.
        /// </summary>
        private void Add<TAsset>([NotNull] TAsset asset)
            where TAsset : Object
        {
            string path = UnityAssetDatabase.GUIDToAssetPath(asset.name);
            string guid = UnityAssetDatabase.GUIDFromAssetPath(path).ToString();

            _assetGuids.Add(guid);
        }
    }
}
#endif