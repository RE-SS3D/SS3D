#if UNITY_EDITOR
using FishNet.Object;
using FishNet.Observing;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using static FishNet.Observing.NetworkObserver;

namespace SS3D.Data.Networking
{
    public partial class NetworkObjects
    {
        /// <summary>
        /// Editor-only prefab cache keyed by GUID.
        /// The sorted order is what ultimately produces stable runtime prefab IDs in the generated arrays.
        /// </summary>
        private SortedDictionary<string, NetworkObject> _prefabs = new();

        /// <summary>
        /// Prevents repeated editor-side reconstruction of <see cref="_prefabs"/> while the asset stays loaded.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Rebuilds the serialized GUID list and prefab array from the sorted editor cache.
        /// Addressable entries keep their GUID slot but leave the runtime prefab slot empty for play mode loading.
        /// Also stamps a <see cref="NetworkObjectTracker"/> onto each prefab so dependency prefabs
        /// can be discovered at runtime, fixing any GUID mismatches from prefab moves or copies.
        /// </summary>
        internal void Generate()
        {
            // Preserve sorted GUID order so prefab IDs remain deterministic across machines.
            List<string> sortedGuids = _prefabs.Keys.ToList();

            _guidToIndex = new();

            for (int i = 0; i < sortedGuids.Count; i++)
            {
                _guidToIndex[sortedGuids[i]] = i;
            }

            // Non-addressable prefabs can be serialized directly. Addressable prefabs are resolved at runtime by GUID.
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            _loadedPrefabs = new NetworkObject[_prefabs.Count];

            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < sortedGuids.Count; i++)
                {
                    string guid = sortedGuids[i];
                    NetworkObject prefab = _prefabs[guid];

                    TrySetOverride(prefab, !prefab.TryGetComponent(out NetworkBarrier _) ? ConditionOverrideType.UseManager : ConditionOverrideType.IgnoreManager);
                    EnsureTracker(prefab, guid);

                    if (settings.FindAssetEntry(guid) == null)
                    {
                        _loadedPrefabs[i] = prefab;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        /// <summary>
        /// Returns a copy of the generated GUID order currently serialized on this asset.
        /// Tests use this to validate deterministic generation without reaching into serialized fields directly.
        /// </summary>
        [NotNull]
        internal string[] GetObjectGuidsSnapshot()
        {
            // Dictionary key enumeration order is not guaranteed, so keys are placed
            // at their index position to match the original sorted generation order.
            string[] guids = new string[_guidToIndex.Count];

            foreach (KeyValuePair<string, int> pair in _guidToIndex)
            {
                guids[pair.Value] = pair.Key;
            }

            return guids;
        }

        /// <summary>
        /// Ensures an addressable prefab's <see cref="NetworkObserver"/> uses <see cref="ConditionOverrideType.UseManager"/>
        /// so the <see cref="PreloadCondition"/> from the <c>ObserverManager</c> is applied at spawn time.
        /// </summary>
        private static void TrySetOverride([NotNull] NetworkObject networkObject, ConditionOverrideType overrideType)
        {
            if (!networkObject.TryGetComponent(out NetworkObserver observer) || observer.OverrideType == overrideType)
            {
                return;
            }

            SerializedObject serializedObserver = new(observer);
            serializedObserver.FindProperty("_overrideType").intValue = (int)overrideType;
            serializedObserver.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Ensures the prefab has a <see cref="NetworkObjectTracker"/> with the correct GUID.
        /// Adds the component if missing, fixes mismatches, and saves the prefab if modified.
        /// </summary>
        private static void EnsureTracker([NotNull] NetworkObject prefab, [NotNull] string guid)
        {
            bool modified = false;

            if (!prefab.TryGetComponent(out NetworkObjectTracker tracker))
            {
                tracker = prefab.gameObject.AddComponent<NetworkObjectTracker>();
                modified = true;
            }

            if (tracker.Guid != guid)
            {
                tracker.SetGuid(guid);
                modified = true;
            }

            if (modified)
            {
                PrefabUtility.SavePrefabAsset(prefab.gameObject);
            }
        }

        /// <summary>
        /// Reconstructs the sorted editor cache from the serialized GUID list, removing stale or invalid entries on the way.
        /// </summary>
        private void EditorInitialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _prefabs = new();
            List<string> staleGuids = new();

            foreach (string guid in _guidToIndex.Keys)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                // Strip stale GUIDs and prefabs that no longer expose a NetworkObject so the generated collection stays valid.
                if (!prefab || !prefab.TryGetComponent(out NetworkObject networkObject) || !_prefabs.TryAdd(guid, networkObject))
                {
                    staleGuids.Add(guid);
                }
            }

            foreach (string guid in staleGuids)
            {
                _guidToIndex.Remove(guid);
            }

            // Recompact indices to be contiguous after stale removal.
            int newIndex = 0;

            foreach (string guid in _prefabs.Keys)
            {
                _guidToIndex[guid] = newIndex++;
            }

            _isInitialized = true;
        }
    }
}
#endif