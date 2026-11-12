#if UNITY_EDITOR
using System.Collections.Generic;
using FishNet.Object;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Construction.Editor
{
    /// <summary>
    /// Tier-B recipe: puts <see cref="ConstructionPlacementSubSystem"/> on the NetworkSystemsHub prefab and
    /// rewrites the hub NetworkObject's behaviour list, so the new behaviour is part of the spawn. Appended
    /// last, existing component indices are unchanged. The full hub rebuild
    /// (SS3D → Bootstrap → Rebuild NetworkSystemsHub Prefab) lists the type too. Idempotent.
    /// </summary>
    public static class ConstructionHubRecipe
    {
        public const string HubPath = "Assets/Resources/NetworkSystemsHub.prefab";

        public static bool SetupAll()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HubPath);
            if (root == null)
            {
                Debug.LogError($"[ConstructionHubRecipe] Missing {HubPath}");
                return false;
            }

            try
            {
                if (root.GetComponent<ConstructionPlacementSubSystem>() != null)
                {
                    return false;
                }

                root.AddComponent<ConstructionPlacementSubSystem>();
                NetworkObject networkObject = root.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    RebuildNetworkBehaviours(networkObject);
                }

                PrefabUtility.SaveAsPrefabAsset(root, HubPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Same rewrite as the hub rebuild and StorageContainerPrefabSetup (FishNet's UpdateNetworkBehaviours for assets).</summary>
        internal static void RebuildNetworkBehaviours(NetworkObject networkObject)
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
