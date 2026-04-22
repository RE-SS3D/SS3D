using System.Collections.Generic;
using Coimbra;
using JetBrains.Annotations;
using NUnit.Framework;
using SS3D.Data.AssetDatabases;
using System.Linq;
using UnityAssetDatabase = UnityEditor.AssetDatabase;
using UnityEditor;
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
        public void IncludedAssetDatabasesAreNotNull(AddressablesDatabase database)
        {
            Assert.IsTrue(database != null);
        }

        /// <summary>
        /// Test to check if there are databases loaded.
        /// </summary>
        [Test]
        public void IncludedAssetDatabasesAreNotEmpty()
        {
            List<AddressablesDatabase> databases = _assetDatabaseSettings.IncludedAssetDatabases;

            bool databasesAreEmpty = databases.Count == 0;
            Assert.IsFalse(databasesAreEmpty);
        }

        /// <summary>
        /// Test to see if all the project's databases are included.
        /// </summary>
        [Test]
        public void AllProjectAssetDatabasesAreOnIncludedDatabases()
        {
            List<AddressablesDatabase> projectAssetDatabases = AddressablesDatabase.FindAllAssetDatabases();
            List<AddressablesDatabase> loadedAssetDatabases = _assetDatabaseSettings.IncludedAssetDatabases;

            bool hasMissingDatabases = false;
            List<AddressablesDatabase> missingDatabases = new();

            foreach (AddressablesDatabase projectAssetDatabase in projectAssetDatabases)
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
                foreach (AddressablesDatabase missingDatabase in missingDatabases)
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
        public void IncludedAssetDatabasesDoNotContainNullObjects([NotNull] AddressablesDatabase addressablesDatabase)
        {
            bool hasNullAssets = false;
            List<string> nullGuids = new();

            foreach (string guid in
                from guid in addressablesDatabase.AssetGuids
                let path = UnityAssetDatabase.GUIDToAssetPath(guid)
                let asset = UnityAssetDatabase.LoadAssetAtPath<Object>(path)
                where !asset
                select guid)
            {
                hasNullAssets = true;
                nullGuids.Add(guid);
            }

            if (!hasNullAssets)
            {
                Assert.Pass();

                return;
            }

            foreach (string nullGuid in nullGuids)
            {
                Debug.LogError($"Asset is null on {addressablesDatabase.name} : {nullGuid}");
            }

            Assert.Fail($"{addressablesDatabase.name} has null assets");
        }

        private static List<AddressablesDatabase> AllAssetDatabases() => ScriptableSettings.GetOrFind<AssetDatabaseSettings>().IncludedAssetDatabases;
    }
}