namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Connection-row copy derived from real power state, replacing invented node/port ids.
    /// </summary>
    public static class MachineStatusLine
    {
        public const string GridFeedOk = "WIRED · POWER NET · GRID FEED";
        public const string NoGridFeed = "WIRED · POWER NET · NO GRID FEED";
        public const string Powered = "WIRED · POWER NET";
        public const string Unpowered = "NO POWER";

        /// <summary>APC / SMES: whether the grid is currently feeding the device.</summary>
        public static string GridFeed(bool hasFeed) => hasFeed ? GridFeedOk : NoGridFeed;

        /// <summary>Plain consumers: whether the device has power.</summary>
        public static string Power(bool powered) => powered ? Powered : Unpowered;
    }
}
