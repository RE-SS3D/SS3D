using SS3D.Data;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    /// <summary>
    /// The one owner of an item's world rotation on the floor: the item's rest pose (<see cref="Item.WorldRestRotation"/>)
    /// turned by a yaw. Every spawn, drop, place, throw and outlet path that puts an item down at rest goes through
    /// here (Documents/plans/item-orientation.plan.md B2). Paths that set a deliberate non-rest pose (a wall-mounted
    /// charge, a severed limb at its anchor, a vending tumble, a teleport that keeps the current pose) do not.
    /// </summary>
    public static class ItemPlacement
    {
        /// <summary>World rotation of <paramref name="item"/> lying at rest, turned <paramref name="yawDegrees"/> about world up.</summary>
        public static Quaternion WorldRotation(Item item, float yawDegrees) =>
            WorldRotation(item.WorldRestRotation, yawDegrees);

        /// <summary>World rotation of the item a tilemap entry places, at rest and turned <paramref name="yawDegrees"/>.</summary>
        public static Quaternion WorldRotation(ItemObjectSo itemSo, float yawDegrees)
        {
            Item prefab = itemSo != null && itemSo.PrefabAsset ? Assets.Get<Item>(itemSo.PrefabAsset) : null;
            return prefab != null ? WorldRotation(prefab, yawDegrees) : Quaternion.Euler(0f, yawDegrees, 0f);
        }

        /// <summary>A rest rotation turned <paramref name="yawDegrees"/> about world up, for callers without an <see cref="Item"/>.</summary>
        public static Quaternion WorldRotation(Quaternion restRotation, float yawDegrees) =>
            Quaternion.Euler(0f, yawDegrees, 0f) * restRotation;

        /// <summary>
        /// The yaw <paramref name="worldRotation"/> was composed with, the inverse of <see cref="WorldRotation(Item, float)"/>.
        /// Reading <c>eulerAngles.y</c> instead adds the rest pose's own yaw (M4, bike horn) and is gimbal-locked
        /// for a pitched rest pose.
        /// </summary>
        public static float YawAtRest(Item item, Quaternion worldRotation) =>
            YawOf(worldRotation * Quaternion.Inverse(item.WorldRestRotation) * Vector3.forward);

        /// <summary>
        /// The yaw of a horizontal heading, in degrees (0 = world +Z). A vertical or zero heading gives 0.
        /// </summary>
        public static float YawOf(Vector3 heading) =>
            heading.x == 0f && heading.z == 0f ? 0f : Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
    }
}
