using SS3D.Logging.LogSettings;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Represents an asset data asset, contains the name of the asset and the related database.
    /// </summary>
    public sealed class DatabaseAsset
    {
        /// <summary>
        /// The name of the asset.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The database this asset belongs to.
        /// </summary>
        public readonly string DatabaseName;

        /// <summary>
        /// Returns the prefab of this asset as a GameObject.
        /// </summary>
        public GameObject Prefab => Get<GameObject>();
        
        /// <summary>
        /// Instantiates a new GameObject of this asset's prefab
        /// </summary>
        public GameObject New() => GameObject.Instantiate(Prefab);

        public DatabaseAsset(string name, string databaseName)
        {
            Name = name;
            DatabaseName = databaseName;
        }

        /// <summary>
        /// Gets this asset as T.
        /// </summary>
        /// <typeparam name="T">The type to cast the asset to.</typeparam>
        /// <returns>The casted asset.</returns>
        public T Get<T>()
            where T : Object
        {
            return Assets.Get<T>(DatabaseName, Name);
        } 

        public static implicit operator string(DatabaseAsset asset)
        {
            return asset.Name;
        } 

        public static implicit operator GameObject(DatabaseAsset asset)
        {
            return asset.Get<GameObject>();
        }

        public static implicit operator Sprite(DatabaseAsset asset)
        {
            return asset.Get<Sprite>();
        }

        public static implicit operator LogSettings(DatabaseAsset asset)
        {
            return asset.Get<LogSettings>();
        }
    }
}