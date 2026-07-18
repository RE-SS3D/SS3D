using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions.Extensions
{
    public static class InteractionExtensions
    {
        /// <summary>
        /// Resolves the interaction tier, defaulting to instant when not explicitly provided.
        /// </summary>
        public static InteractionTier GetInteractionTier(this IInteraction interaction, InteractionEvent interactionEvent)
        {
            if (interaction is IInteractionTierProvider tierProvider)
            {
                return tierProvider.GetTier(interactionEvent);
            }

            return InteractionTier.Instant;
        }

        /// <summary>
        /// Check if position of player changed, if it did by a distance above tolerance, should return false;
        /// </summary>>
        public static bool CharacterMoveCheck(Vector3 startingPosition, Vector3 currentPosition, float tolerance = 0.1f)
        {
            return Vector3.Distance(startingPosition, currentPosition) < tolerance;
        }


        public static bool RangeCheck(InteractionEvent interactionEvent)
        {
            IInteractionRangeLimit interactionRangeLimit = interactionEvent.Source.GetComponentInTree<IInteractionRangeLimit>(out IGameObjectProvider provider);
            if (interactionRangeLimit == null)
            {
                // No range limit
                return true;
            }

            Vector3 point = interactionEvent.Point;

            // Block interaction when point is on top of wall or above.
            if (HasResolvedPoint(point) && IsWallTop(point, 0.1f))
            {
                return false;
            }

            Vector3 sourcePosition;
            if (provider is IInteractionOriginProvider origin)
            {
                // Object has a custom interaction origin
                sourcePosition = origin.InteractionOrigin;
            }
            else
            {
                // Use default game object origin
                sourcePosition = provider.GameObject.transform.position;
            }

            RangeLimit range = interactionEvent.Source.GetRange();
            if (HasResolvedPoint(point) && range.IsInRange(sourcePosition, point))
            {
                return true;
            }

            // Missing or out-of-range points still range against the target itself.
            // Never treat an unresolved (default zero) point as unlimited range — wall mounts
            // without colliders otherwise pass RangeCheck from anywhere.
            return IsTargetWithinRange(sourcePosition, range, interactionEvent.Target);
        }

        /// <summary>
        /// Default <see cref="InteractionEvent"/> point is Vector3.zero when unset.
        /// </summary>
        private static bool HasResolvedPoint(Vector3 point)
        {
            return point.sqrMagnitude >= 0.001f;
        }

        private static bool IsTargetWithinRange(Vector3 sourcePosition, RangeLimit range, IInteractionTarget target)
        {
            if (target == null)
            {
                return false;
            }

            Collider targetCollider = target.GetComponent<Collider>();
            if (targetCollider == null)
            {
                GameObject targetObject = target.GetGameObject();
                if (targetObject != null)
                {
                    targetCollider = targetObject.GetComponentInChildren<Collider>();
                }
            }

            if (targetCollider != null)
            {
                Vector3 closestPointOnCollider = targetCollider.ClosestPointOnBounds(sourcePosition);
                return range.IsInRange(sourcePosition, closestPointOnCollider);
            }

            Rigidbody targetRigidBody = target.GetComponent<Rigidbody>();
            if (targetRigidBody != null)
            {
                Vector3 closestPointOnRigidBody = targetRigidBody.ClosestPointOnBounds(sourcePosition);
                return range.IsInRange(sourcePosition, closestPointOnRigidBody);
            }

            GameObject gameObject = target.GetGameObject();
            if (gameObject != null)
            {
                return range.IsInRange(sourcePosition, gameObject.transform.position);
            }

            return false;
        }

        private static bool IsWallTop(Vector3 position, float deadzone = 0)
        {
            return false;
            /*
            TileObject tileObject = TileManager.singleton.GetTile(position);
            if (!tileObject.Tile.turf.isWall)
            {
                return false;
            }

            GameObject wallGameObject = tileObject.GetLayer(1);
            Collider[] collidersOnWall = wallGameObject.GetComponentsInChildren<Collider>();
            float topHeight = 0;
            for (int i = 0; i < collidersOnWall.Length; i++)
            {
                topHeight = Mathf.Max(topHeight, collidersOnWall[i].bounds.max.y);
            }

            return position.y >= topHeight - deadzone;
            */
        }
    }
}
