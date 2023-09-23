using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Interactions.Extensions
{
    public static class InteractionExtensions
    {
        /// <summary>
        /// Check if position of player changed, if it did by a distance above tolerance, should return false;
        /// </summary>>
        public static bool CharacterMoveCheck(Vector3 startingPosition, Vector3 currentPosition, float tolerance = 0.1f)
        {
            return Vector3.Distance(startingPosition, currentPosition) < tolerance;
        }


        public static bool RangeCheck(InteractionEvent interactionEvent)
        {
            Vector3 point = interactionEvent.Point;

            // Ignore range when there is no point
            if (point.sqrMagnitude < 0.001)
            {
                return true;
            }

            IInteractionRangeLimit interactionRangeLimit = interactionEvent.Source.GetComponentInTree<IInteractionRangeLimit>(out IGameObjectProvider provider);
            if (interactionRangeLimit == null)
            {
                // No range limit
                return true;
            }

            // Block interaction when point is on top of wall or above.
            if (IsWallTop(point, 0.1f))
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
            if (range.IsInRange(sourcePosition, point))
            {
                return true;
            }

            Collider targetCollider = interactionEvent.Target.GetComponent<Collider>();
            if (targetCollider != null)
            {
                Vector3 closestPointOnCollider = targetCollider.ClosestPointOnBounds(sourcePosition);
                return range.IsInRange(sourcePosition, closestPointOnCollider);
            }

            Rigidbody targetRigidBody = interactionEvent.Target.GetComponent<Rigidbody>();
            if (targetRigidBody == null)
            {
                return false;
            }

            Vector3 closestPointOnRigidBody = targetRigidBody.ClosestPointOnBounds(sourcePosition);
            return range.IsInRange(sourcePosition, closestPointOnRigidBody);
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