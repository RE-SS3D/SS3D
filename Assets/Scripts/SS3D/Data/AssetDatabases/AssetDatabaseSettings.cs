using System.Collections.Generic;
using Coimbra;
using SS3D.Attributes;

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
    }
}