using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace SS3D.Systems.Spawners
{
    /// <summary>
    /// This system handles spawning players on their correct SpawnPoints. It does not account for RoleData.
    /// It registers all the alive SpawnPoints on the game, and it randomly picks valid ones.
    /// 
    /// It's important to also note that this system just overwrites the predefined location that EntitySubSystem uses in SpawnPlayer method.
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
        
        /// <summary>
        /// Decides the SpawnPoints of an entity (Player) based on SpawnTypes (late-join, job) and tries to pick a valid location.
        /// If there's no valid locations, it spawns the player at (0, 0).
        /// </summary>
        /// <param name="entity"></param> The entity we want to handle spawning for
        /// <param name="isLateJoin"></param> Whether the entity is late-joining or not
        [Server]
        public void HandleSpawning(Entity entity, bool isLateJoin)
        {
            List<SpawnPoint> possibleSpawnPoints = new List<SpawnPoint>();

            // Iterate over spawn points to choose a valid one
            foreach (SpawnPoint spawnPoint in _spawnPoints)
            {
                // The round is ongoing and the spawn point is a late-join
                if (isLateJoin && spawnPoint.SpawnPointData.SpawnType == SpawnType.LateJoin)
                {
                    possibleSpawnPoints.Add(spawnPoint);
                }
                
                // The round is not ongoing (in-lobby) and the spawn type is a job
                if (!isLateJoin && spawnPoint.SpawnPointData.SpawnType == SpawnType.Job)
                {
                    possibleSpawnPoints.Add(spawnPoint);
                }
            }

            // If no valid spawn points exist, either pick the default location of EntitySubSystem, or the first spawn point in our _spawnPoints list
            if (possibleSpawnPoints.Count == 0)
            {
                // Spawn at default location since our _spawnPoints is empty
                if (_spawnPoints.Count == 0)
                {
                    Log.Error(this, "No spawn points were available on this map. Spawning at default location");
                    return;
                }
                
                // Pick first spawn point from _spawnPoints as fallback 
                possibleSpawnPoints.Add(_spawnPoints[0]);
                Log.Error(this, "No valid spawn points were available on this map, spawning at random spawn point.");
            }
            
            // Random selection of valid spawn points
            int randIndex = Random.Range(0, possibleSpawnPoints.Count);
            SpawnPoint spawnLocation = possibleSpawnPoints[randIndex];

            // Actually try to spawn the player here
            spawnLocation.SpawnPlayerOnPoint(entity);
        }
    }
}