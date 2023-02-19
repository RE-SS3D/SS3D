using System.Collections.Generic;
using Coimbra;
using SS3D.Attributes;

namespace SS3D.Data.AssetDatabases
{
    [ProjectSettings("SS3D")]
    public class AssetDatabaseSettings : ScriptableSettings
    {
#if UNITY_EDITOR
        [ReadOnly]
#endif
        public List<AssetDatabase> IncludedAssetDatabases;
    }
}