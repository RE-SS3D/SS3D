namespace SS3D.Systems.Area
{
    public sealed class AreaRecord
    {
        public AreaId Id;

        public string DisplayName;

        public string ParentTag;

        public IAreaApcOrigin Apc;

        public string AmbienceTrackId;

        // Future: NormalFixtures[], EmergencyFixtures[]
    }
}
