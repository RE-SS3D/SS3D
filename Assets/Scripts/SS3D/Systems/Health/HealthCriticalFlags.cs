using System;

namespace SS3D.Systems.Health
{
    [Flags]
    public enum HealthCriticalFlags
    {
        None = 0,
        LowBlood = 1 << 0,
        HighOxyDebt = 1 << 1,
        HighToxin = 1 << 2,
        LowBrain = 1 << 3,
    }
}
