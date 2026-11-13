using System.Collections.Generic;
using Coimbra.Services.Events;
using SS3D.Systems.Entities;

namespace SS3D.Systems.Rounds.Events
{
    public partial struct SpawnReadyPlayersEvent : IEvent
    {
        public readonly List<Player> ReadyPlayers;

        /// <summary>Players who chose Observe; they enter the round as ghosts (<c>observer.md</c> §2).</summary>
        public readonly List<Player> Observers;

        public SpawnReadyPlayersEvent(List<Player> readyPlayers, List<Player> observers = null)
        {
            ReadyPlayers = readyPlayers;
            Observers = observers ?? new List<Player>();
        }
    }
}