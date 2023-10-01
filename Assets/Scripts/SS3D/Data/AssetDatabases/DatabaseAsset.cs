using SS3D.Logging.LogSettings;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    public class DatabaseAsset
    {
        public readonly string Name;

        public readonly string DatabaseName;

        public GameObject Prefab => Get<GameObject>();

        public GameObject New => GameObject.Instantiate(Prefab);

        public DatabaseAsset(string name, string databaseName)
        {
            Name = name;
            DatabaseName = databaseName;
        }

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