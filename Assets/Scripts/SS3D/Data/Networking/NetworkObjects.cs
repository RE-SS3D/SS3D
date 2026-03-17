using FishNet.Managing.Object;
using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Data.Networking
{
    /// <summary>
    /// FishNet prefab collection backed by a deterministic GUID order.
    /// Editor generation produces the ordered GUID list, non-addressable prefabs are serialized directly,
    /// and addressable prefabs fill their runtime slots when <see cref="AssetSubSystem"/> changes shared asset residency.
    /// </summary>
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SS3D Data", order = 0)]
    public sealed partial class NetworkObjects : PrefabObjects
    {
        /// <summary>
        /// Serialized GUID order used to derive stable FishNet prefab IDs across editor generation and runtime loading.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private List<string> _objectGuids = new();

        /// <summary>
        /// Runtime prefab slots aligned with <see cref="_objectGuids"/>.
        /// Non-addressable prefabs are populated during generation; addressable prefabs are inserted when their asset is loaded.
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
        public override int GetObjectCount()
        {
#if UNITY_EDITOR
            return _prefabs.Count;
#else
            return _objectGuids.Count;
#endif
        }

        /// <summary>
        /// Gets a prefab based on its ID (index). The ID is determined by the index of the prefab in the list of GUIDs.
        /// </summary>
        /// <param name="asServer">if server is calling the function</param>
        /// <param name="id">ID of the prefab</param>
        /// <returns>NetworkObject for the ID</returns>
        public override NetworkObject GetObject(bool asServer, int id)
        {
            if (!CheckId(id))
            {
                return null;
            }

            if (!_loadedPrefabs[id])
            {
                Log.Error(this, $"Prefab on id {id} is not loaded.");
            }

            return _loadedPrefabs[id];
        }

        /// <summary>
        /// Checks whether a runtime prefab slot currently contains a loaded network prefab.
        /// </summary>
        public bool IsLoaded(int id) => !CheckId(id) ? false : _loadedPrefabs[id];

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

        private void OnEnable()
        {
#if UNITY_EDITOR
            Initialize();
#endif

            // Addressable prefabs are inserted and removed from runtime slots as the asset subsystem changes shared residency.
            AssetSubSystem.OnAssetLoaded += HandleAssetLoaded;
            AssetSubSystem.OnAssetUnloaded += HandleAssetUnloaded;
        }

        private void OnDisable()
        {
            AssetSubSystem.OnAssetLoaded -= HandleAssetLoaded;
            AssetSubSystem.OnAssetUnloaded -= HandleAssetUnloaded;
        }

        /// <summary>
        /// Registers a loaded addressable prefab into its deterministic runtime slot when the asset exposes a <see cref="NetworkObject"/>.
        /// </summary>
        /// <param name="guid">GUID of the asset loaded.</param>
        /// <param name="asset">Asset loaded.</param>
        private void HandleAssetLoaded(string guid, Object asset)
        {
            if (asset is GameObject gameObject && gameObject.TryGetComponent(out NetworkObject networkObject))
            {
                AddObject(networkObject, guid);
            }
        }

        /// <summary>
        /// Clears the runtime prefab slot when an addressable prefab is unloaded.
        /// </summary>
        /// <param name="guid">GUID of the unloaded asset.</param>
        private void HandleAssetUnloaded(string guid)
        {
            int index = _objectGuids.IndexOf(guid);

            if (index >= 0 && index < _loadedPrefabs.Length)
            {
                _loadedPrefabs[index] = null;
            }
        }

        /// <summary>
        /// Inserts a runtime-loaded addressable prefab into the slot derived from its GUID.
        /// </summary>
        /// <param name="networkObject">Loaded prefab root that exposes a <see cref="NetworkObject"/>.</param>
        /// <param name="guid">GUID used to find the prefab's deterministic runtime slot.</param>
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
        /// Initializes a loaded prefab slot with FishNet so the prefab ID matches the generated collection index.
        /// </summary>
        /// <param name="id">ID of the prefab</param>
        private void InitializePrefab(int id)
        {
            if (!CheckId(id))
            {
                return;
            }

            if (_loadedPrefabs[id])
            {
                ManagedObjects.InitializePrefab(_loadedPrefabs[id], id, CollectionId);
            }
        }

        /// <summary>
        /// Validates that the requested prefab ID maps to a slot inside the generated collection.
        /// </summary>
        private bool CheckId(int id)
        {
            if (id < _loadedPrefabs.Length && id >= 0)
            {
                return true;
            }

            Log.Error(this, $"PrefabId {id} is out of range.");

            return false;
        }
    }
}