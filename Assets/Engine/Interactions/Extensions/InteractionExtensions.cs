using UnityEngine;

namespace SS3D.Engine.Interactions.Extensions
{
    public static class InteractionExtensions
    {
        public static bool RangeCheck(InteractionEvent interactionEvent)
        {
            Vector3 point = interactionEvent.Point;

            // Ignore range when there is no point
            if (point.sqrMagnitude < 0.001)
            {
                return true;
            }

            var interactionRangeLimit = interactionEvent.Source.GetComponentInTree<IInteractionRangeLimit>(out IGameObjectProvider provider);
            if (interactionRangeLimit == null)
            {
                // No range limit
                return true;
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
            float horizontal = range.horizontal;
            // Check horizontal and vertical range
            return Mathf.Abs(point.y - sourcePosition.y) < range.vertical &&
                   (new Vector2(point.x, point.z) - new Vector2(sourcePosition.x, sourcePosition.z)).sqrMagnitude <
                   horizontal * horizontal;
        }
    }
}