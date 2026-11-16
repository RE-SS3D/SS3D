using System;

namespace SS3D.Systems.Health
{
    [Serializable]
    public struct OrganState
    {
        public OrganType Type;
        public float FunctionPercent;
        public bool IsCritical;

        public static OrganState Default(OrganType type) => new()
        {
            Type = type,
            FunctionPercent = 100f,
            IsCritical = false,
        };
    }
}
