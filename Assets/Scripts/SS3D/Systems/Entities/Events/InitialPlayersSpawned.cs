using System.Collections.Generic;
using Coimbra.Services.Events;

namespace SS3D.Systems.Entities.Events
{
    public partial struct InitialPlayersSpawned : IEvent
    {
        public readonly List<Entity> Players;

        public InitialPlayersSpawned(List<Entity> players)
        {
            Players = players;
        }
    }
}