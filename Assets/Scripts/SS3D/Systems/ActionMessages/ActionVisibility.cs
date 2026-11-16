namespace SS3D.Systems.ActionMessages
{
    /// <summary>Who perceives a narrated action (action-messages.md §4). There is no silent level.</summary>
    public enum ActionVisibility : byte
    {
        /// <summary>Anyone with line of sight, within hearing range.</summary>
        Public = 0,

        /// <summary>Only those within a tile or two.</summary>
        Close = 1,

        /// <summary>Only the person it is done to.</summary>
        TargetOnly = 2,
    }
}
