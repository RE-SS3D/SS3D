using FishNet.Object;
using Serilog;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Roles;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Spawners
{
    public class SpawnPoint : NetworkActor
    {
        public SpawnPointData SpawnPointData;
        
        public void Awake()
        {
            SubSystems.Get<EntitySubSystem>().OnServerSpawn += ServerSpawn;
        }

        public void OnDestroy()
        {
            SubSystems.Get<EntitySubSystem>().OnServerSpawn -= ServerSpawn;
        }

        [Server]
        private void ServerSpawn(Entity entity)
        {
            RoleSubSystem roleSystem = SubSystems.Get<RoleSubSystem>();
            var roles = roleSystem.GetRolePlayers();
            Player player = entity.Mind.player;
            KeyValuePair<Player, RoleData>? rolePair = roleSystem.GetRoleFromPlayer(player);
            
            // We must match the RoleData of this SpawnPoint in order to spawn on it
            if (rolePair is not { } role || role.Value != SpawnPointData.RoleData)
            {
                return;
            }
            
            entity.Position = Position;
            
            Log.Information("Spawned player on our spawn point tile!");
        }
    }
}