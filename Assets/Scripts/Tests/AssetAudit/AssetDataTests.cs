using System.Collections.Generic;
using Coimbra;
using NUnit.Framework;
using SS3D.Data.AssetDatabases;
using System.Linq;
using UnityEngine;

namespace AssetAudit
{
    public class AssetDataTests
    {
        private AssetDatabaseSettings _assetDatabaseSettings;

        [SetUp]
        public void SetUp()
        {
            _assetDatabaseSettings = ScriptableSettings.GetOrFind<AssetDatabaseSettings>();
        }

        /// <summary>
        /// Test to confirm all included asset databases are not null.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllAssetDatabases))]
        public void IncludedAssetDatabasesAreNotNull(AssetDatabase database)
        {
            Assert.IsTrue(database != null);
        }

        /// <summary>
        /// Test to check if there are databases loaded.
        /// </summary>
        [Test]
        public void IncludedAssetDatabasesAreNotEmpty()
        {
            List<AssetDatabase> databases = _assetDatabaseSettings.IncludedAssetDatabases;

            bool databasesAreEmpty = databases.Count == 0;
            Assert.IsFalse(databasesAreEmpty);
        }

        /// <summary>
        /// Test to see if all the project's databases are included.
        /// </summary>
        [Test]
        public void AllProjectAssetDatabasesAreOnIncludedDatabases()
        {
            List<AssetDatabase> projectAssetDatabases = AssetDatabase.FindAllAssetDatabases();
            List<AssetDatabase> loadedAssetDatabases = _assetDatabaseSettings.IncludedAssetDatabases;

            bool hasMissingDatabases = false;
            List<AssetDatabase> missingDatabases = new();

            foreach (AssetDatabase projectAssetDatabase in projectAssetDatabases)
            {
                if (loadedAssetDatabases.Contains(projectAssetDatabase))
                {
                    continue;
                }

                hasMissingDatabases = true;
                missingDatabases.Add(projectAssetDatabase);
            }

            if (hasMissingDatabases)
            {
                foreach (AssetDatabase missingDatabase in missingDatabases)
                { 
                    Debug.Log($"Added asset database {missingDatabase.name} to included asset databases");
                }
            }

            Assert.IsFalse(hasMissingDatabases);
        }

        /// <summary>
        /// Test to see if there is any null references on any database assets.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllAssetDatabases))]
        public void IncludedAssetDatabasesDoNotContainNullObjects(AssetDatabase assetDatabase)
        {
            bool hasNullAssets = false;
            Dictionary<AssetDatabase, List<int>> assetDatabasesNullRefIndexes = new();


            for (int index = 0; index < assetDatabase.Assets.Count; index++)
            {
                Object asset = assetDatabase.Assets.Values.ToList()[index];

                if (asset != null)
                {
                    continue;
                }

                hasNullAssets = true;
                assetDatabasesNullRefIndexes.Add(assetDatabase, new List<int>());

                assetDatabasesNullRefIndexes.TryGetValue(assetDatabase, out List<int> assetIndexes);
                assetIndexes!.Add(index);
            }

            if (hasNullAssets)
            {
                DebugNullAssets(assetDatabasesNullRefIndexes);
            }

            Assert.IsFalse(hasNullAssets);
        }

        /// <summary>
        /// Debugs all the null assets in databases.
        /// </summary>
        /// <param name="assetDatabasesNullRefIndexes"></param>
        private static void DebugNullAssets(Dictionary<AssetDatabase, List<int>> assetDatabasesNullRefIndexes)
        {
            foreach (AssetDatabase assetDatabase in assetDatabasesNullRefIndexes.Keys)
            {
                assetDatabasesNullRefIndexes.TryGetValue(assetDatabase, out List<int> assetIndexes);

                if (assetIndexes == null)
                {
                    continue;
                }

                foreach (int assetIndex in assetIndexes)
                {
                    Debug.Log($"Asset is null on {assetDatabase.name} at index {assetIndex}");
                }
            }
        }

        public static List<AssetDatabase> AllAssetDatabases()
        {
            return ScriptableSettings.GetOrFind<AssetDatabaseSettings>().IncludedAssetDatabases;
        }
    }
}