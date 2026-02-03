using JetBrains.Annotations;
using SS3D.Attributes;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
  #endif
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
	/// <summary>
	/// This SO is used to reference world object assets in inspector fields without worrying about losing data.
	/// World object assets are anything that can be placed in the world as a GameObject, like items and tileobjects.
	/// </summary>
	public sealed class ObjectAssetReference : ScriptableObject
	{
#if UNITY_EDITOR
        /// <summary>
        /// The path in the project where to put the ObjectAssetReference assets.
        /// </summary>
        public static readonly string WorldObjectAssetPath = "Assets/Content/Data/WorldObjectAssetReferences/";
#endif
        
#if UNITY_EDITOR
		[ReadOnly]
#endif
		[Header("This file is auto-generated, do not modify it manually")]
		public string Id;

#if UNITY_EDITOR
		[ReadOnly]
#endif
		[Header("This file is auto-generated, do not modify it manually")]
		public string Database;

#if UNITY_EDITOR
        [CanBeNull]
        public static ObjectAssetReference Create(Object asset)
        {
            if (!asset)
            {
                Debug.LogError("No asset found.");

                return null;
            }

            ObjectAssetReference assetReference = CreateInstance<ObjectAssetReference>();
            assetReference.Init(asset);

            string assetPath = System.IO.Path.Combine(WorldObjectAssetPath, $"{asset.name}.asset");

            UnityEditor.AssetDatabase.CreateAsset(assetReference, assetPath);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(assetReference);

            return assetReference;
        }

        private void Init(Object asset)
        {
            if (!asset)
            {
                Debug.LogError("No asset found.");

                return;
            }
            
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(asset);
            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
            
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                Debug.LogError($"Asset {asset.name} with GUID {guid} not found in Addressable Asset Settings.");
                return;
            }
            
            AssetDatabaseSettings databaseSettings = Coimbra.ScriptableSettings.GetOrFind<AssetDatabaseSettings>();

            if (!databaseSettings)
            {
                Debug.LogError("AssetDatabaseSettings not found.");
                return;
            }
            
            foreach (AssetDatabase database in databaseSettings.IncludedAssetDatabases.Where(database => database.AssetGroup == entry.parentGroup))
            {
                Id = guid;
                Database = database.DatabaseID;
                break;
            }
        }

        #endif
    }
}