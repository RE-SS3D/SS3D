using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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

        public bool AllAssetsLoaded;

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
            if (AllAssetsLoaded)
            {
                return;
            }

            foreach (AssetReference assetReference in Assets)
            {
                assetReference.LoadAssetAsync<T>();
            }

#pragma warning disable CS4014
            WaitUntilAllAssetsAreLoaded();
#pragma warning restore CS4014
        }

        /// <summary>
        /// Async Task to wait until all the assets are loaded.
        /// </summary>
        private async UniTaskVoid WaitUntilAllAssetsAreLoaded()
        {
            await UniTask.WaitUntil(() => Assets.All(reference => reference.Asset != null));

            AllAssetsLoaded = true;
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
            return Assets[index].Asset as T;
        }
    }
}