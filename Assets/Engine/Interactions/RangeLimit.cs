using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Engine.Interactions
{
    [Serializable]
    public struct RangeLimit
    {
        public float horizontal;
        public float vertical;

        public RangeLimit(float horizontal, float vertical)
        {
            this.horizontal = horizontal;
            this.vertical = vertical;
        }

        public bool IsInRange(Vector3 origin, Vector3 target)
        {
            return Mathf.Abs(target.y - origin.y) < vertical &&
                   (new Vector2(target.x, target.z) - new Vector2(origin.x, origin.z)).sqrMagnitude <
                   horizontal * horizontal;
        }

        public static readonly RangeLimit Max = new RangeLimit {horizontal = float.MaxValue, vertical = float.MaxValue};
    }
}