using System.Collections.Generic;
using Coimbra.Services.Events;

namespace SS3D.Systems.Rounds.Events
{
    public partial struct SpawnReadyPlayersEvent : IEvent
    {
        public readonly List<string> ReadyPlayers;

        public SpawnReadyPlayersEvent(List<string>readyPlayers)
        {
            ReadyPlayers = readyPlayers;
        }
    }
}