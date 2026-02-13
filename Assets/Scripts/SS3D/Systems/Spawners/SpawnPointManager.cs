using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Roles;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Spawners
{
    /// <summary>
    /// This system handles spawning players on their correct SpawnPoints.
    /// It registers all the alive SpawnPoints on the game, and it randomly picks valid ones.
    /// 
    /// It's important to also note that the Spawn Points rely on the mapper.
    /// The mapper should:
    /// 1. Map the exact spawn points required for each role (e.g. if map has 5 sec roles, map 5 sec spawners)
    /// 2. Map enough late-join spawners. This depends on the map.
    /// For example, if a map is low-pop and holds 30 players then you should map; 30 minus spawn points for ready players.
    ///
    /// Fallback will happen if the every player stands on a late-join spawn point (very unlikely)
    ///
    /// Spawn points for ready players (Job Spawners) get reserved once someone spawns on them. Late-join spawners don't get reserved.
    ///
    /// TODO: find a better way to handle late-join spawners because compared to ss13/ss14, in ss3d player-collision is prominent
    /// TODO: Implement Observer spawners once they get implemented
    /// TODO: Implement Cryo late-join spawners once they get implemented
    /// </summary>
    public class SpawnPointManager : NetworkSubSystem
    {
        /// <summary>
        /// Holds spawn points that have RoleData in them.
        /// </summary>
        private readonly Dictionary<RoleData, List<SpawnPoint>> _spawnPoints = new();
        
        /// <summary>
        /// Holds late-join spawn points
        /// </summary>
        private readonly List<SpawnPoint> _lateJoinSpawnPoints = new();
        
        /// <summary>
        /// Holds all spawn points
        /// </summary>
        private readonly List<SpawnPoint> _allSpawnPoints = new();

        /// <summary>
        /// RoleSubSystem needed for checking the role of the player
        /// </summary>
        private RoleSubSystem _roleSubSystem;

        /// <summary>
        /// Character layer needed for checking if a spawn point is occupied by another player, physics-wise
        /// </summary>
        private LayerMask _characterLayer;

        protected override void OnStart()
        {
            _roleSubSystem = SubSystems.Get<RoleSubSystem>();
            _characterLayer = LayerMask.GetMask("Characters");
            
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateChanged));
        }

        public void HandleRoundStateChanged(ref EventContext context, in RoundStateUpdated state)
        {
            // Clear all spawn points for new round
            if (state.RoundState == RoundState.Preparing)
            {
                foreach (SpawnPoint point in _allSpawnPoints)
                {
                    // Unreserve them on the prefab script so we can spawn on them next round
                    point.Reserved = false;
                }
                
                _spawnPoints.Clear();
                _allSpawnPoints.Clear();
                _lateJoinSpawnPoints.Clear();
                
                Log.Information(this, "Cleared all spawn points for new round.");
            }
        }

        public void RegisterSpawnPoint(SpawnPoint spawnPoint)
        {
            // Register the spawn point to all spawn points
            _allSpawnPoints.Add(spawnPoint);
            
            // Register the spawn point to late-join spawners, if it's a latejoin
            if (spawnPoint.SpawnPointData.SpawnType == SpawnType.LateJoin)
            {
                _lateJoinSpawnPoints.Add(spawnPoint);

                return;
            }

            // Register the spawn point based on role
            RoleData role = spawnPoint.SpawnPointData.RoleData;
            if (role)
            {
                if (!_spawnPoints.TryGetValue(role, out List<SpawnPoint> points))
                {
                    points = new List<SpawnPoint>();
                    _spawnPoints[role] = points;
                }
                
                points.Add(spawnPoint);
            }
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
            RoleData roleData = _roleSubSystem.GetRoleFromPlayer(player);
            if (!roleData)
            {
                Log.Error(this, $"Player {player} had no role set.");
                return null;
            }

            // User is late-joining, get a late-join spawner and try to spawn them on it
            if (isLateJoin)
            {
                SpawnPoint spawnPoint = GetValidSpawnPoint(_lateJoinSpawnPoints);

                if (spawnPoint)
                {
                    return spawnPoint;
                }
            }
            else
            {
                // This is a player who pressed "Ready", so get the spawn points that corresponds to the correct role
                if (_spawnPoints.TryGetValue(roleData, out List<SpawnPoint> spawnPoints))
                {
                    SpawnPoint spawnPoint = GetValidSpawnPoint(spawnPoints);
                    if (spawnPoint)
                    {
                        spawnPoint.Reserve();
                        return spawnPoint;
                    }
                }
            }

            // Nothing worked, get a fallback spawner
            SpawnPoint fallback = GetValidSpawnPoint(_allSpawnPoints);
            if (fallback)
            {
                if (fallback.SpawnPointData.SpawnType == SpawnType.LateJoin)
                {
                    return fallback;
                }
                
                fallback.Reserve();
                return fallback;
            }

            // There's no spawn points at all
            if (_allSpawnPoints.Count == 0)
            {
                Log.Error(this, "No spawn points exist on this map");
                return null;
            }

            // Well, this will likely result into clipping so have fun...
            Log.Error(this, $"There were no valid spawn points for {roleData.name}, spawning at first spawn point");
            return _allSpawnPoints[0];
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
        
        /// <summary>
        /// Gets a valid spawn point that isn't occupied
        /// </summary>
        /// <param name="spawnPoints"></param> The spawn points to check
        /// <returns></returns>
        private SpawnPoint GetValidSpawnPoint(List<SpawnPoint> spawnPoints)
        {
            if (spawnPoints.Count == 0)
            {
                return null;
            }
            
            foreach (SpawnPoint point in spawnPoints)
            {
                // Try to skip physics check for job spawners, since they rely on Reserved boolean
                if (point.SpawnPointData.SpawnType == SpawnType.Job && !point.Reserved)
                {
                    return point;
                }
                
                if (!IsSpawnPointOccupied(point))
                {
                    return point;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Checks whether a spawn point is occupied or not.
        /// First checks if its reserved (applies only for Spawn Points that have SpawnType set to Job)
        /// Then, if they're not reserved, checks if you can spawn on it
        /// </summary>
        /// <param name="spawnPoint"></param>
        /// <returns></returns>
        private bool IsSpawnPointOccupied(SpawnPoint spawnPoint)
        {
            if (spawnPoint.Reserved)
            {
                return true;
            }
            
            float checkRadius = 0.4f;
            return Physics.CheckSphere(spawnPoint.Position, checkRadius, _characterLayer);
        }
    }
}