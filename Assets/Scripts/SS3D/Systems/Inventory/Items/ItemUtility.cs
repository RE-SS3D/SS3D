using UnityEngine;

namespace SS3D.Systems.Inventory.Items
{
    public static class ItemUtility
    {
        /// <summary>
        /// Puts <paramref name="item"/> on a surface at <paramref name="position"/>: applies the rotation, then
        /// lifts the item so the bottom of its colliders rests on the surface (centre-pivot items such as
        /// medkits otherwise sink halfway into the floor). Callers that register the item with the tilemap
        /// must pass the item's resulting transform, not the raw surface point.
        /// </summary>
        public static void Place(Item item, Vector3 position, Quaternion rotation, Transform lookTarget = null)
        {
            Transform itemTransform = item.transform;
            itemTransform.position = position;

            if (lookTarget)
            {
                itemTransform.LookAt(lookTarget);
            }
            else
            {
                itemTransform.rotation = rotation;
            }

            // Collider bounds lag the transform until physics syncs.
            Physics.SyncTransforms();
            if (TryGetColliderBounds(item, out Bounds bounds))
            {
                float lift = position.y - bounds.min.y;
                if (lift > 0f)
                {
                    itemTransform.position = position + new Vector3(0f, lift, 0f);
                }
            }

            SetWorldPose(itemTransform, itemTransform.position, itemTransform.rotation);
        }

        /// <summary>
        /// Moves an item and its Rigidbody together. An interpolated Rigidbody that is moved only through
        /// its Transform in the frame it was created snaps back to its creation pose (soda cans landed at
        /// the world origin after map-editor placement), so the body pose is written as well.
        /// </summary>
        public static void SetWorldPose(Transform itemTransform, Vector3 position, Quaternion rotation)
        {
            itemTransform.SetPositionAndRotation(position, rotation);
            if (itemTransform.TryGetComponent(out Rigidbody body))
            {
                body.position = position;
                body.rotation = rotation;
            }
        }

        private static bool TryGetColliderBounds(Item item, out Bounds bounds)
        {
            bounds = default;
            bool found = false;

            foreach (Collider collider in item.NativeColliders)
            {
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = collider.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            return found;
        }
    }
}
