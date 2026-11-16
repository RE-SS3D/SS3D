using FishNet.Broadcast;
using System;

namespace SS3D.Systems.Comms
{
    /// <summary>
    /// Non-positional comms line (radio / announcement). Plain text — presentation formats from
    /// channel id + sender on each client.
    /// </summary>
    [Serializable]
    public struct CommsMessage : IBroadcast
    {
        public string ChannelId;
        public string Sender;
        public string Text;
        public CommsChannelKind Kind;

        /// <summary>
        /// Optional follow-up <c>AssetDatabases.Sounds</c> id started with the announcement banner
        /// after <see cref="CommsAudioTrackIds.StationAnnounce"/> finishes. Empty = chime then
        /// banner only (no second clip). On a private feed line it plays once as the line appears.
        /// </summary>
        public string SoundId;
    }
}
