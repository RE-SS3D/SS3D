using FishNet.Broadcast;
using System;

namespace SS3D.Systems.Comms
{
    /// <summary>
    /// A single speech line broadcast from the server to observers of the speaking Entity's
    /// NetworkObject. Deliberately raw/unformatted so each client can independently decide how to
    /// render it — full text, garbled, styled differently for a shout, etc. — based on its own
    /// viewer-relative audibility tier.
    ///
    /// There is no speaker identifier on this struct: it's always delivered via an ObserversRpc
    /// declared on the speaking Entity's own LocalSpeechEmitter, so the receiving NetworkObject
    /// instance already *is* the speaker - no separate lookup is needed.
    /// </summary>
    [Serializable]
    public struct SpeechEvent : IBroadcast
    {
        public string Text;
        public SpeechMode Mode;
        public float ServerTimestamp;
    }
}
