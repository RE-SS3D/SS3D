using System;
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

        public static readonly RangeLimit Max = new RangeLimit {horizontal = float.MaxValue, vertical = float.MaxValue};
    }
}