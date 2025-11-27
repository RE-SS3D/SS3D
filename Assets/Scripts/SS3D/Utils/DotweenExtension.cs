//using DebugDrawingExtension;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace SS3D.Utils
{
    public static class DotweenExtension
    {
        /// <summary>
        /// Extends DOTween's SetLookAt to align a custom local axis with the direction of the path.
        /// </summary>
        /// <param name="tweener">The tweener handling the path movement.</param>
        /// <param name="lookAhead">The amount of look-ahead for calculating the direction on the path (same as SetLookAt).</param>
        /// <param name="customAxis">The custom axis to align with the path's direction (e.g., Vector3.up).</param>
        /// <returns></returns>
        public static Tweener SetLookAtWithCustomAxis(this Tweener tweener, float lookAhead, Vector3 customAxis)
        {
            return tweener.OnUpdate(() =>
            {
                tweener.ForceInit();
                // Get the position at the current look-ahead point on the path
                Vector3 currentPos = tweener.PathGetPoint(tweener.ElapsedPercentage());
                Vector3 nextPos = tweener.PathGetPoint(tweener.ElapsedPercentage() + lookAhead); // Small step forward

                // Calculate the direction to look at, based on the path's progression
                Vector3 direction = (nextPos - currentPos).normalized;

                DebugExtension.DebugArrow(currentPos, direction);

                if(direction == Vector3.zero) return;

                // Apply the rotation based on the custom axis
                Quaternion targetRotation = Quaternion.LookRotation(customAxis, direction);

                // Set the object's rotation to the calculated value
                Transform targetTransform = tweener.target as Transform;
                targetTransform.localRotation = targetRotation;
                
            });
        }
    }
}
