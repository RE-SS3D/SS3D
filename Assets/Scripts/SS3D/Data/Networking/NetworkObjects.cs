using FishNet.Managing.Object;
using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Data.Networking
{
    /// <summary>
    /// A ScriptableObject that holds references to NetworkObjects that can be spawned in the game, and their corresponding GUIDs. This is used to load the prefabs at runtime and get them by their ID, which is determined by their index in the list of prefabs.
    /// </summary>
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SS3D Data", order = 0)]
    public sealed class NetworkObjects : PrefabObjects
    {
#if UNITY_EDITOR
        /// <summary>
        /// A sorted dictionary that holds the GUIDs of the prefabs as keys and the NetworkObject references as values.
        /// </summary>
        private SortedDictionary<string, NetworkObject> _prefabs = new();
#endif

        /// <summary>
        /// A list of the GUIDs of the prefabs, used to load the prefabs at runtime. 
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private List<string> _objectGuids = new();

        /// <summary>
        /// An array of the loaded prefabs, used to get the prefabs at runtime. The index of the prefab in the array is determined by its index in the list of GUIDs.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private NetworkObject[] _loadedPrefabs = Array.Empty<NetworkObject>();

        /// <summary>
        /// Clears the collection of prefabs and their GUIDs.
        /// </summary>
        public override void Clear()
        {
#if UNITY_EDITOR
            _prefabs.Clear();
#endif
            _objectGuids.Clear();
            Array.Clear(_loadedPrefabs, 0, _loadedPrefabs.Length);
        }

        /// <summary>
        /// Gets the count of prefabs in the collection.
        /// </summary>
        /// <returns>Number of objects in collection</returns>
        public override int GetObjectCount() => _objectGuids.Count;

        /// <summary>
        /// Gets a prefab based on its ID (index). The ID is determined by the index of the prefab in the list of GUIDs.
        /// </summary>
        /// <param name="asServer">if server is calling the function</param>
        /// <param name="id">ID of the prefab</param>
        /// <returns>NetworkObject for the ID</returns>
        public override NetworkObject GetObject(bool asServer, int id)
        {
            if (id < 0 || id >= _loadedPrefabs.Length)
            {
                Log.Error(this, $"PrefabId {id} is out of range.");

                return null;
            }

            if (!_loadedPrefabs[id])
            {
                Log.Error(this, $"Prefab on id {id} is not loaded.");
            }

            return _loadedPrefabs[id];
        }

        /// <summary>
        /// Removes null references from the collection of prefabs.
        /// </summary>
        public override void RemoveNull()
        {
#if UNITY_EDITOR
            List<string> keysToRemove = new();

            foreach ((string guid, NetworkObject networkObject) in _prefabs)
            {
                if (!networkObject)
                {
                    keysToRemove.Add(guid);
                }
            }

            foreach (string guid in keysToRemove)
            {
                _prefabs.Remove(guid);
            }
#endif
        }

        /// <summary>
        /// Attempts to add a prefab to the collection. (Editor Only)
        /// </summary>
        /// <param name="networkObject">prefab to add</param>
        /// <param name="checkForDuplicates">DOESN'T MATTER</param>
        public override void AddObject(NetworkObject networkObject, bool checkForDuplicates = false)
        {
            if (UnityEngine.Application.isPlaying)
            {
                Log.Error(this, new InvalidOperationException("Adding objects is not supported in DatabaseObjects at runtime."), "Use AddObject(NetworkObject, string) instead.");

                return;
            }

#if UNITY_EDITOR
            if (_prefabs.ContainsValue(networkObject))
            {
                return;
            }

            string path = UnityEditor.AssetDatabase.GetAssetPath(networkObject.gameObject);
            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            _prefabs.TryAdd(guid, networkObject);
#endif
        }

        /// <summary>
        /// Attempts to add a list of prefabs to the collection. (Editor Only)
        /// </summary>
        /// <param name="networkObjects">list if prefabs to add</param>
        /// <param name="checkForDuplicates">DOESN'T MATTER</param>
        public override void AddObjects([NotNull] List<NetworkObject> networkObjects, bool checkForDuplicates = false)
        {
            AddObjects(networkObjects);
        }

        /// <summary>
        /// Attempts to add an array of prefabs to the collection. (Editor Only)
        /// </summary>
        /// <param name="networkObjects">array of prefabs to add</param>
        /// <param name="checkForDuplicates">DOESN'T MATTER</param>
        public override void AddObjects([NotNull] NetworkObject[] networkObjects, bool checkForDuplicates = false)
        {
            AddObjects(networkObjects);
        }

        /// <summary>
        /// Attempts to add a IEnumerable of prefabs to the collection. (Editor Only)
        /// </summary>
        /// <param name="networkObjects">IEnumerable of prefabs to add</param>
        public void AddObjects([NotNull] IEnumerable<NetworkObject> networkObjects)
        {
            if (UnityEngine.Application.isPlaying)
            {
                Log.Error(this, new InvalidOperationException("Adding objects is not supported while in play mode."), "Use AddObject(NetworkObject, int) instead");

                return;
            }

#if UNITY_EDITOR
            foreach (NetworkObject nob in networkObjects)
            {
                AddObject(nob);
            }
#endif
        }

        public override void AddObject(DualPrefab dualPrefab, bool checkForDuplicates = false)
        {
            Log.Error(this, "Adding DualPrefabs is not supported in NetworkObjects.");
        }

        public override void AddObjects(List<DualPrefab> dualPrefab, bool checkForDuplicates = false)
        {
            Log.Error(this, "Adding DualPrefabs is not supported in NetworkObjects.");
        }

        public override void AddObjects(DualPrefab[] dualPrefab, bool checkForDuplicates = false)
        {
            Log.Error(this, "Adding DualPrefabs is not supported in NetworkObjects.");
        }

        /// <summary>
        /// Initializes loaded prefabs from the given index.
        /// </summary>
        /// <param name="startIndex">the index to start from</param>
        public override void InitializePrefabRange(int startIndex)
        {
            for (int i = startIndex; i < _loadedPrefabs.Length; i++)
            {
                InitializePrefab(i);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Generate the asset added objects. (Editor Only)
        /// </summary>
        internal void Generate()
        {
            // Generate the _objectGuids list based on the keys of the _prefabs dictionary, which are the GUIDs of the prefabs.
            _objectGuids = _prefabs.Keys.ToList();

            // Generate the _loadedPrefabs array based on whether the prefabs are included in the Addressable Asset System or not.
            // If they are included, they will be loaded at runtime using their GUIDs, so we don't need to include them in the _loadedPrefabs array.
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            _loadedPrefabs = new NetworkObject[_prefabs.Count];

            for (int i = 0; i < _prefabs.Count; i++)
            {
                string guid = _objectGuids[i];

                if (settings.FindAssetEntry(guid) != null)
                {
                    continue;
                }

                _loadedPrefabs[i] = _prefabs[guid];
            }
        }
#endif

        private void OnEnable()
        {
            AssetLoader.OnAssetLoaded += OnAssetLoaded;

            Initialize();
        }

        private void OnDisable()
        {
            AssetLoader.OnAssetLoaded -= OnAssetLoaded;
        }

        /// <summary>
        /// Initializes the _prefabs dictionary. (Editor Only)
        /// </summary>
        private void Initialize()
        {
#if UNITY_EDITOR
            _prefabs = new();

            for (int i = 0; i < _objectGuids.Count;)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(_objectGuids[i]);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (!prefab.TryGetComponent(out NetworkObject networkObject) || !_prefabs.TryAdd(_objectGuids[i], networkObject))
                {
                    _objectGuids.RemoveAt(i);

                    continue;
                }

                i++;
            }
#endif
        }

        /// <summary>
        /// Event handler for when asset is loaded by Assets class.
        /// </summary>
        /// <param name="loadedAssetData">GUID and the object loaded</param>
        private void OnAssetLoaded(KeyValuePair<string, Object> loadedAssetData)
        {
            if (loadedAssetData.Value is GameObject gameObject && gameObject.TryGetComponent(out NetworkObject networkObject))
            {
                AddObject(networkObject, loadedAssetData.Key);
            }
        }

        /// <summary>
        /// Attempts to add a prefab to collection. (Runtime only)
        /// </summary>
        /// <param name="networkObject"></param>
        /// <param name="guid"></param>
        private void AddObject(NetworkObject networkObject, string guid)
        {
            if (!UnityEngine.Application.isPlaying)
            {
                Log.Error(this, new InvalidOperationException("Adding objects is not supported while not in play mode"), "Use AddObject(NetworkObject, int) instead.");

                return;
            }

            if (!_objectGuids.Contains(guid))
            {
                Log.Error(this, "GUID not found in collection.");

                return;
            }

            int index = _objectGuids.IndexOf(guid);
            _loadedPrefabs[index] = networkObject;
            InitializePrefab(index);
        }

        /// <summary>
        /// Initializes a prefab in loaded prefabs at a specified index.
        /// </summary>
        /// <param name="id">ID of the prefab</param>
        private void InitializePrefab(int id)
        {
            if (_loadedPrefabs[id])
            {
                ManagedObjects.InitializePrefab(_loadedPrefabs[id], id, CollectionId);
            }
        }
    }
}