using Coimbra.Services.Events;
using FishNet.Component.Transforming;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Roles;
using System.Collections.Generic;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Tile;

namespace SS3D.Systems.Spawners
{
    /// <summary>
    /// A SpawnPoint is a prefab that is used for deciding the spawn locations of the players.
    /// </summary>
    public class SpawnPoint : NetworkActor
    {
        /// <summary>
        /// The data needed to validate the spawning conditions.
        /// For example, checking whether the player can spawn on this SpawnPoint based on their job (e.g. an assistant can't spawn on security SpawnPoint)
        /// </summary>
        public SpawnPointData SpawnPointData;

        public override void OnStartServer()
        {
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateChanged));
        }

        public void HandleRoundStateChanged(ref EventContext context, in RoundStateUpdated state)
        {
            // Prepare the SpawnPoint on the current map
            TileSubSystem tileSubSystem = SubSystems.Get<TileSubSystem>();
            if (state.RoundState == RoundState.WarmingUp && tileSubSystem.CurrentMap)
            {
                SubSystems.Get<SpawnPointManager>().RegisterSpawnPoint(this);
            }
        }

        /// <summary>
        /// Spawns a player on this SpawnPoint based on whether they have the correct RoleData
        /// </summary>
        /// <param name="entity"></param>
        [Server]
        public void SpawnPlayerOnPoint(Entity entity)
        {
            // We do not care about having the correct job in late-join spawn points
            if (SpawnPointData.SpawnType == SpawnType.LateJoin)
            {
                entity.Position = Position;

                return;
            }
            
            Player player = entity.Mind.player;
            KeyValuePair<Player, RoleData>? rolePair = SubSystems.Get<RoleSubSystem>().GetRoleFromPlayer(player);

            // This player does not have the correct Role for this SpawnPoint, so we don't spawn them
            if (rolePair is not { } role || role.Value != SpawnPointData.RoleData)
            {
                return;
            }
            
            entity.Position = Position;
        }
    }
}