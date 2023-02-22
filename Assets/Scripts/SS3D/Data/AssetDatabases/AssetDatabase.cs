﻿using System.Collections.Generic;
using Object = UnityEngine.Object;
using SS3D.Attributes;
using SS3D.Logging;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.AddressableAssets.Settings;
#endif

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Generic database class, used to create asset databases.
    /// </summary>
    public class AssetDatabase : ScriptableObject
    {
        /// <summary>
        /// The path that the enum will be generated to.
        /// </summary>
        public const string EnumPath = @"\Scripts\SS3D\Data\Enums";

        /// <summary>
        ///  The namespace that will be included on the generated Enum.
        /// </summary>
        public string EnumNamespaceName = "SS3D.Data.Enums";

        /// <summary>
        ///  The name that the generated enum will have;
        /// </summary>
        public string EnumName;

#if UNITY_EDITOR
        /// <summary>
        /// The asset group that constitutes this AssetDatabase, the system gets every asset from it and adds to an asset list.
        /// </summary>
        public AddressableAssetGroup AssetGroup;

        /// <summary>
        /// All loaded assets that will be included in the built game.
        /// </summary>
        [ReadOnly]
#endif
        public List<Object> Assets;

#if UNITY_EDITOR
        /// <summary>
        /// Loads all the assets from the asset group to the Assets list.
        /// </summary>
        public void LoadAssetsFromAssetGroup()
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
        /// <param name="index">Uses the ID of the asset cast into a int to get the asset from a list position.</param>
        /// <typeparam name="T">The type of asset to get.</typeparam>
        /// <returns></returns>
        public T Get<T>(int index) where T : Object
        {
            return Assets[index] as T;
        }
    }
}