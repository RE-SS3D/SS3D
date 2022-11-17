using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra.Services.Events;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Logging;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Entities.Messages;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Rounds.Messages;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Controls player spawning.
    /// </summary>
    public class EntitySpawnSystem : NetworkedSystem
    {
        [Header("Settings")]
        [SerializeField] private Transform _tempSpawnPoint;
        
        [SyncObject] private readonly SyncList<string> _spawnedPlayers = new();

        private readonly List<Entity> _serverSpawnedPlayers = new();
        private bool _alreadySpawnedInitialPlayers;

        private Dictionary<Data.Entities, Entity> _entities;

        public bool IsPlayerSpawned(string ckey) => _spawnedPlayers.Contains(ckey);

        public bool IsPlayerSpawned(NetworkConnection connection) =>
            _spawnedPlayers.Contains(GameSystems.Get<PlayerControlSystem>().GetCkey(connection));

        public Entity GetEntityPrefab(Data.Entities entity) => _entities[entity]; 

        protected override void OnStart()
        {
            base.OnStart();

            _spawnedPlayers.OnChange += HandleSpawnedPlayersChanged;

            LoadEntities();
        }

        /// <summary>
        /// Loads all assets for entities
        /// </summary>
        private void LoadEntities()
        {
            List<AssetReference> assets = AssetData.Entities.Assets;

            Entity getEntityFilter(AssetReference reference)
            {
                GameObject asset = reference.Asset as GameObject;
                return asset!.GetComponent<Entity>();
            }

            List<Entity> entitiesList = assets.Select(getEntityFilter).ToList();

            Dictionary<Data.Entities, Entity> entities = new();
            Array values = Enum.GetValues(typeof(Data.Entities));

            for (int index = 0; index < values.Length; index++)
            {
                Data.Entities id = (Data.Entities)values.GetValue(index);
                Entity entity = entitiesList[index];

                entity.Id = id;

                entities.Add(id, entity);
            }

            _entities = entities;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            SyncSpawnedPlayers();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            ServerManager.RegisterBroadcast<RequestEmbarkMessage>(HandleRequestEmbark);
            ServerManager.RegisterBroadcast<RequestMindSwap>(HandleRequestMindSwap);
            ServerManager.RegisterBroadcast<RequestJoinAsAdmin>(HandleRequestJoinAsAdmin);

            SpawnReadyPlayersEvent.AddListener(HandleSpawnReadyPlayers);
            RoundStateUpdated.AddListener(HandleRoundStateUpdated);
        }

        private void HandleRequestMindSwap(NetworkConnection conn, RequestMindSwap m)
        {
            ProcessMindSwap(m.Origin, m.Target);
        }

        private void HandleSpawnedPlayersChanged(SyncListOperation op, int index, string olditem, string newitem, bool asserver)
        {
            SyncSpawnedPlayers();
        }

        [Server]
        private void ProcessMindSwap(GameObject origin, GameObject target)
        {
            Entity originControllable = origin.GetComponent<Entity>();
            Entity targetControllable = target.GetComponent<Entity>();

            Soul originSoul = originControllable.ControllingSoul;
            Soul targetSoul = targetControllable.ControllingSoul;

            originControllable.SetControllingSoul(targetSoul);
            targetControllable.SetControllingSoul(originSoul);
        }

        [Server]
        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            RoundState roundState = e.RoundState;

            if (roundState == RoundState.Stopped)
            {
                _alreadySpawnedInitialPlayers = false;
                _spawnedPlayers.Clear();  

                DestroySpawnedPlayers();
            }
        }

        [Server]
        private void HandleSpawnReadyPlayers(ref EventContext context, in SpawnReadyPlayersEvent e)
        {
            List<string> playersToSpawn = e.ReadyPlayers;

            SpawnReadyPlayers(playersToSpawn);
        }

        [Server]
        private void HandleRequestEmbark(NetworkConnection conn, RequestEmbarkMessage m)
        {
            string ckey = m.Ckey;

            SpawnLatePlayer(ckey);
        }

        [Server]
        private void HandleRequestJoinAsAdmin(NetworkConnection networkConnection, RequestJoinAsAdmin requestJoinAsAdmin)
        {
            const ServerRoleTypes requiredRole = ServerRoleTypes.Administrator;

            string author = requestJoinAsAdmin.Author.Ckey;
            PermissionSystem permissionSystem = GameSystems.Get<PermissionSystem>();

            bool isUserAuthorized = permissionSystem.IsUserAuthorized(author, requiredRole);
            if (isUserAuthorized)
            {
                SpawnLatePlayer(author, Data.Entities.TilemapGhost);
            }
        }

        /// <summary>
        /// Spawns a player after the round has started
        /// </summary>
        /// <param name="ckey"></param>
        [Server]
        private void SpawnLatePlayer(string ckey, Data.Entities entityToSpawn = Data.Entities.Human)
        {
            if (!_spawnedPlayers.Contains(ckey) && _alreadySpawnedInitialPlayers)
            {
                SpawnPlayer(ckey, entityToSpawn);
            }
        }

        /// <summary>
        /// Spawns all the players that are ready when the round starts
        /// </summary>
        /// <param name="players"></param>
        [Server]
        private void SpawnReadyPlayers(List<string> players)
        {
            if (_alreadySpawnedInitialPlayers)
            {
                return;
            }

            if (players == null || players.Count == 0)
            {
                _alreadySpawnedInitialPlayers = true;
                Punpun.Say(this, "No players to spawn", Logs.ServerOnly);
                return;
            }

            foreach (string ckey in players)
            {
                SpawnPlayer(ckey);
            }

            _alreadySpawnedInitialPlayers = true;
        }

        /// <summary>
        /// Destroys all spawned players
        /// </summary>
        [Server]
        private void DestroySpawnedPlayers()
        {
            foreach (Entity player in _serverSpawnedPlayers)
            {
                player.ProcessDespawn();
            }

            _serverSpawnedPlayers.Clear();
        }

        /// <summary>
        /// Spawns a player with a Ckey
        /// </summary>
        /// <param name="ckey">Unique user key</param>
        [Server]
        private void SpawnPlayer(string ckey, Data.Entities entityToSpawn = Data.Entities.Human)
        {
            PlayerControlSystem playerControlSystem = GameSystems.Get<PlayerControlSystem>();

            Soul soul = playerControlSystem.GetSoul(ckey);
            _spawnedPlayers.Add(ckey);

            Entity entity = GetEntityPrefab(entityToSpawn);
            entity = Instantiate(entity, _tempSpawnPoint.position, Quaternion.identity);

            _serverSpawnedPlayers.Add(entity);

            ServerManager.Spawn(entity.NetworkObject, soul.Owner);
            entity.SetControllingSoul(soul);

            string message = $"Spawning player {soul.Ckey} on {entity.name}";
            Punpun.Say(this, message, Logs.ServerOnly); 
        }

        private void SetSpawnedPlayers(SyncListOperation op, int index, string old, string player, bool asServer)
        {
            SyncSpawnedPlayers();
        }

        private void SyncSpawnedPlayers()
        {
            SpawnedPlayersUpdated spawnedPlayersUpdated = new(_spawnedPlayers.ToList());
            spawnedPlayersUpdated.Invoke(this);
        }
    }
}
