using SS3D.Systems.Health;

namespace SS3D.UI.MachineInterface
{
    /// <summary>Per-zone flags a scan reads off <see cref="ZoneDamageState"/>.</summary>
    [System.Flags]
    public enum HealthScanZoneFlags : byte
    {
        None = 0,
        Bleeding = 1 << 0,
        Disabled = 1 << 1,
        Splinted = 1 << 2,
        Severed = 1 << 3,
    }

    /// <summary>
    /// One live scan of a character (virology.md §5): real values read off the patient's health
    /// state at the moment of the scan. Zones are indexed by <see cref="BodyZone"/>, organs by
    /// <see cref="OrganType"/>.
    /// </summary>
    public struct HealthScanSnapshot
    {
        public int MachineObjectId;

        public string InterfaceId;

        public string ModelLabel;

        public string DeviceTitle;

        public string SubjectName;

        public HealthState State;

        public bool IsConscious;

        public bool IsCardiacArrest;

        public HealthCriticalFlags CriticalFlags;

        /// <summary>0–1, 1 is a full blood volume.</summary>
        public float BloodVolumeRatio;

        /// <summary>0–1 oxygen debt; saturation reads as its complement.</summary>
        public float OxyDebt;

        /// <summary>0–1 toxin concentration.</summary>
        public float Toxin;

        /// <summary>Whole-body shock, 0–100+.</summary>
        public float Shock;

        /// <summary>Pain from recent hits that has not become shock yet.</summary>
        public float IncomingPain;

        /// <summary>Summed bleed rate of every zone (the simulation's per-tick units).</summary>
        public float TotalBleedingRate;

        public WoundSeverity[] ZoneSeverity;

        public float[] ZoneBrute;

        public float[] ZoneBurn;

        public HealthScanZoneFlags[] ZoneFlags;

        /// <summary>Function percent per organ, 0–100.</summary>
        public float[] OrganFunction;
    }
}
