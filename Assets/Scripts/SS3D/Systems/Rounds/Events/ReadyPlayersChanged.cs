using System.Collections.Generic;
using Coimbra.Services.Events;
using SS3D.Systems.Entities;

namespace SS3D.Systems.Rounds.Events
{
    public partial struct ReadyPlayersChanged : IEvent
    {
        public readonly List<Soul> ReadyPlayers;

        public ReadyPlayersChanged(List<Soul> readyPlayers)
        {
            ReadyPlayers = readyPlayers;
        }
    }
}