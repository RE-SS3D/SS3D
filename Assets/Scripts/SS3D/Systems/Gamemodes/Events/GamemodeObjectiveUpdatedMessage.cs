using Coimbra.Services.Events;
using FishNet.Broadcast;
using SS3D.Systems.Gamemodes;

namespace SS3D.Systems.GameModes.Events
{
    public struct GamemodeObjectiveUpdatedMessage : IBroadcast
    {
        public readonly GamemodeObjective Objective;

        public GamemodeObjectiveUpdatedMessage(GamemodeObjective objective)
        {
            Objective = objective;
        }
    }
}
