using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Roles;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SS3D.Systems.Spawners
{
    /// <summary>
    /// This system handles spawning players on their correct SpawnPoints.
    /// It registers all the alive SpawnPoints on the game, and it randomly picks valid ones.
    /// 
    /// It's important to also note that this system just overwrites the predefined location that EntitySubSystem uses in SpawnPlayer method.
    /// </summary>
    public class SpawnPointManager : NetworkSubSystem
    {
        public Action RequestSpawnPoints;
        
        /// <summary>
        /// The SpawnPoints that exist on the current map.
        /// </summary>
        private List<SpawnPoint> _spawnPoints = new();

        /// <summary>
        /// Max tries before we give up when no unnocupied spawn points exist, and nothing is valid
        /// </summary>
        private int _maxTries = 50;

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
        /// <param name="player"></param> The player we want to handle spawning for
        /// <param name="isLateJoin"></param> Whether the entity is late-joining or not
        [Server]
        public SpawnPoint HandleSpawning(Player player, bool isLateJoin)
        {
            RoleSubSystem roleSubSystem = SubSystems.Get<RoleSubSystem>();
            List<SpawnPoint> possibleSpawnPoints = new List<SpawnPoint>();
            RoleData roleData = roleSubSystem.GetRoleFromPlayer(player);

            // Iterate over spawn points to choose a valid one
            foreach (SpawnPoint spawnPoint in _spawnPoints)
            {
                if (IsSpawnPointOccupied(spawnPoint))
                {
                    Log.Warning(this, "SpawnPoint was occupied");
                    continue;
                }
                
                // The round is ongoing and the spawn point is a late-join
                if (isLateJoin && spawnPoint.SpawnPointData.SpawnType == SpawnType.LateJoin)
                {
                    possibleSpawnPoints.Add(spawnPoint);
                }
                
                // The round is not ongoing (in-lobby) and the spawn type is a job
                if (!isLateJoin 
                    && spawnPoint.SpawnPointData.SpawnType == SpawnType.Job
                    && ( roleData == spawnPoint.SpawnPointData.RoleData ) )
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
                    return null;
                }
                
                Log.Warning(this, $"Map does not have enough spawn points to handle the job: {roleData?.Name}.");
                
                // Usually the below block happens when a mapper hasn't mapped the correct amount of spawn points for ready players
                //
                // Check if there exists any unoccupied spawn points and spawn them there
                foreach (SpawnPoint spawnPoint in _spawnPoints)
                {
                    if (!IsSpawnPointOccupied(spawnPoint))
                    {
                        return spawnPoint;
                    }
                }

                // Nothing worked, spawning at first spawn point. Time for clipping!
                Log.Error(this, "No valid unoccupied spawn points were available on this map, spawning at first spawn point.");
                return _spawnPoints[0];
            }
            
            // Random selection of valid spawn points
            int randIndex = Random.Range(0, possibleSpawnPoints.Count);
            return possibleSpawnPoints[randIndex];
        }

        /// <summary>
        /// Synces the position of a networkobject with the spawn point
        /// </summary>
        /// <param name="networkObject"></param>The object to sync
        /// <param name="spawnPoint"></param>The spawn point to sync with
        [ObserversRpc]
        public void SyncObjectWithSpawnPoint(NetworkObject networkObject, SpawnPoint spawnPoint)
        {
            // TODO: find better solution...? why this works is beyond me and im tired of debugging this
            if (networkObject.TryGetComponent<CharacterController>(out CharacterController cc))
            {
                cc.enabled = false;
                networkObject.transform.position = spawnPoint.Position;
                cc.enabled = true;
            }
        }
        
        [Server]
        private bool IsSpawnPointOccupied(SpawnPoint spawnPoint)
        {
            float checkRadius = 0.5f;
            LayerMask characterLayer = LayerMask.GetMask("Characters");
    
            Collider[] hits = Physics.OverlapSphere(spawnPoint.Position, checkRadius, characterLayer);

            return hits.Length > 0;
        }
    }
}