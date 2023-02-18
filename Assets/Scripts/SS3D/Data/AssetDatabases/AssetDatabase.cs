using System.Collections.Generic;
using Coimbra;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets.Settings;
#endif
using Object = UnityEngine.Object;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Generic database class, used to create asset databases.
    /// </summary>                                             s
    public class AssetDatabase : ScriptableSettings
    {
        public string EnumPath = @"\Scripts\SS3D\Data\Enums";
        public string EnumNamespaceName = "SS3D.Data.Enums";
        public string EnumName;

#if UNITY_EDITOR
        public AddressableAssetGroup AssetGroup;
#endif

        [ReadOnly]
        public List<Object> Assets;

#if UNITY_EDITOR
        public void GetAssetNames()
        {
            Assets.Clear();

            foreach (AddressableAssetEntry entry in AssetGroup.entries)
            {
                Assets.Add(entry.MainAsset);
            }
        }
#endif

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
            return Assets[index] as T;
        }
    }
}