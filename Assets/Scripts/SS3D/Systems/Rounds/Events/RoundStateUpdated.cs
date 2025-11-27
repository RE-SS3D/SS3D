using Coimbra.Services.Events;

namespace SS3D.Systems.Rounds.Events
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
