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
            
            if (interactionEvent.Source is IGameObjectProvider provider)
            {
                float range = interactionEvent.Source.GetRange();
                Vector3 sourcePosition = provider.GameObject.transform.position;
                // Check range, ignoring height
                return (new Vector2(point.x, point.z) - new Vector2(sourcePosition.x, sourcePosition.z)).sqrMagnitude <
                       range * range;
            }

            return true;
        }
    }
}