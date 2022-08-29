using FishNet.Broadcast;

namespace SS3D.Systems.Rounds.Messages
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