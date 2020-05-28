using UnityEngine;

namespace SS3D.Engine.Interactions.Extensions
{
    public static class InteractionExtensions
    {
        public static bool RangeCheck(InteractionEvent interactionEvent)
        {
            // Ignore range when there is no point
            if (interactionEvent.Point.sqrMagnitude < 0.001)
            {
                return true;
            }
            
            if (interactionEvent.Source is IGameObjectProvider provider)
            {
                return Vector3.Distance(provider.GameObject.transform.position, interactionEvent.Point) <
                       interactionEvent.Source.GetRange();
            }

            return true;
        }
    }
}