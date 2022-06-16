
using FishNet.Broadcast;
using SS3D.Core.Systems.Rounds;

namespace SS3D.Core.Rounds.Messages
{
    public struct RoundStateUpdatedMessage : IBroadcast
    {
        public readonly RoundState RoundState;

        public RoundStateUpdatedMessage(RoundState roundState)
        {
            RoundState = roundState;
        }
    }
}