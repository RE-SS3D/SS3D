using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Wall mounts spawned on this client. The field of view shows or hides each one with the floor tile it
    /// faces (fov.md §4, 2026-11_fov-rework D9). Main thread only.
    /// </summary>
    public static class ClientWallMounts
    {
        private static readonly List<PlacedTileObject> Members = new();

        public static IReadOnlyList<PlacedTileObject> All => Members;

        public static bool IsWallMount(PlacedTileObject placed) =>
            placed != null && placed.tileObjectSO != null
            && placed.tileObjectSO.layer is TileLayer.WallMountHigh or TileLayer.WallMountLow;

        /// <summary>World centre of the floor tile in front of the mount: the spot you'd stand on to use it.</summary>
        public static Vector3 FacingFloor(PlacedTileObject mount)
        {
            System.Tuple<int, int> step = TileHelper.ToCardinalVector(mount.Direction);
            Vector2Int floor = mount.WorldOrigin + new Vector2Int(step.Item1, step.Item2);
            return new Vector3(floor.x, mount.transform.position.y, floor.y);
        }

        internal static void Add(PlacedTileObject mount)
        {
            if (!Members.Contains(mount))
                Members.Add(mount);
        }

        internal static void Remove(PlacedTileObject mount) => Members.Remove(mount);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Members.Clear();
    }
}
