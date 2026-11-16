#if UNITY_EDITOR
using System.Collections.Generic;
using FishNet.Object;
using SS3D.Systems.Interactions;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Combat.Editor
{
    /// <summary>
    /// Ensures <see cref="CombatInteractionNetwork"/> on Human / TestHuman roots and rebuilds
    /// FishNet <c>_networkBehaviours</c> (tier B — registered on HumanPrefabRecipes).
    /// Rebuild walk matches <c>HumanPrefabHygiene</c> (descend into nested body-part NOs).
    /// </summary>
    public static class CombatInteractionNetworkPrefabSetup
    {
        private const string HumanPrefabPath = "Assets/Content/WorldObjects/Entities/Humanoids/Human/Human.prefab";

        [InitializeOnLoadMethod]
        private static void AutoWireIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                {
                    return;
                }

                GameObject human = AssetDatabase.LoadAssetAtPath<GameObject>(HumanPrefabPath);
                if (human == null || human.GetComponent<CombatInteractionNetwork>() != null)
                {
                    return;
                }

                Wire();
            };
        }

        /// <summary>BatchMode: <c>-executeMethod SS3D.Systems.Combat.Editor.CombatInteractionNetworkPrefabSetup.WireBatch</c></summary>
        public static void WireBatch()
        {
            bool changed = Wire();
            Debug.Log($"[CombatInteractionNetworkPrefabSetup] {(changed ? "Updated" : "Already present on")} Human.prefab.");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        public static bool Wire()
        {
            bool changed = EnsureOnPrefab(HumanPrefabPath);
            // TestHuman is a stripped visual/smoke prefab without InteractionController — skip.
            if (changed)
            {
                AssetDatabase.SaveAssets();
            }

            return changed;
        }

        private static bool EnsureOnPrefab(string prefabPath)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"[CombatInteractionNetworkPrefabSetup] Missing prefab: {prefabPath}");
                return false;
            }

            try
            {
                if (!prefabRoot.TryGetComponent(out NetworkObject rootNetworkObject))
                {
                    Debug.LogError($"[CombatInteractionNetworkPrefabSetup] No root NetworkObject on {prefabPath}");
                    return false;
                }

                if (!prefabRoot.TryGetComponent(out InteractionController interactionController))
                {
                    Debug.LogError($"[CombatInteractionNetworkPrefabSetup] No InteractionController on {prefabPath}");
                    return false;
                }

                bool added = false;
                if (!prefabRoot.TryGetComponent(out CombatInteractionNetwork combat))
                {
                    combat = prefabRoot.AddComponent<CombatInteractionNetwork>();
                    added = true;
                }

                SerializedObject combatSo = new(combat);
                SerializedProperty controllerProp = combatSo.FindProperty("_interactionController");
                bool wired = false;
                if (controllerProp != null && controllerProp.objectReferenceValue != interactionController)
                {
                    controllerProp.objectReferenceValue = interactionController;
                    combatSo.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(combat);
                    wired = true;
                }

                if (!added && !wired)
                {
                    return false;
                }

                RebuildNetworkBehaviours(rootNetworkObject);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log($"[CombatInteractionNetworkPrefabSetup] {(added ? "Added" : "Wired")} CombatInteractionNetwork on {prefabPath}");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        /// <summary>
        /// FishNet's UpdateNetworkBehaviours for a prefab asset: lists every NetworkBehaviour under the
        /// root on its NetworkObject and points each back at it. Run after adding one in a recipe.
        /// </summary>
        internal static void RebuildNetworkBehaviours(NetworkObject rootNetworkObject)
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
                behaviourSo.FindProperty("_addedNetworkObject").objectReferenceValue = rootNetworkObject;
                behaviourSo.FindProperty("_networkObjectCache").objectReferenceValue = rootNetworkObject;
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
