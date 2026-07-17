using System;

namespace SS3D.Systems.Health
{
    [Serializable]
    public struct SystemicPools
    {
        public float BloodVolumeRatio;
        public float OxyDebt;
        public float ToxinConcentration;

        public static SystemicPools Default => new()
        {
            BloodVolumeRatio = 1f,
            OxyDebt = 0f,
            ToxinConcentration = 0f,
        };
    }
}
