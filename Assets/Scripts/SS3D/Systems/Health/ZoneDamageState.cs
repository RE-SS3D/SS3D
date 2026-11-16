using System;

namespace SS3D.Systems.Health
{
    [Serializable]
    public struct ZoneDamageState
    {
        public float Brute;
        public float Burn;
        public WoundSeverity Severity;
        public float BleedingRate;
        public bool IsDisabled;
        public bool IsSplinted;
        public bool IsSevered;

        public static ZoneDamageState Default => new()
        {
            Brute = 0f,
            Burn = 0f,
            Severity = WoundSeverity.None,
            BleedingRate = 0f,
            IsDisabled = false,
            IsSplinted = false,
            IsSevered = false,
        };
    }
}
