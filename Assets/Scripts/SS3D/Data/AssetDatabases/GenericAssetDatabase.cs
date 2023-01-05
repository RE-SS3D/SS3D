using System.Collections.Generic;
using Coimbra;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Generic database class, used to create
    /// </summary>                                             s
    /// <typeparam name="T"></typeparam>
    // [CreateAssetMenu(menuName = "AssetData/InteractionIcons", fileName = "InteractionIcons")]
    public class GenericAssetDatabase : ScriptableSettings
    {
        public string EnumName;
        public List<AssetReference> Assets;

        public virtual void PreloadAssets()
        {

        }

        /// <summary>
        /// Pre-loads assets in memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        protected void PreloadAssets<T>() where T : Object
        {
            foreach (AssetReference assetReference in Assets)
            {
                assetReference.LoadAssetAsync<T>();
            }
        }

        /// <summary>
        /// Gets an asset based on its ID (index).
        /// </summary>
        /// <param name="icon"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T Get<T>(int icon) where T : Object
        {
            return Assets[icon].Asset as T;
        }
    }
}