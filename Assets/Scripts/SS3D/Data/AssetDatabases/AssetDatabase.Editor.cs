#if UNITY_EDITOR
using SS3D.CodeGeneration.Creators;
using System.Linq;

namespace SS3D.Data.AssetDatabases
{
    public abstract partial class AssetDatabase
    {
        /// <summary>
        /// The path that the enum will be generated to.
        /// </summary>
        public const string DatabaseAssetPath = @"\Scripts\SS3D\Data\Generated";

        /// <summary>
        /// The namespace that will be included on the generated Enum.
        /// </summary>
        public const string DatabaseAssetNamespaceName = "SS3D.Data.Generated";

        /// <summary>
        /// Generates a script with the data of this database for easy access.
        /// </summary>
        public void GenerateDatabaseCode()
        {
            if (AssetDatabaseSettings.SkipCodeGeneration)
            {
                return;
            }

            DatabaseScriptCreator.CreateAtPath(DatabaseAssetPath, name, AssetGuids.ToList(), DatabaseAssetNamespaceName);
        }
    }
}
#endif