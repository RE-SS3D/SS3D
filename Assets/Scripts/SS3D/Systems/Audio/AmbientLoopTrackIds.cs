namespace SS3D.Systems.Audio
{
    /// <summary>The machine sounds <see cref="AmbientLoopSource"/> can play.</summary>
    public enum AmbientLoopKind : byte
    {
        Vent = 0,
        Scrubber = 1,
        GasPump = 2,
        Vendor = 3,
        Apc = 4,
        Smes = 5,
        AirAlarm = 6,
    }

    /// <summary>How one kind of machine sounds.</summary>
    public readonly struct AmbientLoopSettings
    {
        public readonly string TrackId;
        public readonly float Volume;
        public readonly float MaxDistance;

        public AmbientLoopSettings(string trackId, float volume, float maxDistance)
        {
            TrackId = trackId;
            Volume = volume;
            MaxDistance = maxDistance;
        }
    }

    /// <summary>
    /// `AssetDatabases.Sounds` clip ids for machine loops (audio.md §3), and each kind's level and
    /// reach. Levels are first-pass values for audition.
    /// </summary>
    public static class AmbientLoopTrackIds
    {
        public const string GasVent = "6a4ac39d302a4fa4bcb108184f25581f";
        public const string Scrubber = "584ae5977d47442ab1b2b9a5e1ca543b";
        public const string GasPump = "01a9546d7ca743a9be934057b41a9062";
        public const string PowerHum = "dc98662fc0431164bb4cf043fc5a1951";
        public const string Machinery = "d343550d1c1c4dd28149bc530f85407f";
        public const string Alarm = "89185afdd1154c6aa893b8f814af572c";

        public static AmbientLoopSettings Get(AmbientLoopKind kind) => kind switch
        {
            AmbientLoopKind.Vent => new AmbientLoopSettings(GasVent, 0.35f, 7f),
            AmbientLoopKind.Scrubber => new AmbientLoopSettings(Scrubber, 0.3f, 7f),
            AmbientLoopKind.GasPump => new AmbientLoopSettings(GasPump, 0.4f, 9f),
            AmbientLoopKind.Vendor => new AmbientLoopSettings(PowerHum, 0.2f, 5f),
            AmbientLoopKind.Apc => new AmbientLoopSettings(PowerHum, 0.12f, 4f),
            AmbientLoopKind.Smes => new AmbientLoopSettings(Machinery, 0.35f, 9f),
            AmbientLoopKind.AirAlarm => new AmbientLoopSettings(Alarm, 0.5f, 14f),
            _ => default,
        };
    }
}
