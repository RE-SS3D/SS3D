using System.Collections.Generic;
using Coimbra;
using Object = UnityEngine.Object;
using SS3D.Attributes;

#if UNITY_EDITOR
using UnityEditor.AddressableAssets.Settings;
#endif

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Generic database class, used to create asset databases.
    /// </summary>
    public class AssetDatabase : ScriptableSettings
    {
        public string EnumPath = @"\Scripts\SS3D\Data\Enums";
        public string EnumNamespaceName = "SS3D.Data.Enums";
        public string EnumName;

#if UNITY_EDITOR
        public AddressableAssetGroup AssetGroup;

        [ReadOnly]
#endif
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