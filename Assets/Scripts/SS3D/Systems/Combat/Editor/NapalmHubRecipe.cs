#if UNITY_EDITOR
using FishNet.Object;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Combat.Editor
{
    /// <summary>Appends world-owned napalm to the hub without changing existing behaviour indices.</summary>
    public static class NapalmHubRecipe
    {
        public static bool SetupAll()
        {
            const string path = "Assets/Resources/NetworkSystemsHub.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponent<NapalmSubSystem>() != null) return false;
                root.AddComponent<NapalmSubSystem>();
                NetworkObject networkObject = root.GetComponent<NetworkObject>();
                CombatInteractionNetworkPrefabSetup.RebuildNetworkBehaviours(networkObject);
                SerializedObject serialized = new(networkObject);
                serialized.FindProperty("_scenePathHash").longValue = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
#endif
