#if UNITY_EDITOR
using Coimbra;
using JetBrains.Annotations;
using SS3D.Logging;
using System.Linq;
using UnityAssetDatabase = UnityEditor.AssetDatabase;
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

            UnityAssetDatabase.CreateAsset(assetReference, assetPath);
            UnityAssetDatabase.SaveAssetIfDirty(assetReference);

            return assetReference;
        }

        private void Init(Object asset)
        {
            if (!asset)
            {
                Log.Error(this, "No asset found.");

                return;
            }

            string assetPath = UnityAssetDatabase.GetAssetPath(asset);
            string guid = UnityAssetDatabase.AssetPathToGUID(assetPath);
            Id = guid;
            
            AssetDatabaseSettings settings = ScriptableSettings.GetOrFind<AssetDatabaseSettings>();

            if (!settings.Has(guid))
            {
                Log.Error(this, $"Asset {asset.name} with GUID {guid} not found in any included database.");
            }
        }
    }
}
#endif