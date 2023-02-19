using System.Collections.Generic;
using Coimbra;
using SS3D.Attributes;

namespace SS3D.Data.AssetDatabases
{
    [ProjectSettings("SS3D")]
    public class AssetDatabaseSettings : ScriptableSettings
    {
        [ReadOnly]
        public List<AssetDatabase> IncludedAssetDatabases;
    }
}