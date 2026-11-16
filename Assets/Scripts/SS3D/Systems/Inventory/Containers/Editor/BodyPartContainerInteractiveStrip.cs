#if UNITY_EDITOR
using System.Collections.Generic;
using FishNet.Object;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers.Editor
{
    /// <summary>
    /// Strips the world-click <see cref="ContainerInteractive"/> from the HumanHead/HumanTorso root
    /// GameObjects so hovering/clicking a player's head or torso no longer offers Store/Take/View
    /// container interactions during combat and examine targeting — confirmed live debt, not intentional
    /// (see TECH_DEBT.md §1.1 / Documents/architecture/systems/entities-prefab-recipes.md § Tests). The clothing-slot
    /// <see cref="AttachedContainer"/> itself is kept (still reachable via the HUD equip/storage UI);
    /// only the world-click surface is removed. Surgery's future body-part-as-container work will
    /// re-add a world-interactive surface deliberately rather than reviving this one.
    /// Re-run if the strip regresses (e.g. someone re-enables "Is Interactive" on the Inspector).
    /// </summary>
    public static class BodyPartContainerInteractiveStrip
    {
        private static readonly string[] TargetPrefabPaths =
        {
            "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanBodyParts/HumanHead.prefab",
            "Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanBodyParts/HumanTorso.prefab",
        };

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Inventory.Containers.Editor.BodyPartContainerInteractiveStrip.StripBatch</c>
        /// Prefer <see cref="SS3D.Systems.Entities.Editor.HumanPrefabRecipes.RunAllBatch"/> for Human re-runs.</summary>
        public static void StripBatch()
        {
            int stripped = StripAll();
            Debug.Log($"[BodyPartContainerInteractiveStrip] Stripped root ContainerInteractive from {stripped} prefab(s).");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        public static int StripAll()
        {
            int stripped = 0;
            foreach (string path in TargetPrefabPaths)
            {
                if (StripPrefab(path))
                {
                    stripped++;
                }
            }

            return stripped;
        }

        private static bool StripPrefab(string prefabPath)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[BodyPartContainerInteractiveStrip] Missing prefab: {prefabPath}");
                return false;
            }

            try
            {
                if (!prefabRoot.TryGetComponent(out ContainerInteractive rootInteractive))
                {
                    Debug.Log($"[BodyPartContainerInteractiveStrip] {prefabPath} already has no root ContainerInteractive.");
                    return false;
                }

                if (!prefabRoot.TryGetComponent(out NetworkObject rootNetworkObject))
                {
                    Debug.LogError($"[BodyPartContainerInteractiveStrip] No root NetworkObject on {prefabPath}");
                    return false;
                }

                AttachedContainer attachedContainer = rootInteractive.attachedContainer;
                if (attachedContainer != null)
                {
                    SerializedObject containerSo = new(attachedContainer);
                    containerSo.FindProperty("ContainerInteractive").objectReferenceValue = null;
                    containerSo.FindProperty("_isInteractive").boolValue = false;
                    containerSo.FindProperty("_isOpenable").boolValue = false;
                    containerSo.FindProperty("_onlyStoreWhenOpen").boolValue = false;
                    containerSo.FindProperty("_hasCustomInteraction").boolValue = false;
                    containerSo.FindProperty("_openWhenContainerViewed").boolValue = false;
                    containerSo.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(attachedContainer);
                }

                Object.DestroyImmediate(rootInteractive, true);
                RebuildNetworkBehaviours(rootNetworkObject);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log($"[BodyPartContainerInteractiveStrip] Stripped root ContainerInteractive from {prefabPath}");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        /// <summary>
        /// Same nested-Nob-aware rebuild as <c>HumanPrefabHygiene</c> — HumanHead.prefab nests
        /// HumanEarLeft/Right, each with its own <see cref="NetworkObject"/>, so the walk must not stop
        /// at a nested (non-root) Nob or it silently drops the ears' behaviours. See entities.md § Pitfalls.
        /// </summary>
        private static void RebuildNetworkBehaviours(NetworkObject rootNetworkObject)
        {
            List<NetworkBehaviour> behaviours = new();
            CollectNetworkBehaviours(rootNetworkObject.transform, behaviours);

            SerializedObject nobSo = new(rootNetworkObject);
            SerializedProperty listProp = nobSo.FindProperty("_networkBehaviours");
            listProp.arraySize = behaviours.Count;
            for (int i = 0; i < behaviours.Count; i++)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = behaviours[i];

                SerializedObject behaviourSo = new(behaviours[i]);
                behaviourSo.FindProperty("_componentIndexCache").intValue = i;
                behaviourSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(behaviours[i]);
            }

            nobSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rootNetworkObject);
        }

        private static void CollectNetworkBehaviours(Transform transform, List<NetworkBehaviour> behaviours)
        {
            behaviours.AddRange(transform.GetComponents<NetworkBehaviour>());
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.TryGetComponent(out NetworkObject childNob) && !childNob.IsNested)
                {
                    continue;
                }

                CollectNetworkBehaviours(child, behaviours);
            }
        }
    }
}
#endif
