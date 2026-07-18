#if UNITY_EDITOR
using System.Collections.Generic;
using FishNet.Object;
using SS3D.Systems.Inventory.Items;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Adds HasUi storage (<see cref="AttachedContainer"/>) to backpack / toolbelt / locker prefabs
    /// so gear-strip click and <see cref="Interactions.ViewContainerInteraction"/> open StoragePanel.
    /// </summary>
    public static class StorageContainerPrefabSetup
    {
        private readonly struct Target
        {
            public readonly string PrefabPath;
            public readonly Vector2Int Size;
            public readonly SizeClass MaxSizeClass;
            public readonly float MaxWeight;

            public Target(string prefabPath, int width, int height, SizeClass maxSizeClass, float maxWeight)
            {
                PrefabPath = prefabPath;
                Size = new Vector2Int(width, height);
                MaxSizeClass = maxSizeClass;
                MaxWeight = maxWeight;
            }
        }

        private static readonly Target[] Targets =
        {
            // Design inventory-storage.md §12: backpack 6 slots, small-max.
            new("Assets/Content/WorldObjects/Items/Clothing/Backpack.prefab", 3, 2, SizeClass.Small, 20f),
            new("Assets/Content/WorldObjects/Items/Clothing/Toolbelt.prefab", 4, 1, SizeClass.Small, 10f),
            new("Assets/Content/WorldObjects/Furniture/Storage/Lockers/Locker.prefab", 4, 4, SizeClass.Bulky, 80f),
            new("Assets/Content/WorldObjects/Furniture/Storage/Lockers/LockerSecure.prefab", 4, 4, SizeClass.Bulky, 80f),
            new("Assets/Content/WorldObjects/Furniture/Storage/Lockers/SecurityLocker.prefab", 4, 4, SizeClass.Bulky, 80f),
        };

        [MenuItem("SS3D/Inventory/Hook Up Storage Prefabs (Backpack/Toolbelt/Lockers)")]
        public static void HookUpMenu()
        {
            int updated = HookUpAll();
            EditorUtility.DisplayDialog(
                "Storage Prefabs",
                $"Hooked up {updated} / {Targets.Length} prefabs.",
                "OK");
        }

        /// <summary>BatchMode entry: <c>-executeMethod SS3D.Systems.Inventory.Containers.Editor.StorageContainerPrefabSetup.HookUpBatch</c></summary>
        public static void HookUpBatch()
        {
            int updated = HookUpAll();
            Debug.Log($"[StorageContainerPrefabSetup] Hooked up {updated} / {Targets.Length} prefabs.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(updated > 0 ? 0 : 1);
            }
        }

        public static int HookUpAll()
        {
            int updated = 0;
            foreach (Target target in Targets)
            {
                if (HookUpPrefab(target))
                {
                    updated++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return updated;
        }

        private static bool HookUpPrefab(Target target)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(target.PrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[StorageContainerPrefabSetup] Missing prefab: {target.PrefabPath}");
                return false;
            }

            try
            {
                if (!prefabRoot.TryGetComponent(out NetworkObject networkObject))
                {
                    Debug.LogError($"[StorageContainerPrefabSetup] No NetworkObject on {target.PrefabPath}");
                    return false;
                }

                if (TryFindExistingStorage(prefabRoot, out AttachedContainer existing))
                {
                    ApplyStorageSettings(existing, target);
                    EnsureContainerInteractive(prefabRoot, existing);
                    RebuildNetworkBehaviours(networkObject);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, target.PrefabPath);
                    Debug.Log($"[StorageContainerPrefabSetup] Updated existing storage on {target.PrefabPath}");
                    return true;
                }

                GameObject storageGo = new("Storage");
                storageGo.transform.SetParent(prefabRoot.transform, false);
                AttachedContainer storage = storageGo.AddComponent<AttachedContainer>();
                ApplyStorageSettings(storage, target);
                EnsureContainerInteractive(prefabRoot, storage);
                RebuildNetworkBehaviours(networkObject);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, target.PrefabPath);
                Debug.Log($"[StorageContainerPrefabSetup] Added Storage child on {target.PrefabPath} ({target.Size.x}x{target.Size.y}, max {target.MaxSizeClass})");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static bool TryFindExistingStorage(GameObject root, out AttachedContainer storage)
        {
            storage = null;
            foreach (AttachedContainer candidate in root.GetComponentsInChildren<AttachedContainer>(true))
            {
                if (candidate.DisplayAsSlotInUI)
                {
                    continue;
                }

                // Prefer HasUi grids; otherwise take the first non-slot container under this prefab.
                if (candidate.HasUi || storage == null)
                {
                    storage = candidate;
                    if (candidate.HasUi)
                    {
                        return true;
                    }
                }
            }

            return storage != null;
        }

        private static void ApplyStorageSettings(AttachedContainer storage, Target target)
        {
            SerializedObject so = new(storage);
            so.FindProperty("_automaticContainerSetUp").boolValue = true;
            so.FindProperty("_hasUi").boolValue = true;
            so.FindProperty("_displayAsSlotInUI").boolValue = false;
            so.FindProperty("_isInteractive").boolValue = true;
            so.FindProperty("_isOpenable").boolValue = false;
            so.FindProperty("_onlyStoreWhenOpen").boolValue = false;
            so.FindProperty("_openWhenContainerViewed").boolValue = false;
            so.FindProperty("_hasCustomInteraction").boolValue = false;
            so.FindProperty("_attachItems").boolValue = true;
            so.FindProperty("_hideItems").boolValue = true;
            so.FindProperty("_maxDistance").floatValue = 5f;
            so.FindProperty("_size").vector2IntValue = target.Size;
            so.FindProperty("_type").intValue = (int)ContainerType.None;
            so.FindProperty("_startFilter").objectReferenceValue = null;
            so.FindProperty("_maxSizeClass").enumValueIndex = (int)target.MaxSizeClass;
            so.FindProperty("_maxWeight").floatValue = target.MaxWeight;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(storage);
        }

        private static void EnsureContainerInteractive(GameObject root, AttachedContainer storage)
        {
            ContainerInteractive interactive = root.GetComponent<ContainerInteractive>();
            if (interactive == null)
            {
                interactive = root.AddComponent<ContainerInteractive>();
            }

            SerializedObject storageSo = new(storage);
            storageSo.FindProperty("ContainerInteractive").objectReferenceValue = interactive;
            storageSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject interactiveSo = new(interactive);
            interactiveSo.FindProperty("attachedContainer").objectReferenceValue = storage;
            interactiveSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(interactive);
            EditorUtility.SetDirty(storage);
        }

        /// <summary>
        /// Mirrors FishNet's UpdateNetworkBehaviours for prefab assets: collect NetworkBehaviours on
        /// this object and descendants that do not have their own NetworkObject, then rewrite indices.
        /// </summary>
        private static void RebuildNetworkBehaviours(NetworkObject networkObject)
        {
            List<NetworkBehaviour> behaviours = new();
            CollectNetworkBehaviours(networkObject.transform, behaviours);

            SerializedObject nobSo = new(networkObject);
            SerializedProperty listProp = nobSo.FindProperty("_networkBehaviours");
            listProp.arraySize = behaviours.Count;
            for (int i = 0; i < behaviours.Count; i++)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = behaviours[i];

                SerializedObject behaviourSo = new(behaviours[i]);
                behaviourSo.FindProperty("_addedNetworkObject").objectReferenceValue = networkObject;
                behaviourSo.FindProperty("_networkObjectCache").objectReferenceValue = networkObject;
                behaviourSo.FindProperty("_componentIndexCache").intValue = i;
                behaviourSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(behaviours[i]);
            }

            nobSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(networkObject);
        }

        private static void CollectNetworkBehaviours(Transform transform, List<NetworkBehaviour> behaviours)
        {
            behaviours.AddRange(transform.GetComponents<NetworkBehaviour>());
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.TryGetComponent(out NetworkObject _))
                {
                    continue;
                }

                CollectNetworkBehaviours(child, behaviours);
            }
        }
    }
}
#endif
