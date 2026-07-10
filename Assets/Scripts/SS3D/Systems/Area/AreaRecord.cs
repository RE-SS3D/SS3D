namespace SS3D.Systems.Area
{
    public sealed class AreaRecord
    {
        public AreaId Id;

        public string DisplayName;

        public string ParentTag;

        public IAreaApcOrigin Apc;

        public string AmbienceTrackId;

        public bool HasDepartmentalLightTint;

        public UnityEngine.Color DepartmentalLightTint;

        /// <summary>
        /// Wall-switch preference: when false, area fixtures stay dark even if the lighting channel has power.
        /// </summary>
        public bool LightingSwitchOn = true;

        // Future: NormalFixtures[], EmergencyFixtures[]
    }
}
