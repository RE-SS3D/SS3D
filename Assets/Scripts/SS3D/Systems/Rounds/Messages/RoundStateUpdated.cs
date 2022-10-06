using Coimbra.Services.Events;
using FishNet.Broadcast;

namespace SS3D.Systems.Rounds.Messages
{
    public partial struct RoundStateUpdated : IEvent
    {
        public readonly RoundState RoundState;

        public RoundStateUpdated(RoundState roundState)
        {
            RoundState = roundState;
        }
    }
}