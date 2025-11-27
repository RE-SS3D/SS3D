using Coimbra.Services.Events;
using System.Collections.Generic;

namespace SS3D.Systems.Entities.Events
{
    public partial struct SpawnedPlayersUpdated : IEvent
    {
        public readonly List<Entity> SpawnedPlayers;

        public SpawnedPlayersUpdated(List<Entity> spawnedPlayers)
        {
            SpawnedPlayers = spawnedPlayers;
        }
    }
}
