using System;

namespace SS3D.Systems.Health
{
    [Serializable]
    public struct HealthSnapshot
    {
        public HealthState State;
        public SystemicPools Pools;
        public float WorstZoneBrute;
        public float WorstZoneBurn;
        public bool IsBleeding;
        public bool IsConscious;
        public bool IsCardiacArrest;
        public int BleedingZoneMask;

        public bool IsZoneBleeding(BodyZone zone)
        {
            return (BleedingZoneMask & (1 << (int)zone)) != 0;
        }

        public static HealthSnapshot Default => new()
        {
            State = HealthState.Healthy,
            Pools = SystemicPools.Default,
            WorstZoneBrute = 0f,
            WorstZoneBurn = 0f,
            IsBleeding = false,
            IsConscious = true,
            IsCardiacArrest = false,
            BleedingZoneMask = 0,
        };
    }
}
