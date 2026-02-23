#if UNITY_EDITOR
using JetBrains.Annotations;
using SS3D.Logging;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    public partial class ObjectAssetReference
    {
        /// <summary>
        /// The path in the project where to put the ObjectAssetReference assets.
        /// </summary>
        public const string ObjectAssetPath = "Assets/Content/Data/ObjectAssetReferences/";


        [CanBeNull]
        public static ObjectAssetReference Create(Object asset)
        {
            if (!asset)
            {
                Log.Error(typeof(ObjectAssetReference), "No asset found.");

                return null;
            }

            ObjectAssetReference assetReference = CreateInstance<ObjectAssetReference>();
            assetReference.Init(asset);

            string assetPath = System.IO.Path.Combine(ObjectAssetPath, $"{asset.name}.asset");

            UnityEditor.AssetDatabase.CreateAsset(assetReference, assetPath);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(assetReference);

            return assetReference;
        }

        private void Init(Object asset)
        {
            if (!asset)
            {
                Log.Error(this, "No asset found.");

                return;
            }

            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(asset);
            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.FindAssetEntry(guid);

            if (entry == null)
            {
                Log.Error(this, $"Asset {asset.name} with GUID {guid} not found in Addressable Asset Settings.");

                return;
            }

            AssetDatabaseSettings databaseSettings = Coimbra.ScriptableSettings.GetOrFind<AssetDatabaseSettings>();

            if (!databaseSettings)
            {
                Log.Error(this, "AssetDatabaseSettings not found.");

                return;
            }

            foreach (AssetDatabase database in databaseSettings.IncludedAssetDatabases.Where(database => database.AssetGroup == entry.parentGroup))
            {
                Id = guid;
                Database = database.DatabaseID;

                break;
            }
        }
    }
}
#endif