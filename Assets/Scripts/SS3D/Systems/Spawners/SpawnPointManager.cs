using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

namespace SS3D.Systems.Spawners
{
    /// <summary>
    /// This system handles spawning players on their correct SpawnPoints. It does not account for RoleData.
    /// It registers all the alive SpawnPoints on the game and it randomly picks valid ones.
    /// 
    /// It's important to also note that this system just overwrites the predefined location that EntitySubSystem uses.
    /// </summary>
    public class SpawnPointManager : NetworkSubSystem
    {
        /// <summary>
        /// The SpawnPoints that exist on the current map.
        /// </summary>
        private List<SpawnPoint> _spawnPoints = new();

        protected override void OnStart()
        {
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateChanged));
        }

        public void HandleRoundStateChanged(ref EventContext context, in RoundStateUpdated state)
        {
            if (state.RoundState == RoundState.Preparing)
            {
                _spawnPoints.Clear();
                Log.Information(this, "Cleared all spawn points for new round.");
            }
        }

        public void RegisterSpawnPoint(SpawnPoint spawnPoint)
        {
            _spawnPoints.Add(spawnPoint);
        }

        public void UnregisterSpawnPoint(SpawnPoint spawnPoint)
        {
            _spawnPoints.Remove(spawnPoint);
        }

        [Server]
        public void HandleSpawning(Entity entity)
        {
            List<SpawnPoint> possibleSpawnPoints = new List<SpawnPoint>();
            RoundSubSystemBase roundSystem = SubSystems.Get<RoundSubSystemBase>();

            // Iterate over spawn points to choose a valid one
            foreach (SpawnPoint spawnPoint in _spawnPoints)
            {
                // The round is ongoing and the spawn point is a job, therefore we spawn them on it
                if (roundSystem.RoundState == RoundState.Ongoing && spawnPoint.SpawnPointData.SpawnType == SpawnType.Job)
                {
                    possibleSpawnPoints.Add(spawnPoint);
                }
                
                // TODO: Handle late-joining here
            }

            // If no valid spawn points exist, either pick (0,0) or the first spawn point in the _spawnPoints list
            if (possibleSpawnPoints.Count == 0)
            {
                // Spawn at (0, 0) since _spawnPoints is empty
                if (_spawnPoints.Count == 0)
                {
                    Log.Error(this, "No spawn points were available on this map. Spawning at (0, 0)");
                    return;
                }
                
                // Pick first spawn point from _spawnPoints as fallback 
                possibleSpawnPoints.Add(_spawnPoints[0]);
                Log.Error(this, "No valid spawn points were available on this map, spawning at random spawn point.");
            }
            
            int randIndex = Random.Range(0, possibleSpawnPoints.Count);
            SpawnPoint spawnLocation = possibleSpawnPoints[randIndex];

            spawnLocation.SpawnPlayerOnPoint(entity);
        }
    }
}