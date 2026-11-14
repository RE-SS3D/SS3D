namespace SS3D.Systems.Examine
{
    /// <summary>
    /// The Syndicate bomb's state lines (explosives-destruction.md §6): timer, countdown, bolts and panel.
    /// Read by <c>SyndicateBomb.AppendSections</c>.
    /// </summary>
    public static class ExamineSyndicateBombKeys
    {
        public const string Safe = "examine.syndicate_bomb.safe";
        public const string SafeFallback = "It is <k=good>not armed</k>. The timer is set to {0}.";
        public const string Armed = "examine.syndicate_bomb.armed";
        public const string ArmedFallback = "<k=danger>Armed</k>: {0} left.";
        public const string Bolted = "examine.syndicate_bomb.bolted";
        public const string BoltedFallback = "It is <k=warn>bolted to the floor</k>.";
        public const string Loose = "examine.syndicate_bomb.loose";
        public const string LooseFallback = "It is not bolted down.";
        public const string PanelOpen = "examine.syndicate_bomb.panel_open";
        public const string PanelOpenFallback = "Its wiring panel is <k=warn>open</k>.";

        public static readonly (string Key, string English)[] All =
        {
            (Safe, SafeFallback), (Armed, ArmedFallback), (Bolted, BoltedFallback), (Loose, LooseFallback),
            (PanelOpen, PanelOpenFallback),
        };
    }
}
