#if UNITY_EDITOR
using FishNet.Object;
using SS3D.Systems.Inventory.Containers.Editor;
using UnityEditor;
using UnityEngine;

namespace SS3D.Systems.Electricity.Editor
{
    /// <summary>
    /// Fits wall-mount colliders to their panels (tier B). The light switch and air alarm boxes were authored at
    /// floor height inside the wall, under panels drawn at chest height, so clicks, reach checks and rounds
    /// resolved against the wrong place. <see cref="ApcPrefabSetup"/> fits the APC the same way.
    /// </summary>
    public static class WallMountColliderSetup
    {
        private static readonly string[] Paths =
        {
            "Assets/Content/WorldObjects/Structures/WallMounts/LightSwitch.prefab",
            "Assets/Content/WorldObjects/Structures/WallMounts/AirAlarm.prefab",
        };

        public static int SetupAll()
        {
            foreach (string path in Paths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    FitToMesh(root);
                    // Loading re-adds components the scripts require but the prefab lacked (the air alarm's
                    // PlacedTileObject and AuthLogDeviceBehaviour), so the network list must cover them.
                    StorageContainerPrefabSetup.RebuildNetworkBehaviours(root.GetComponent<NetworkObject>());
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return Paths.Length;
        }

        /// <summary>Sizes the root's box collider to the root mesh's bounds.</summary>
        public static void FitToMesh(GameObject root)
        {
            BoxCollider box = root.GetComponent<BoxCollider>();
            Bounds panel = root.GetComponent<MeshFilter>().sharedMesh.bounds;
            box.center = panel.center;
            box.size = panel.size;
        }
    }
}
#endif
