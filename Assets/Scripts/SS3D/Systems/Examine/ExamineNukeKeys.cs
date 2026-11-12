namespace SS3D.Systems.Examine
{
    /// <summary>
    /// The nuclear device's state lines, readable at any range (antagonist-content.md §6: disk loaded, armed,
    /// time left). Read by <c>Nuke.AppendSections</c>.
    /// </summary>
    public static class ExamineNukeKeys
    {
        public const string DiskEmpty = "examine.nuke.disk_empty";
        public const string DiskEmptyFallback = "The disk slot is <k=good>empty</k>.";
        public const string DiskLoaded = "examine.nuke.disk_loaded";
        public const string DiskLoadedFallback = "An authentication disk is <k=warn>loaded</k>.";
        public const string Safe = "examine.nuke.safe";
        public const string SafeFallback = "It is <k=good>not armed</k>.";
        public const string Armed = "examine.nuke.armed";
        public const string ArmedFallback = "<k=danger>Armed</k>: {0} left.";
        public const string PanelOpen = "examine.nuke.panel_open";
        public const string PanelOpenFallback = "Its wiring panel is <k=warn>open</k>.";

        public static readonly (string Key, string English)[] All =
        {
            (DiskEmpty, DiskEmptyFallback), (DiskLoaded, DiskLoadedFallback),
            (Safe, SafeFallback), (Armed, ArmedFallback), (PanelOpen, PanelOpenFallback),
        };
    }
}
