using System;
using UnityEngine;

namespace SS3D.Interactions
{
    [Serializable]
    public struct RangeLimit
    {
        public float Horizontal;
        public float Vertical;

        public RangeLimit(float horizontal, float vertical)
        {
            Horizontal = horizontal;
            Vertical = vertical;
        }

        public bool IsInRange(Vector3 origin, Vector3 target)
        {
            bool isInVerticalRange = Mathf.Abs(target.y - origin.y) < Vertical;
            Vector2 vector2 = (new Vector2(target.x, target.z) - new Vector2(origin.x, origin.z));
            bool isInHorizontalRange = vector2.sqrMagnitude < Horizontal * Horizontal;

            return isInVerticalRange && isInHorizontalRange;
        }

        public static readonly RangeLimit Max = new() {Horizontal = float.MaxValue, Vertical = float.MaxValue};
    }
}