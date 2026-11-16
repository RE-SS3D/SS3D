using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Selection
{
    /// <summary>
    /// Resolves world-space interaction points for a shader-picked selectable.
    /// </summary>
    public static class SelectionTargetUtility
    {
        /// <summary>
        /// Casts a camera ray against colliders on the selectable and its non-selectable
        /// descendants, producing a point and normal for interaction range checks.
        /// </summary>
        public static bool TryResolveInteractionPoint(Camera camera, Selectable selectable, out Vector3 point, out Vector3 normal)
        {
            point = Vector3.zero;
            normal = Vector3.zero;

            if (camera == null || selectable == null)
            {
                return false;
            }

            Vector2 screenPosition = Mouse.current != null
                ? Mouse.current.position.ReadValue()
                : new Vector2(camera.pixelWidth * 0.5f, camera.pixelHeight * 0.5f);

            Ray ray = camera.ScreenPointToRay(screenPosition);
            return TryResolveInteractionPoint(ray, selectable, out point, out normal);
        }

        /// <summary>
        /// Casts a ray against colliders on the selectable and its non-selectable
        /// descendants, producing a point and normal for interaction range checks.
        /// </summary>
        public static bool TryResolveInteractionPoint(Ray ray, Selectable selectable, out Vector3 point, out Vector3 normal)
        {
            point = Vector3.zero;
            normal = Vector3.zero;

            if (selectable == null)
            {
                return false;
            }

            List<Collider> colliders = new();
            CollectColliders(selectable.gameObject, selectable, colliders);

            if (colliders.Count == 0)
            {
                return false;
            }

            float closestDistance = float.PositiveInfinity;
            RaycastHit closestHit = default;
            bool foundHit = false;

            foreach (Collider collider in colliders)
            {
                if (!collider.Raycast(ray, out RaycastHit hit, float.PositiveInfinity))
                {
                    continue;
                }

                if (hit.distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = hit.distance;
                closestHit = hit;
                foundHit = true;
            }

            if (foundHit)
            {
                point = closestHit.point;
                normal = closestHit.normal;
                return true;
            }

            float closestDistanceSqr = float.PositiveInfinity;
            Vector3 closestPoint = Vector3.zero;
            bool foundClosest = false;

            foreach (Collider collider in colliders)
            {
                Vector3 candidate = GetClosestPoint(collider, ray.origin);
                float distanceSqr = (candidate - ray.origin).sqrMagnitude;
                if (distanceSqr >= closestDistanceSqr)
                {
                    continue;
                }

                closestDistanceSqr = distanceSqr;
                closestPoint = candidate;
                foundClosest = true;
            }

            if (!foundClosest)
            {
                return false;
            }

            point = closestPoint;
            normal = (closestPoint - ray.origin).sqrMagnitude > 0.001f
                ? (closestPoint - ray.origin).normalized
                : Vector3.up;

            return true;
        }

        /// <summary>
        /// <see cref="Collider.ClosestPoint"/> only supports box/sphere/capsule and convex meshes.
        /// Non-convex MeshColliders (common on furniture) must use bounds instead or Unity logs every frame.
        /// </summary>
        private static Vector3 GetClosestPoint(Collider collider, Vector3 position)
        {
            if (collider is MeshCollider { convex: false })
            {
                return collider.ClosestPointOnBounds(position);
            }

            if (collider is BoxCollider or SphereCollider or CapsuleCollider or MeshCollider)
            {
                return collider.ClosestPoint(position);
            }

            return collider.ClosestPointOnBounds(position);
        }

        private static void CollectColliders(GameObject gameObject, Selectable root, List<Collider> colliders)
        {
            Selectable current = gameObject.GetComponent<Selectable>();
            if (current != null && current != root)
            {
                return;
            }

            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null && collider.enabled)
            {
                colliders.Add(collider);
            }

            foreach (Transform child in gameObject.transform)
            {
                CollectColliders(child.gameObject, root, colliders);
            }
        }
    }
}
