#if UNITY_EDITOR
using FishNet.Object;
using SS3D.Systems.Inventory.Containers.Editor;
using SS3D.Systems.StructuralDamage;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Electricity.Editor
{
    /// <summary>Makes the APC breakable (tier B): rounds, swings and blasts wear it down to a dead APC.</summary>
    public static class ApcPrefabSetup
    {
        private const string ApcPath = "Assets/Content/WorldObjects/Structures/WallMounts/APC.prefab";

        /// <summary>Five M4 rounds, ten laser shots, two crowbar swings or a dozen punches.</summary>
        public const float ApcMaxIntegrity = 60f;

        public static int SetupAll()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ApcPath);
            try
            {
                if (!root.TryGetComponent(out BreakableDevice device)) device = root.AddComponent<BreakableDevice>();
                SerializedObject so = new(device);
                so.FindProperty("_maxIntegrity").floatValue = ApcMaxIntegrity;
                SerializedProperty emissive = so.FindProperty("_emissiveRenderers");
                emissive.arraySize = 1;
                emissive.GetArrayElementAtIndex(0).objectReferenceValue = root.GetComponent<MeshRenderer>();
                so.ApplyModifiedPropertiesWithoutUndo();
                // The collider was authored at floor level inside the wall, under the panel drawn at chest height,
                // so rounds and swings never reached it. Fit it to the panel.
                WallMountColliderSetup.FitToMesh(root);

                StorageContainerPrefabSetup.RebuildNetworkBehaviours(root.GetComponent<NetworkObject>());
                PrefabUtility.SaveAsPrefabAsset(root, ApcPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return 1;
        }
    }
}
#endif
