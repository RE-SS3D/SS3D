namespace SS3D.Systems.ActionMessages
{
    /// <summary>Action message distances (balancing, not design: examine.md §11, action-messages.md §4).</summary>
    public static class ActionMessageConfig
    {
        /// <summary>Public lines reach this far; matches local speech's inaudible distance (comms.md §3).</summary>
        public const float PublicRange = 10f;

        /// <summary>"Within a tile or two": Close lines, and how near an examiner must stand to be noticed.</summary>
        public const float CloseRange = 2.5f;

        /// <summary>The examine cue's range (examine.md §4: within a couple of tiles).</summary>
        public const float CueRange = CloseRange;

        /// <summary>One "looks you over" per examiner and target this often.</summary>
        public const float CueCooldownSeconds = 5f;
    }
}
