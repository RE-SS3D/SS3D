using System.Collections.Generic;
using Coimbra.Services.Events;

namespace SS3D.Systems.Entities.Events
{
    public partial struct SpawnedPlayersUpdated : IEvent
    {
        public readonly List<string> SpawnedPlayers;

        public SpawnedPlayersUpdated(List<string> spawnedPlayers)
        {
            SpawnedPlayers = spawnedPlayers;
        }
    }
}