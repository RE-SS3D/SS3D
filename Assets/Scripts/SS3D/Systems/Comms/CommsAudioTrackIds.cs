namespace SS3D.Systems.Comms
{
    /// <summary>
    /// <see cref="SS3D.Data.Generated.AssetDatabases.Sounds"/> clip ids for non-diegetic comms cues.
    /// </summary>
    public static class CommsAudioTrackIds
    {
        /// <summary>
        /// Generic station announcement chime (SS14/tgstation announce.ogg) — always played
        /// before the banner for every announcement.
        /// </summary>
        public const string StationAnnounce = "38ecc6789330457e90285c4a09543c0a";

        /// <summary>
        /// Round-start welcome clip (SS14/tgstation welcome.ogg) — follow-up started with the
        /// banner after <see cref="StationAnnounce"/> ends.
        /// </summary>
        public const string StationWelcome = "352365062a1a4d5b855363d4c8d5f660";
    }
}
