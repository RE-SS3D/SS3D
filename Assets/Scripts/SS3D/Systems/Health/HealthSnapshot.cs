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
        public int SeveredZoneMask;
        /// <summary>
        /// Quantized per-zone bleed rates: 3 bits each, code = round(rate * 2) so 0/0.5/1/1.5/2 map cleanly.
        /// </summary>
        public int BleedingRatePacked;
        public float TotalBleedingRate;
        public float BrainFunctionPercent;
        public float HeartFunctionPercent;
        public float MovementSpeedMultiplier;
        public bool CanUseArms;
        public HealthCriticalFlags CriticalFlags;
        public bool CanDefibrillate;

        public bool IsZoneBleeding(BodyZone zone)
        {
            return (BleedingZoneMask & (1 << (int)zone)) != 0;
        }

        public bool IsZoneSevered(BodyZone zone)
        {
            return (SeveredZoneMask & (1 << (int)zone)) != 0;
        }

        public float GetZoneBleedingRate(BodyZone zone)
        {
            int index = (int)zone;
            if (index < 0 || index >= HealthConstants.ZoneCount)
            {
                return 0f;
            }

            int code = (BleedingRatePacked >> (index * 3)) & 0x7;
            return code * 0.5f;
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
            SeveredZoneMask = 0,
            BleedingRatePacked = 0,
            TotalBleedingRate = 0f,
            BrainFunctionPercent = 100f,
            HeartFunctionPercent = 100f,
            MovementSpeedMultiplier = 1f,
            CanUseArms = true,
            CriticalFlags = HealthCriticalFlags.None,
            CanDefibrillate = false,
        };
    }
}
