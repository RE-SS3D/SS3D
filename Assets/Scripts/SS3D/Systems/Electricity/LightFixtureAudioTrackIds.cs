namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// <c>AssetDatabases.Sounds</c> clip ids for light fixtures. The small glass breaks, not the window shatters,
    /// so a bulb doesn't sound like a pane going.
    /// </summary>
    public static class LightFixtureAudioTrackIds
    {
        public static readonly string[] BulbBreak =
        {
            "28cb8f97272843269110c1bc4b819d6e", // GlassBreak1
            "88ff826ca50044918696e4b6327515e0", // GlassBreak2
            "45637e6ca20c48af93c1ae2792d5307b", // GlassBreak3
            "2bb5d8565d0143088aced9013024f9c1", // GlassBreak4
        };

        /// <summary>A bulb seated in or pulled from its socket, a higher-pitched light switch click.</summary>
        public const string Socket = Area.LightSwitchAudioTrackIds.Click;
    }
}
