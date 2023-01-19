using System;
using System.Collections.Generic;
using Coimbra;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Generic database class, used to create asset databases.
    /// </summary>                                             s
    public class GenericAssetDatabase : ScriptableSettings
    {
        public event Action OnDatabaseLoaded; 

        public string EnumPath = @"\Scripts\SS3D\Data\Enums";
        public string EnumNamespaceName = "SS3D.Data.Enums";
        public string EnumName;

        public List<AssetReference> Assets;

        private bool _loaded;

        /// <summary>
        /// Pre-loads all the assets in the database in memory.
        /// </summary>
        public virtual void PreloadAssets() { }

        /// <summary>
        /// Pre-loads assets in memory.
        /// </summary>
        /// <typeparam name="T">The type of asset to load.</typeparam>
        protected void PreloadAssets<T>() where T : Object
        {
            if (_loaded)
            {
                return;
            }

            foreach (AssetReference assetReference in Assets)
            {
                assetReference.LoadAssetAsync<T>();
            }

            _loaded = true;
            OnDatabaseLoaded?.Invoke();
        }

        /// <summary>
        /// Gets an asset based on its ID (index).
        ///
        /// WARNING: Not sure how
        /// </summary>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>(int index) where T : Object
        {
            if (!_loaded)
            {
                PreloadAssets();
            }

            return Assets[index].Asset as T;
        }
    }
}