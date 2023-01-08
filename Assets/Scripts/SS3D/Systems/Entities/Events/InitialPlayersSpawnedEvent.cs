using System.Collections.Generic;
using Coimbra.Services.Events;

namespace SS3D.Systems.Entities.Events
{
    public partial struct InitialPlayersSpawnedEvent : IEvent
    {
        public readonly List<Entity> Players;

        public InitialPlayersSpawnedEvent(List<Entity> players)
        {
            Players = players;
        }
    }
}