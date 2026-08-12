using System.Collections.Generic;
using Coimbra;
using JetBrains.Annotations;
using NUnit.Framework;
using SS3D.Data.AssetDatabases;
using System.Linq;
using UnityAssetDatabase = UnityEditor.AssetDatabase;
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
        /// Test to confirm all included asset catalogs are not null.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllAssetCatalogs))]
        public void IncludedAssetCatalogsAreNotNull(AssetCatalog catalog)
        {
            Assert.IsTrue(catalog);
        }

        /// <summary>
        /// Test to confirm all included asset databases are not null.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllAssetDatabases))]
        public void IncludedAssetDatabasesAreNotNull(AssetDatabase database)
        {
            Assert.IsTrue(database);
        }

        /// <summary>
        /// Test to check if there are any catalogs loaded.
        /// </summary>
        [Test]
        public void IncludedAssetCatalogsAreNotEmpty()
        {
            Assert.IsTrue(_assetDatabaseSettings.IncludedCatalogs.Any());
        }

        /// <summary>
        /// Test to check if there are databases loaded.
        /// </summary>
        [Test]
        public void IncludedAssetDatabasesAreNotEmpty()
        {
            Assert.IsTrue(_assetDatabaseSettings.AllDatabases.Any());
        }

        /// <summary>
        /// Test to see if all the project's databases are included.
        /// </summary>
        [Test]
        public void AllProjectAssetDatabasesAreOnIncludedDatabases()
        {
            List<AssetDatabase> projectAssetDatabases = UnityAssetDatabase
                .FindAssets($"t:{nameof(AssetDatabase)}")
                .Select(UnityAssetDatabase.GUIDToAssetPath)
                .Select(UnityAssetDatabase.LoadAssetAtPath<AssetDatabase>)
                .Where(database => database)
                .ToList();

            HashSet<AssetDatabase> loadedAssetDatabases = new(_assetDatabaseSettings.AllDatabases);

            List<AssetDatabase> missingDatabases = projectAssetDatabases
                .Where(database => !loadedAssetDatabases.Contains(database))
                .ToList();

            foreach (AssetDatabase missingDatabase in missingDatabases)
            {
                Debug.Log($"Missing asset database {missingDatabase.name} \u2014 add it to a catalog in AssetDatabaseSettings.");
            }

            Assert.IsFalse(missingDatabases.Any());
        }

        /// <summary>
        /// Test to see if there is any null references on any database assets.
        /// </summary>
        [Test]
        [TestCaseSource(nameof(AllAssetDatabases))]
        public void IncludedAssetDatabasesDoNotContainNullObjects([NotNull] AssetDatabase database)
        {
            List<string> nullGuids = (
                from guid in database.AssetGuids
                let path = UnityAssetDatabase.GUIDToAssetPath(guid)
                let asset = UnityAssetDatabase.LoadAssetAtPath<Object>(path)
                where !asset
                select guid).ToList();

            if (!nullGuids.Any())
            {
                Assert.Pass();

                return;
            }

            foreach (string nullGuid in nullGuids)
            {
                Debug.LogError($"Asset is null on {database.name} : {nullGuid}");
            }

            Assert.Fail($"{database.name} has null assets");
        }

        [NotNull]
        private static List<AssetDatabase> AllAssetDatabases() => ScriptableSettings.GetOrFind<AssetDatabaseSettings>().AllDatabases.ToList();
        
        private static List<AssetCatalog> AllAssetCatalogs() => ScriptableSettings.GetOrFind<AssetDatabaseSettings>().IncludedCatalogs;
    }
}