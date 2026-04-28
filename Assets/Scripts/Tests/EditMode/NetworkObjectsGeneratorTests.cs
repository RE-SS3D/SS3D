using System;
using System.IO;
using System.Linq;
using FishNet.Object;
using JetBrains.Annotations;
using NUnit.Framework;
using SS3D.Data;
using SS3D.Data.Networking;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    /// <summary>
    /// Edit-mode regression tests for the generated <see cref="NetworkObjects"/> asset.
    /// These tests invoke the generator directly so they do not depend on Unity asset postprocessor timing.
    /// </summary>
    public sealed class NetworkObjectsGeneratorTests
    {
        private const string TempFolder = "Assets/Temp";
        private const string TempRootFolder = "Assets/Temp/NetworkObjectsGeneratorTests";
        private const string PrefabsFolder = TempRootFolder + "/Prefabs";
        private const string GeneratedFolder = TempRootFolder + "/Generated";
        private const string GeneratedAssetPath = GeneratedFolder + "/NetworkObjects.asset";

        /// <summary>
        /// Tracks whether this test created <see cref="TempFolder"/> so teardown does not remove a shared temp root.
        /// </summary>
        private bool _createdTempFolder;

        /// <summary>
        /// Starts each test from a clean temporary asset tree.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            _createdTempFolder = !AssetDatabase.IsValidFolder(TempFolder);
            DeleteTempRoot();
            EnsureFolder(PrefabsFolder);
            EnsureFolder(GeneratedFolder);
        }

        /// <summary>
        /// Removes the temporary assets created by the test run.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            DeleteTempRoot();
        }

        /// <summary>
        /// Verifies that incremental generation removes a prefab entry when the prefab still exists but no longer exposes a <see cref="NetworkObject"/>.
        /// </summary>
        [Test]
        public void ShouldRemovePrefabWhenImportedPrefabLosesNetworkObject()
        {
            string prefabPath = CreateNetworkPrefab("TrackedPrefab");
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);

            RunGenerator(new[] { prefabPath }, Array.Empty<string>());

            CollectionAssert.AreEqual(new[] { guid }, GetGeneratedGuids());

            RemoveNetworkObject(prefabPath);
            RunGenerator(new[] { prefabPath }, Array.Empty<string>());

            Assert.That(GetGeneratedGuids(), Is.Empty);
        }

        /// <summary>
        /// Verifies that generated prefab ordering is driven by GUID sorting rather than import order.
        /// </summary>
        [Test]
        public void ShouldGenerateStableGuidOrderRegardlessOfImportOrder()
        {
            string firstPrefabPath = CreateNetworkPrefab("FirstPrefab");
            string secondPrefabPath = CreateNetworkPrefab("SecondPrefab");
            string thirdPrefabPath = CreateNetworkPrefab("ThirdPrefab");

            RunGenerator(new[] { thirdPrefabPath, firstPrefabPath, secondPrefabPath }, Array.Empty<string>());
            string[] firstGeneration = GetGeneratedGuids();

            // Recreate the generated asset from scratch so the second pass cannot inherit ordering from serialized state.
            AssetDatabase.DeleteAsset(GeneratedAssetPath);

            RunGenerator(new[] { secondPrefabPath, thirdPrefabPath, firstPrefabPath }, Array.Empty<string>());
            string[] secondGeneration = GetGeneratedGuids();

            CollectionAssert.AreEqual(firstGeneration, secondGeneration);
            CollectionAssert.AreEqual(firstGeneration.OrderBy(guid => guid).ToArray(), firstGeneration);
        }

        /// <summary>
        /// Verifies that the generator stamps network discovery components with the correct GUID onto each prefab.
        /// </summary>
        [Test]
        public void ShouldStampNetworkPrefabIdentifierAndNetworkTracker()
        {
            string prefabPath = CreateNetworkPrefab("TrackedPrefab");
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);

            RunGenerator(new[] { prefabPath }, Array.Empty<string>());

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            AssetIdentifier identifier = prefab.GetComponent<AssetIdentifier>();
            NetworkObjectTracker tracker = prefab.GetComponent<NetworkObjectTracker>();

            Assert.That(identifier, Is.Not.Null, "Generator should add AssetIdentifier");
            Assert.That(identifier.AssetGuid, Is.EqualTo(guid));
            Assert.That(tracker, Is.Not.Null, "Generator should add NetworkObjectTracker");
            Assert.That(tracker.AssetGuid, Is.EqualTo(guid));
        }

        /// <summary>
        /// Verifies that running the generator multiple times does not duplicate network discovery components.
        /// </summary>
        [Test]
        public void ShouldNotDuplicateTrackerOnRegeneration()
        {
            string prefabPath = CreateNetworkPrefab("TrackedPrefab");

            RunGenerator(new[] { prefabPath }, Array.Empty<string>());
            RunGenerator(new[] { prefabPath }, Array.Empty<string>());

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            AssetIdentifier[] identifiers = prefab.GetComponents<AssetIdentifier>();
            NetworkObjectTracker[] trackers = prefab.GetComponents<NetworkObjectTracker>();

            Assert.That(identifiers.Length, Is.EqualTo(1), "Generator should not duplicate AssetIdentifier");
            Assert.That(trackers.Length, Is.EqualTo(1), "Generator should not duplicate NetworkObjectTracker");
        }

        [Test]
        public void ShouldStampPlainAssetPrefabWithIdentifierAndLifetimeTracker()
        {
            string prefabPath = CreatePlainPrefab("PlainAssetPrefab");
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            AssetPrefabStamper.StampAssetPrefab(prefab, guid);

            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            AssetIdentifier identifier = prefab.GetComponent<AssetIdentifier>();
            InstanceLifetimeTracker tracker = prefab.GetComponent<InstanceLifetimeTracker>();

            Assert.That(identifier, Is.Not.Null);
            Assert.That(identifier.AssetGuid, Is.EqualTo(guid));
            Assert.That(tracker, Is.Not.Null);
            Assert.That(tracker.Identifier, Is.EqualTo(identifier));
            Assert.That(tracker.IsArmed, Is.False);
        }

        /// <summary>
        /// Creates a temporary prefab asset with a <see cref="NetworkObject"/> component.
        /// </summary>
        /// <param name="prefabName">Name of the prefab asset to create.</param>
        /// <returns>The asset path of the generated prefab.</returns>
        [NotNull]
        private static string CreateNetworkPrefab(string prefabName)
        {
            GameObject root = new(prefabName);
            root.AddComponent<NetworkObject>();

            return SavePrefab(root, prefabName);
        }

        [NotNull]
        private static string CreatePlainPrefab(string prefabName)
        {
            GameObject root = new(prefabName);

            return SavePrefab(root, prefabName);
        }

        [NotNull]
        private static string SavePrefab(GameObject root, string prefabName)
        {
            string prefabPath = $"{PrefabsFolder}/{prefabName}.prefab";

            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceSynchronousImport);

            return prefabPath;
        }

        /// <summary>
        /// Ensures the requested asset folder hierarchy exists.
        /// </summary>
        /// <param name="folderPath">Project-relative folder path to create.</param>
        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string currentPath = parts[0];

            for (int index = 1; index < parts.Length; index++)
            {
                string nextPath = $"{currentPath}/{parts[index]}";

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[index]);
                }

                currentPath = nextPath;
            }
        }

        /// <summary>
        /// Loads the generated database and returns its serialized GUID order.
        /// </summary>
        /// <returns>A snapshot of the generated prefab GUIDs.</returns>
        [NotNull]
        private static string[] GetGeneratedGuids()
        {
            NetworkObjects networkObjects = AssetDatabase.LoadAssetAtPath<NetworkObjects>(GeneratedAssetPath);

            Assert.That(networkObjects, Is.Not.Null);

            return networkObjects.GetObjectGuidsSnapshot();
        }

        /// <summary>
        /// Removes the <see cref="NetworkObject"/> component from an existing prefab asset.
        /// </summary>
        /// <param name="prefabPath">Project-relative path to the prefab asset.</param>
        private static void RemoveNetworkObject(string prefabPath)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                NetworkObject networkObject = prefabRoot.GetComponent<NetworkObject>();

                Assert.That(networkObject, Is.Not.Null);

                Object.DestroyImmediate(networkObject, true);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceSynchronousImport);
        }

        /// <summary>
        /// Runs the generator against the temporary test database path.
        /// </summary>
        /// <param name="importedAssets">Assets treated as imported for this refresh.</param>
        /// <param name="deletedAssets">Assets treated as deleted for this refresh.</param>
        private static void RunGenerator(string[] importedAssets, string[] deletedAssets)
        {
            NetworkObjectsGenerator.RefreshForTests(importedAssets, deletedAssets, GeneratedAssetPath);
        }

        /// <summary>
        /// Deletes the temporary test asset tree and removes <see cref="TempFolder"/> only when this test created it and it is still empty.
        /// </summary>
        private void DeleteTempRoot()
        {
            if (AssetDatabase.IsValidFolder(TempRootFolder))
            {
                AssetDatabase.DeleteAsset(TempRootFolder);
                AssetDatabase.Refresh();
            }

            if (!_createdTempFolder || !AssetDatabase.IsValidFolder(TempFolder))
            {
                return;
            }

            string fullTempPath = Path.Combine(Application.dataPath, "Temp");

            // Do not remove Assets/Temp if some other test or editor process wrote files into it.
            if (!Directory.Exists(fullTempPath) || Directory.EnumerateFileSystemEntries(fullTempPath).Any())
            {
                return;
            }

            AssetDatabase.DeleteAsset(TempFolder);
            AssetDatabase.Refresh();
        }
    }
}