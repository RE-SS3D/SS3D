#if UNITY_EDITOR
using FishNet.Object;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.ActionMessages.Editor
{
    /// <summary>
    /// Tier-B recipe: puts <see cref="ActionMessageSubSystem"/> on the NetworkSystemsHub prefab, appended
    /// last so existing component indices are unchanged, and rewrites the hub NetworkObject's behaviour
    /// list with ConstructionHubRecipe's rewrite. The full hub rebuild lists the type too. Idempotent.
    /// </summary>
    public static class ActionMessageHubRecipe
    {
        public const string HubPath = "Assets/Resources/NetworkSystemsHub.prefab";

        public static bool SetupAll()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HubPath);
            if (root == null)
            {
                Debug.LogError($"[ActionMessageHubRecipe] Missing {HubPath}");
                return false;
            }

            try
            {
                if (root.GetComponent<ActionMessageSubSystem>() != null)
                {
                    return false;
                }

                root.AddComponent<ActionMessageSubSystem>();
                SS3D.Systems.Construction.Editor.ConstructionHubRecipe.RebuildNetworkBehaviours(root.GetComponent<NetworkObject>());
                PrefabUtility.SaveAsPrefabAsset(root, HubPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
#endif
