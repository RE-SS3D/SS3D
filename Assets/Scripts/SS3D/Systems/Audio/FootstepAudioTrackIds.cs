namespace SS3D.Systems.Audio
{
    /// <summary>
    /// `AssetDatabases.Sounds` clip ids for footwear footsteps (audio.md §3 — ordinary positional
    /// SFX). Clips live under <c>Assets/Art/Sound/Entities/Humanoid/Footsteps/</c>.
    /// </summary>
    public static class FootstepAudioTrackIds
    {
        /// <summary>Barefoot / socks — no shoes equipped.</summary>
        public const string Socks = "1342f00d3f3444ba9d135ac709a632b4";

        /// <summary>Soft shoes (e.g. ShoeHiTops).</summary>
        public const string Shoes = "762218e71c4b4521936af533b2ee6998";

        /// <summary>Hard footwear (e.g. ShoeJackboots).</summary>
        public const string Boots = "52e21acb637e4160beb8747b8834cceb";
    }
}
