using Coimbra.Services.Events;
using SS3D.Systems.Entities;
using System.Collections.Generic;

namespace SS3D.Systems.Rounds.Events
{
    public partial struct ReadyPlayersChanged : IEvent
    {
        public readonly List<Player> ReadyPlayers;

        public ReadyPlayersChanged(List<Player> readyPlayers)
        {
            ReadyPlayers = readyPlayers;
        }
    }
}
