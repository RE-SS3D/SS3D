using System.Collections.Generic;
using Coimbra.Services.Events;
using FishNet.Broadcast;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Entities;

namespace SS3D.Systems.Rounds.Events
{
    public partial struct ReadyPlayersChanged : IBroadcast
    {
        public readonly List<string> ReadyPlayers;

        public ReadyPlayersChanged(List<string>readyPlayers)
        {
            ReadyPlayers = readyPlayers;
        }
    }
}