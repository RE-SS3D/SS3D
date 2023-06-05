using System.Collections.Generic;
using Coimbra.Services.Events;
using SS3D.Systems.Entities;

namespace SS3D.Systems.PlayerControl.Events
{
    public partial struct ServerPlayersChanged : IEvent
    {
        public readonly List<Player> ServerPlayers;

        public ChangeType ChangeType;
        public readonly Player Changed;

        public ServerPlayersChanged(List<Player> serverPlayers, ChangeType changeType, Player changed)
        {
            ServerPlayers = serverPlayers;
            ChangeType = changeType;
            Changed = changed;
        }
    }
}