using FishNet.Broadcast;
using SS3D.Systems.Entities;

namespace SS3D.Systems.Rounds.Messages
{
    public struct RequestEmbarkMessage : IBroadcast
    {
        public readonly Soul Soul;

        public RequestEmbarkMessage(Soul soul)
        {
            Soul = soul;
        }
    }
}