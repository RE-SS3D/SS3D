namespace SS3D.Systems.Area
{
    public sealed class AreaRecord
    {
        public AreaId Id;

        public string DisplayName;

        public string ParentTag;

        public IAreaApcOrigin Apc;

        /// <summary>Optional override of the department's ambience (area.md §5).</summary>
        public string AmbienceProfileId;

        /// <summary>The ambience this room asks for: its override, else its department. Empty means generic.</summary>
        public string EffectiveAmbienceProfileId => !string.IsNullOrEmpty(AmbienceProfileId)
            ? AmbienceProfileId
            : ParentTag?.ToLowerInvariant() ?? string.Empty;

        public bool HasDepartmentalLightTint;

        public UnityEngine.Color DepartmentalLightTint;

        /// <summary>
        /// Wall-switch preference: when false, area fixtures stay dark even if the lighting channel has power.
        /// </summary>
        public bool LightingSwitchOn = true;

        /// <summary>
        /// Default access requirement for doors opening into this area.
        /// </summary>
        public IdAccess.AccessMask DefaultRequiredAccess;

        // Future: NormalFixtures[], EmergencyFixtures[]
    }
}
