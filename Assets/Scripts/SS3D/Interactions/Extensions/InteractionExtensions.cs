using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions.Extensions
{
    public static class InteractionExtensions
    {
        /// <summary>
        /// Stops the reach cast this far short of each end, so the surface the point was resolved on,
        /// or a wall mount sitting flush on the near face of a wall, doesn't count as an obstruction.
        /// </summary>
        private const float ReachEndTolerance = 0.05f;

        private static readonly RaycastHit[] ReachHits = new RaycastHit[16];

        private static int _reachMask = -1;

        // Wall and door solids are on Default; the Walls layer only holds vision obstacles.
        private static int ReachMask => _reachMask == -1 ? _reachMask = LayerMask.GetMask("Default") : _reachMask;

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

            // Block interaction when point is on top of wall or above.
            if (interactionEvent.HasPoint && IsWallTop(interactionEvent.Point, 0.1f))
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
            if (interactionEvent.HasPoint && range.IsInRange(sourcePosition, interactionEvent.Point))
            {
                return IsReachUnobstructed(sourcePosition, interactionEvent.Point, interactionEvent.Target);
            }

            // Missing or out-of-range points still range against the target itself.
            // Never treat an unresolved point as unlimited range — wall mounts without
            // colliders otherwise pass RangeCheck from anywhere.
            return IsTargetWithinRange(sourcePosition, range, interactionEvent.Target, out Vector3 reachPoint)
                && IsReachUnobstructed(sourcePosition, reachPoint, interactionEvent.Target);
        }

        /// <summary>
        /// True when no <see cref="IReachBlocker"/> (wall, window, closed door) other than the target
        /// itself sits between <paramref name="origin"/> and <paramref name="reachPoint"/>. Casts both
        /// ways, because a ray that starts inside a collider does not hit that collider.
        /// </summary>
        public static bool IsReachUnobstructed(Vector3 origin, Vector3 reachPoint, IInteractionTarget target)
        {
            Transform targetRoot = GetTargetTransform(target);
            if (targetRoot != null)
            {
                IReachSurface surface = targetRoot.GetComponentInParent<IReachSurface>();
                if (surface != null)
                {
                    reachPoint = surface.GetReachPoint(reachPoint);
                }
            }

            Vector3 delta = reachPoint - origin;
            float distance = delta.magnitude;
            if (distance <= ReachEndTolerance)
            {
                return true;
            }

            Vector3 direction = delta / distance;
            float castLength = distance - ReachEndTolerance;

            return !HasBlockingHit(origin, direction, castLength, targetRoot)
                && !HasBlockingHit(reachPoint, -direction, castLength, targetRoot);
        }

        /// <summary>
        /// The target's transform. Not every target is an <see cref="IGameObjectProvider"/> (airlocks,
        /// lockers), so <see cref="InteractionTargetExtensions.GetGameObject"/> alone can't exempt them.
        /// </summary>
        private static Transform GetTargetTransform(IInteractionTarget target)
        {
            GameObject targetObject = target?.GetGameObject();
            if (targetObject != null)
            {
                return targetObject.transform;
            }

            return target is Component component && component != null ? component.transform : null;
        }

        private static bool HasBlockingHit(Vector3 origin, Vector3 direction, float length, Transform targetRoot)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, ReachHits, length, ReachMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hitCollider = ReachHits[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                if (targetRoot != null && hitCollider.transform.IsChildOf(targetRoot))
                {
                    continue;
                }

                IReachBlocker blocker = hitCollider.GetComponentInParent<IReachBlocker>();
                if (blocker == null || !blocker.BlocksReach)
                {
                    continue;
                }

                // The target is part of the blocker (an airlock's own panel, a wall's interaction child).
                if (targetRoot != null && blocker is Component blockerComponent && targetRoot.IsChildOf(blockerComponent.transform))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool IsTargetWithinRange(Vector3 sourcePosition, RangeLimit range, IInteractionTarget target, out Vector3 reachPoint)
        {
            reachPoint = sourcePosition;
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
                reachPoint = targetCollider.ClosestPointOnBounds(sourcePosition);
                return range.IsInRange(sourcePosition, reachPoint);
            }

            Rigidbody targetRigidBody = target.GetComponent<Rigidbody>();
            if (targetRigidBody != null)
            {
                reachPoint = targetRigidBody.ClosestPointOnBounds(sourcePosition);
                return range.IsInRange(sourcePosition, reachPoint);
            }

            GameObject gameObject = target.GetGameObject();
            if (gameObject != null)
            {
                reachPoint = gameObject.transform.position;
                return range.IsInRange(sourcePosition, reachPoint);
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
