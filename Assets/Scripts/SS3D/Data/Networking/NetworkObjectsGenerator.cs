#if UNITY_EDITOR
using Coimbra;
using Coimbra.Editor;
using FishNet.Object;
using JetBrains.Annotations;
#if PARRELSYNC
using ParrelSync;
#endif
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SS3D.Data.Networking
{
    /// <summary>
    /// Editor-only asset postprocessor that keeps the configured <see cref="NetworkObjects"/> asset synchronized with every prefab that contains a <see cref="NetworkObject"/>.
    /// The generated asset provides the ordered prefab list used to derive stable runtime prefab IDs.
    /// </summary>
    internal sealed class NetworkObjectsGenerator : AssetPostprocessor
    {
        /// <summary>
        /// Prevents the save triggered by this generator from recursively scheduling another import pass.
        /// </summary>
        private static bool IgnoreNextImport;

        /// <summary>
        /// Lazily loads the project settings that control whether generation runs, how much it logs, and where the generated asset is stored.
        /// </summary>
        [CanBeNull]
        private static NetworkObjectsGeneratorSettings Settings => ScriptableSettings.GetOrFind<NetworkObjectsGeneratorSettings>(FindOrCreate);

        /// <summary>
        /// Responds to Unity asset import events and refreshes the generated <see cref="NetworkObjects"/> asset when relevant prefabs change.
        /// </summary>
        /// <param name="importedAssets">Asset paths imported during this pass.</param>
        /// <param name="deletedAssets">Asset paths deleted during this pass.</param>
        /// <param name="movedAssets">Asset paths moved during this pass.</param>
        /// <param name="movedFromAssetPaths">Previous paths for moved assets.</param>
        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
#if PARRELSYNC
            if (ClonesManager.IsClone() && Preferences.AssetModPref.Value)
            {
                // Let the original project own generated assets when ParrelSync mirrors asset changes.
                return;
            }
  #endif

            if (!CanRun())
            {
                return;
            }

            if (!Settings!.Enabled)
            {
                if (Settings.LogToConsole)
                {
                    Debug.LogError("NetworkObjectsGenerator is disabled. Enable it in the Project Settings to generate the NetworkObjects asset.");
                }

                return;
            }

            NetworkObjects nobDatabase = GetOrCreateDatabase();

            if (Settings!.FullRebuild ? !GenerateFull(nobDatabase) : !GenerateChanged(nobDatabase, importedAssets))
            {
                return;
            }

            // Always rebuild the serialized arrays from the editor cache before persisting the asset.
            Generate(nobDatabase);

            EditorUtility.SetDirty(nobDatabase);
            SaveChanges(nobDatabase);
        }

        /// <summary>
        /// Test seam for invoking the generator deterministically from edit-mode tests.
        /// This bypasses project settings and import-time guards so tests can drive generation against a temporary asset path.
        /// </summary>
        /// <param name="importedAssets">Asset paths treated as imported for this refresh.</param>
        /// <param name="deletedAssets">Asset paths treated as deleted for this refresh.</param>
        /// <param name="networkObjectsPath">Output path for the generated <see cref="NetworkObjects"/> asset.</param>
        /// <param name="fullRebuild">When <see langword="true"/>, rebuilds from every prefab in the project instead of applying incremental changes.</param>
        internal static void RefreshForTests([NotNull] string[] importedAssets, [NotNull] string[] deletedAssets, [NotNull] string networkObjectsPath, bool fullRebuild = false)
        {
            IgnoreNextImport = false;

            NetworkObjects nobDatabase = GetOrCreateDatabase(networkObjectsPath);

            if (!(fullRebuild ? GenerateFull(nobDatabase) : GenerateChanged(nobDatabase, importedAssets)))
            {
                return;
            }

            Generate(nobDatabase);
            EditorUtility.SetDirty(nobDatabase);
            AssetDatabase.SaveAssetIfDirty(nobDatabase);
        }

        /// <summary>
        /// Rebuilds the editor-side prefab cache from scratch by scanning every prefab in the project for <see cref="NetworkObject"/> components.
        /// </summary>
        /// <param name="nobDatabase">The database instance to repopulate.</param>
        /// <returns><see langword="true"/> after the database has been rebuilt.</returns>
        private static bool GenerateFull([NotNull] NetworkObjects nobDatabase)
        {
            nobDatabase.Clear();

            NetworkObject[] prefabs = AssetDatabase.FindAssets("t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<NetworkObject>)
                .Where(nob => nob)
                .ToArray();

            AddObjects(nobDatabase, prefabs);

            return true;
        }

        /// <summary>
        /// Applies incremental updates by removing deleted or invalidated prefabs and adding newly imported prefabs that expose a <see cref="NetworkObject"/>.
        /// </summary>
        /// <param name="nobDatabase">The database instance to update.</param>
        /// <param name="importedAssets">Asset paths imported during this pass.</param>
        /// <returns><see langword="true"/> when the tracked prefab set changed.</returns>
        private static bool GenerateChanged([NotNull] NetworkObjects nobDatabase, [NotNull] string[] importedAssets)
        {
            int initialCount = nobDatabase.GetObjectCount();

            // Imported prefabs can lose their NetworkObject component without being deleted, which
            // turns the cached component reference into a stale entry that still needs to be purged.
            nobDatabase.RemoveNull();
            bool modified = nobDatabase.GetObjectCount() != initialCount;

            if (importedAssets.Length <= 0)
            {
                return modified;
            }

            foreach (string importedAsset in importedAssets)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(importedAsset);

                if (!prefab || prefab.TryGetComponent(out NetworkObject _))
                {
                    continue;
                }

                string guid = AssetDatabase.AssetPathToGUID(importedAsset);

                // Imported prefabs that still exist but no longer expose a NetworkObject have to be
                // removed explicitly because they will not show up as deleted assets.
                modified = nobDatabase.RemoveObject(guid) || modified;
            }

            NetworkObject[] possibleNewPrefabs = importedAssets.Select(AssetDatabase.LoadAssetAtPath<GameObject>).
                Select(go => go?.GetComponent<NetworkObject>()).
                Where(nob => nob).
                ToArray();

            initialCount = nobDatabase.GetObjectCount();
            AddObjects(nobDatabase, possibleNewPrefabs);
            modified = modified || nobDatabase.GetObjectCount() != initialCount;

            return modified;
        }

        /// <summary>
        /// Adds the discovered prefab references to the database, ignoring empty slots produced by asset lookups.
        /// The database itself handles duplicate filtering and GUID extraction.
        /// </summary>
        /// <param name="nobDatabase">The database instance to update.</param>
        /// <param name="prefabs">Candidate prefab references collected from the asset database.</param>
        private static void AddObjects([NotNull] NetworkObjects nobDatabase, [NotNull] NetworkObject[] prefabs)
        {
            if (prefabs.Any(nob => nob))
            {
                nobDatabase.AddObjects(prefabs);
            }
        }

        /// <summary>
        /// Rebuilds the serialized GUID and prefab arrays from the current editor-side cache.
        /// This is the step that translates editor discovery state into the deterministic runtime collection.
        /// </summary>
        /// <param name="nobDatabase">The database instance to serialize.</param>
        private static void Generate([NotNull] NetworkObjects nobDatabase)
        {
            nobDatabase.Generate();

            if (Settings!.LogToConsole)
            {
                Debug.Log($"Generated NetworkObjects asset with {nobDatabase.GetObjectCount()} prefabs.");
            }
        }

        /// <summary>
        /// Saves the generated asset when configured and suppresses the import event raised by that save.
        /// </summary>
        /// <param name="nobDatabase">The database instance to persist.</param>
        private static void SaveChanges([NotNull] NetworkObjects nobDatabase)
        {
            if (!Settings!.SaveChanges)
            {
                return;
            }

            // Saving the asset triggers another import pass; suppress that single self-induced callback.
            IgnoreNextImport = true;
            AssetDatabase.SaveAssetIfDirty(nobDatabase);
        }

        /// <summary>
        /// Determines whether the current import pass is safe to process.
        /// Generation is skipped during play mode, compilation, and the import pass caused by our own save.
        /// </summary>
        /// <returns><see langword="true"/> when the generator should handle this import event.</returns>
        private static bool CanRun()
        {
            if (UnityEngine.Application.isPlaying)
            {
                if (Settings!.LogToConsole)
                {
                    Debug.LogError("NetworkObjectsGenerator cannot run while the game is playing.");
                }

                return false;
            }

            if (!IgnoreNextImport)
            {
                return !EditorApplication.isCompiling;
            }

            IgnoreNextImport = false;

            // This pass was triggered by SaveChanges.
            return false;
        }

        /// <summary>
        /// Loads the configured <see cref="NetworkObjects"/> asset, creating it and its parent folders when it does not exist yet.
        /// </summary>
        /// <returns>The existing or newly created database asset.</returns>
        private static NetworkObjects GetOrCreateDatabase()
        {
            return GetOrCreateDatabase(Settings?.NetworkObjectsPath);
        }

        /// <summary>
        /// Loads the configured <see cref="NetworkObjects"/> asset for a specific path, creating it and its parent folders when it does not exist yet.
        /// </summary>
        /// <param name="assetPath">Asset path where the generated database should live.</param>
        /// <exception cref="InvalidOperationException">If the asset path is null.</exception>
        /// <returns>The existing or newly created database asset.</returns>
        private static NetworkObjects GetOrCreateDatabase([CanBeNull] string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new InvalidOperationException("NetworkObjects asset path cannot be null or empty.");
            }

            NetworkObjects nobDatabase = AssetDatabase.LoadAssetAtPath<NetworkObjects>(assetPath);

            if (nobDatabase)
            {
                return nobDatabase;
            }

            string directory = Path.GetDirectoryName(assetPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                // The generator may be pointed at a temporary test location that does not exist yet.
                Directory.CreateDirectory(directory);
            }

            nobDatabase = ScriptableObject.CreateInstance<NetworkObjects>();
            AssetDatabase.CreateAsset(nobDatabase, assetPath);

            return nobDatabase;
        }

        /// <summary>
        /// Finds the requested <see cref="ScriptableSettings"/> asset or creates one when the project has not stored it yet.
        /// </summary>
        /// <param name="type">The concrete settings type to resolve.</param>
        /// <returns>The existing or newly created settings asset.</returns>
        [CanBeNull]
        private static ScriptableSettings FindOrCreate(Type type)
        {
            ScriptableSettings settings = ScriptableSettings.FindSingle(type);

            if (!settings)
            {
                settings = ScriptableSettingsUtility.LoadOrCreate(type);
            }

            return settings;
        }
    }
}
#endif