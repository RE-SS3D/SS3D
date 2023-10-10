using System.Collections.Generic;
using Coimbra;
using SS3D.Attributes;
using SS3D.CodeGeneration;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    [ProjectSettings("SS3D/Assets")]
    public sealed class AssetDatabaseSettings : ScriptableSettings
    {
        /// <summary>
        /// Included databases on the game.
        /// </summary>
#if UNITY_EDITOR
        [ReadOnly]
#endif
        public List<AssetDatabase> IncludedAssetDatabases;

#if UNITY_EDITOR
        [SerializeField]
        private bool _skipCodeGeneration;

        /// <summary>
        /// If ticked the asset data system won't try to generate the generated asset data code.
        /// </summary>
        public static bool SkipCodeGeneration => GetOrFind<AssetDatabaseSettings>()._skipCodeGeneration;
#endif

#if UNITY_EDITOR
        /// <summary>
        /// Generates the script with the data from this database.
        /// </summary>
        public void GenerateCode()
        {
            if (SkipCodeGeneration)
            {
                return;
            }

            const string dataPath = AssetDatabase.EnumPath;

            DatabaseAssetCreator.CreateAtPath(dataPath, typeof(DatabaseAsset), "AssetDatabases", IncludedAssetDatabases);
        }
#endif
    }
}