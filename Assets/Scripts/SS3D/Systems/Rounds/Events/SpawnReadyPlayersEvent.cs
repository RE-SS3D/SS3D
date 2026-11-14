using System.Collections.Generic;
using Coimbra.Services.Events;
using SS3D.Systems.Entities;

namespace SS3D.Systems.Rounds.Events
{
    public partial struct SpawnReadyPlayersEvent : IEvent
    {
        public readonly List<Player> ReadyPlayers;

        public SpawnReadyPlayersEvent(List<Player> readyPlayers)
        {
            ReadyPlayers = readyPlayers;
        }
    }
}