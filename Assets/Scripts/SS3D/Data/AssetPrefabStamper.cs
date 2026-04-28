#if UNITY_EDITOR
using Coimbra;
using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Networking;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AssetDatabase = UnityEditor.AssetDatabase;

namespace SS3D.Data
{
    /// <summary>
    /// Editor-only helper that keeps asset prefabs stamped with their required runtime metadata.
    /// </summary>
    internal static class AssetPrefabStamper
    {
        /// <summary>
        /// Applies the editor-time metadata required for a prefab to participate in asset lifetime
        /// tracking, network prefab discovery, or both.
        /// </summary>
        /// <param name="prefab">Prefab asset root to stamp.</param>
        /// <param name="guid">GUID from Unity's asset database for <paramref name="prefab"/>.</param>
        /// <param name="requireInstanceLifetimeTracker">
        /// Whether the prefab should keep an <see cref="InstanceLifetimeTracker"/> for asset lifetime accounting.
        /// </param>
        /// <param name="requireNetworkObjectTracker">
        /// Whether the prefab should keep a <see cref="NetworkObjectTracker"/> when it has a root <see cref="NetworkObject"/>.
        /// </param>
        /// <param name="savePrefab">Whether to persist the prefab asset when stamping changes it.</param>
        /// <returns><see langword="true"/> when the prefab was modified.</returns>
        internal static bool StampAssetPrefab(
            GameObject prefab,
            [CanBeNull] string guid,
            bool requireInstanceLifetimeTracker = true,
            bool requireNetworkObjectTracker = false,
            bool savePrefab = true)
        {
            if (!prefab || string.IsNullOrEmpty(guid))
            {
                return false;
            }

            bool modified = false;
            AssetIdentifier identifier = EnsureAssetIdentifier(prefab, guid, ref modified);

            if (requireInstanceLifetimeTracker)
            {
                EnsureInstanceLifetimeTracker(prefab, identifier, ref modified);
            }

            if (requireNetworkObjectTracker)
            {
                EnsureNetworkObjectTracker(prefab, ref modified);
            }

            if (modified && savePrefab)
            {
                PrefabUtility.SavePrefabAsset(prefab);
            }

            return modified;
        }

        /// <summary>
        /// Ensures the prefab has the serialized source for its asset GUID.
        /// </summary>
        [NotNull]
        private static AssetIdentifier EnsureAssetIdentifier(GameObject prefab, string guid, ref bool modified)
        {
            if (!prefab.TryGetComponent(out AssetIdentifier identifier))
            {
                identifier = prefab.AddComponent<AssetIdentifier>();
                modified = true;
            }

            if (identifier.AssetGuid == guid)
            {
                return identifier;
            }

            identifier.SetGuid(guid);
            EditorUtility.SetDirty(identifier);
            modified = true;

            return identifier;
        }

        /// <summary>
        /// Ensures asset database prefabs are wired for runtime clone/release accounting.
        /// </summary>
        private static void EnsureInstanceLifetimeTracker(GameObject prefab, AssetIdentifier identifier, ref bool modified)
        {
            if (!prefab.TryGetComponent(out InstanceLifetimeTracker tracker))
            {
                tracker = prefab.AddComponent<InstanceLifetimeTracker>();
                modified = true;
            }

            if (!tracker.ConfigureForPrefabAsset(identifier))
            {
                return;
            }

            EditorUtility.SetDirty(tracker);
            modified = true;
        }

        /// <summary>
        /// Adds network discovery metadata only to spawnable/root network prefabs.
        /// Nested FishNet child NetworkObjects inherit their parent prefab's network registration.
        /// </summary>
        private static void EnsureNetworkObjectTracker(GameObject prefab, ref bool modified)
        {
            if (!prefab.TryGetComponent(out NetworkObject _))
            {
                return;
            }

            if (prefab.TryGetComponent(out NetworkObjectTracker _))
            {
                return;
            }

            prefab.AddComponent<NetworkObjectTracker>();
            modified = true;
        }

        /// <summary>
        /// Finds every prefab that violates the editor-time stamping contract.
        /// Tests use this directly so stamp regressions fail automatically.
        /// </summary>
        [NotNull]
        internal static IReadOnlyList<string> FindStampVerificationErrors()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            AssetDatabaseSettings settings = ScriptableSettings.GetOrFind<AssetDatabaseSettings>();
            List<string> errors = new();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (!prefab)
                {
                    continue;
                }

                bool isAssetDatabasePrefab = settings.Has(guid);
                bool hasInstanceLifetimeTracker = prefab.TryGetComponent(out InstanceLifetimeTracker tracker);
                bool hasNetworkObject = prefab.TryGetComponent(out NetworkObject _);
                bool hasNetworkObjectTracker = prefab.TryGetComponent(out NetworkObjectTracker _);
                bool hasAssetIdentifier = prefab.TryGetComponent(out AssetIdentifier identifier);

                if (isAssetDatabasePrefab)
                {
                    if (!hasInstanceLifetimeTracker)
                    {
                        errors.Add($"Prefab {prefab.name} at {path} with GUID {guid} doesn't have InstanceLifetimeTracker, but is included in AssetDatabaseSettings");
                    }
                    else
                    {
                        if (tracker.IsArmed)
                        {
                            errors.Add($"Prefab {prefab.name} at {path} with GUID {guid} has an armed InstanceLifetimeTracker, but should be unarmed on the asset prefab");
                        }

                        if (!hasAssetIdentifier)
                        {
                            errors.Add($"Prefab {prefab.name} at {path} with GUID {guid} doesn't have AssetIdentifier, but is included in AssetDatabaseSettings");
                        }
                        else if (tracker.Identifier != identifier)
                        {
                            errors.Add($"Prefab {prefab.name} at {path} with GUID {guid} has mismatching InstanceLifetimeTracker and AssetIdentifier references");
                        }
                        else if (tracker.Identifier.AssetGuid != guid)
                        {
                            errors.Add($"Prefab {prefab.name} at {path} with GUID {guid} has InstanceLifetimeTracker with mismatching GUID reference");
                        }
                    }
                }
                else if (hasInstanceLifetimeTracker)
                {
                    errors.Add($"Prefab {prefab.name} at {path} with GUID {guid} has InstanceLifetimeTracker, but is not included in AssetDatabaseSettings");
                }

                if (hasNetworkObject)
                {
                    if (!hasNetworkObjectTracker)
                    {
                        errors.Add($"Prefab {prefab.name} at {path} with GUID {guid} has a NetworkObject but is missing NetworkObjectTracker");
                    }

                    if (!hasAssetIdentifier)
                    {
                        errors.Add($"Prefab {prefab.name} at {path} with GUID {guid} has a NetworkObject but is missing AssetIdentifier");
                    }
                    else if (identifier.AssetGuid != guid)
                    {
                        errors.Add($"Prefab {prefab.name} at {path} with GUID {guid} has a NetworkObject but its AssetIdentifier has mismatching GUID reference");
                    }
                }
                else if (hasNetworkObjectTracker)
                {
                    errors.Add($"Prefab {prefab.name} at {path} with GUID {guid} has NetworkObjectTracker, but does not have a NetworkObject");
                }

                if (!isAssetDatabasePrefab && !hasNetworkObject && hasAssetIdentifier)
                {
                    errors.Add($"Prefab {prefab.name} at {path} with GUID {guid} has AssetIdentifier, but is neither included in AssetDatabaseSettings nor a NetworkObject prefab");
                }
            }

            return errors;
        }
    }
}
#endif
