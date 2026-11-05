namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Fixed (non-content-authored) <c>AssetDatabases.Sounds</c> clip ids for personal audio (audio.md §4).
    /// Unlike ambience tracks (content, authored per-area), these are architecturally fixed cues.
    /// Ids are the Unity audio asset GUIDs (same pattern as Comms / Footsteps).
    /// </summary>
    public static class AudioTrackIds
    {
        /// <summary>FreeSound InspectorJ 485076 — critical heartbeat loop.</summary>
        public const string Heartbeat = "e18d13637373424e80cb345f0503b37b";

        /// <summary>FreeSound Under7dude 163383 — heavy breathing (stamina + health max-merge).</summary>
        public const string HeavyBreathing = "b17ea6aba01249a183507785b28ca73c";
        /// <summary>FreeSound ckvoiceover 401336 — calm loop, heard on internals (armor.md §6) and in a suit with no air around.</summary>
        public const string Breathing = "d6487a9829df4ceaa6afc34160b91d3c";

        /// <summary>World/Collisions/MetalHit1.wav, pitched down, for the magboot toggle (armor.md §6).</summary>
        public const string MagbootClunk = "3cd350866d2be174584b9acd88c39bc6";

        /// <summary>SS14 Effects/beep1.ogg — new alert-stack chip ding.</summary>
        public const string AlertCue = "9c25cbf72a814b7daecb30325916da0a";
    }
}
