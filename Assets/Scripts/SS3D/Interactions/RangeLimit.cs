using System;
using UnityEngine;

namespace SS3D.Interactions
{
    /// <summary>
    /// Defines a range limit check for interactions
    /// </summary>
    [Serializable]
    public struct RangeLimit
    {
        public static readonly RangeLimit Max = new()
        {
            Horizontal = float.MaxValue, Vertical = float.MaxValue,
        };

        public float Horizontal;
        public float Vertical;

        public RangeLimit(float horizontal, float vertical)
        {
            Horizontal = horizontal;
            Vertical = vertical;
        }

        /// <summary>
        /// Checks if an interaction is within range
        /// </summary>
        /// <param name="origin">The origin of the interaction</param>
        /// <param name="target">The target object that creates the interaction</param>
        public bool IsInRange(Vector3 origin, Vector3 target)
        {
            bool isInVerticalRange = Mathf.Abs(target.y - origin.y) < Vertical;
            Vector2 vector2 = new Vector2(target.x, target.z) - new Vector2(origin.x, origin.z);
            bool isInHorizontalRange = vector2.sqrMagnitude < Horizontal * Horizontal;

            return isInVerticalRange && isInHorizontalRange;
        }
    }
}