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
    /// AssetPostprocessor that generates a NetworkObjects asset containing references to all NetworkObject prefabs in the project.
    /// This is used to load the prefabs at runtime and get them by their ID, which is determined by their index in the list of prefabs.
    /// </summary>
    internal sealed class NetworkObjectsGenerator : AssetPostprocessor
    {
        /// <summary>
        /// A flag to ignore the next import event, used to prevent infinite loops when saving the generated NetworkObjects asset.
        /// </summary>
        private static bool IgnoreNextImport;
        
        /// <summary>
        /// Settings for the NetworkObjectsGenerator, which can be accessed and modified in the Project Settings.
        /// This is used to enable or disable the generator, log its actions to the console, and specify the path to the generated NetworkObjects asset.
        /// </summary>
        [CanBeNull]
        private static NetworkObjectsGeneratorSettings Settings => ScriptableSettings.GetOrFind<NetworkObjectsGeneratorSettings>(FindOrCreate);


        public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
#if PARRELSYNC
            if (ClonesManager.IsClone() && Preferences.AssetModPref.Value)
            {
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
            
            if (Settings!.FullRebuild ?
                GenerateFull(nobDatabase) :
                GenerateChanged(nobDatabase, importedAssets, deletedAssets))
            {
                SaveChanges(nobDatabase);
            }
        }

        /// <summary>
        /// Generates the NetworkObjects asset by finding all prefabs in the project that have a NetworkObject component.
        /// </summary>
        /// <param name="nobDatabase">The NetworkObjects object to modify</param>
        /// <returns>True if NetworkObjects database is changed</returns>
        private static bool GenerateFull([NotNull] NetworkObjects nobDatabase)
        {
            nobDatabase.Clear();

            NetworkObject[] prefabs = AssetDatabase.FindAssets("t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Select(loadedGameObject => loadedGameObject.GetComponent<NetworkObject>())
                .Where(nob => nob)
                .ToArray();

            Generate(nobDatabase, prefabs);

            return true;
        }

        /// <summary>
        /// Generates the NetworkObjects asset by checking the imported and deleted assets for prefabs with a NetworkObject component, and adding or removing them from the database accordingly.
        /// </summary>
        /// <param name="nobDatabase">The NetworkObjects object to modify</param>
        /// <param name="importedAssets">Collection imported assets</param>
        /// <param name="deletedAssets">Collection of deleted assets</param>
        /// <returns>True if NetworkObjects database is changed</returns>
        private static bool GenerateChanged([NotNull] NetworkObjects nobDatabase, [NotNull] string[] importedAssets, [NotNull] string[] deletedAssets)
        {
            bool modified = false;
            int initialCount = nobDatabase.GetObjectCount();

            if (deletedAssets.Length > 0)
            {
                nobDatabase.RemoveNull();
                modified = nobDatabase.GetObjectCount() != initialCount;
            }

            if (importedAssets.Length <= 0)
            {
                return modified;
            }


            NetworkObject[] possibleNewPrefabs = importedAssets.Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Select(go => go?.GetComponent<NetworkObject>())
                .Where(nob => nob)
                .ToArray();
            initialCount = nobDatabase.GetObjectCount();
            Generate(nobDatabase, possibleNewPrefabs);
            modified = modified || nobDatabase.GetObjectCount() != initialCount;

            return modified;
        }

        /// <summary>
        /// Generates the NetworkObjects asset by adding the given prefabs to the database and saving the changes.
        /// </summary>
        /// <param name="nobDatabase"></param>
        /// <param name="prefabs"></param>
        private static void Generate([NotNull] NetworkObjects nobDatabase, [NotNull] NetworkObject[] prefabs)
        {
            if (!prefabs.Any(nob => nob))
            {
                return;
            }
            
            nobDatabase.AddObjects(prefabs);
            nobDatabase.Generate();

            if (Settings!.LogToConsole)
            {
                Debug.Log($"Generated NetworkObjects asset with {nobDatabase.GetObjectCount()} prefabs.");
            }
        }

        private static void SaveChanges([NotNull] NetworkObjects nobDatabase)
        {
            EditorUtility.SetDirty(nobDatabase);

            if (!Settings!.SaveChanges)
            {
                return;
            }

            IgnoreNextImport = true;
            AssetDatabase.SaveAssetIfDirty(nobDatabase);
        }

        /// <summary>
        /// Checks if the generator can run.
        /// </summary>
        /// <returns>can the generator run.</returns>
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

            return false;
        }

        /// <summary>
        /// Gets the existing NetworkObjects asset at the specified path, or creates a new one if it doesn't exist.
        /// This is used to ensure that we have a NetworkObjects asset to work with when generating the list of prefabs.
        /// </summary>
        /// <returns>Stored or created NetworkObjects asset</returns>
        private static NetworkObjects GetOrCreateDatabase()
        {
            NetworkObjects nobDatabase = AssetDatabase.LoadAssetAtPath<NetworkObjects>(Settings?.NetworkObjectsPath);

            if (nobDatabase)
            {
                return nobDatabase;
            }

            string directory = Path.GetDirectoryName(Settings!.NetworkObjectsPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            nobDatabase = ScriptableObject.CreateInstance<NetworkObjects>();
            AssetDatabase.CreateAsset(nobDatabase, Settings.NetworkObjectsPath);

            return nobDatabase;
        }

        /// <summary>
        /// Finds the NetworkObjectsGeneratorSettings asset in the project, or creates a new one if it doesn't exist.
        /// </summary>
        /// <param name="type">type of ScriptableSettings</param>
        /// <returns>ScriptableSettings of given type.</returns>
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