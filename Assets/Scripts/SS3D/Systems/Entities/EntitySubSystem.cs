using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using Coimbra.Services.Events;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Settings;
using SS3D.Engine.Chat;
using SS3D.Logging;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Roles;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Controls player spawning.
    /// </summary>
    public class EntitySubSystem : NetworkSubSystem
    {
        /// <summary>
        /// Event that should be evoked only on client, when the client spawns in the station.
        /// </summary>
        public Action OnClientSpawn;

        /// <summary>
        /// The prefab used for the player object.
        /// </summary>
        [Header("Settings")]
        [SerializeField]
        private List<Entity> _humanPrefab;

        /// <summary>
        /// The point used to place spawned players
        /// </summary>
        [SerializeField]
        private Transform _spawnPoint;

        /// <summary>
        /// List of the spawned players in the round
        /// </summary>
        [SyncObject]
        private readonly SyncList<Entity> _spawnedPlayers = new();

        /// <summary>
        /// If the system already spawned all the players that were ready when the round started
        /// </summary>
        [SyncVar(OnChange = nameof(SyncHasSpawnedInitialPlayers))]
        private bool _hasSpawnedInitialPlayers;

        public Entity GetSpawnedEntity(Player player)
        {
            var entity = _spawnedPlayers.Find(entity => entity.Mind.player == player);
            if (IsPlayerSpawned(player))
            {
                return entity;
            }
            return null;
        }

        public bool TryGetSpawnedEntity(NetworkConnection conn, out Entity entity)
        {
            entity = _spawnedPlayers.Find(entity => entity.Mind?.player?.Owner == conn);
            return entity != null;
        }

        /// <summary>
        /// Returns true if the player is controlling an entity.
        /// </summary>
        /// <param name="playerThe player's ckey</param>
        /// <returns>Is the player is controlling an entity</returns>
        public bool IsPlayerSpawned(Player player)
        {
            Entity spawnedPlayer = _spawnedPlayers.Find(entity => entity.Mind.player == player);
            return spawnedPlayer != null && spawnedPlayer.Mind != Mind.Empty;
        }

        /// <summary>
        /// Returns true if the networkConnection is controlling an entity.
        /// </summary>
        /// <param name="networkConnection">The player's connection</param>
        /// <returns>Is the player is controlling an entity</returns>
        public bool IsPlayerSpawned(NetworkConnection networkConnection)
        {
            Entity spawnedPlayer = _spawnedPlayers.Find(entity => entity.Mind?.player?.Owner == networkConnection);

            bool isPlayerSpawned;

            if (spawnedPlayer == null)
            {
                isPlayerSpawned = false;
            }
            else if (spawnedPlayer.Mind == Mind.Empty)
            {
                isPlayerSpawned = false;
            }
            else
            {
                isPlayerSpawned = true;
            }

            return isPlayerSpawned;
        }

        /// <summary>
        /// List of currently spawned players in the round.
        /// </summary>
        public List<Entity> SpawnedPlayers => _spawnedPlayers.ToList();

        /// <summary>
        /// Returns the last spawned player.
        /// </summary>
        public Entity LastSpawned => _spawnedPlayers.Count != 0 ? _spawnedPlayers.Last() : null;

        protected override void OnStart()
        {
            base.OnStart();

            _spawnedPlayers.OnChange += HandleSpawnedPlayersChanged;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            SyncSpawnedPlayers();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            ServerAddEventListeners();
        }

        private void ServerAddEventListeners()
        {
            AddHandle(SpawnReadyPlayersEvent.AddListener(HandleSpawnReadyPlayers));
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
        }

        /// <summary>
        /// Asks the server to spawn a player.
        /// </summary>
        /// <param name="player</param>
        /// <param name="networkConnection"></param>
        [ServerRpc(RequireOwnership = false)]
        public void CmdSpawnLatePlayer(Player player, NetworkConnection networkConnection = null)
        {
            SpawnLatePlayer(player);
        }

        public bool TryGetOwnedEntity(NetworkConnection conn, out Entity entity)
        {
            foreach(Entity e in SpawnedPlayers)
            {
                if(e.Owner == conn)
                {
                    entity = e;
                    return true;
                }
            }
            entity = null;
            return false;
        }

        /// <summary>
        /// Spawns a player after the round has started
        /// </summary>
        /// <param name="playerThe player's ckey</param>
        [Server]
        private void SpawnLatePlayer(Player player)
        {
            if (!IsPlayerSpawned(player) && _hasSpawnedInitialPlayers)
            {
                SpawnPlayer(player);
                ChatSubSystem chatSystem = SubSystems.Get<ChatSubSystem>();
                ChatChannels chatChannels = ScriptableSettings.GetOrFind<ChatChannels>();
                
                // TODO: replace with character name and role
                chatSystem.SendServerMessage(chatChannels.stationAlertsChannel, $"{player.Ckey}, assistant, has joined the ship");
            }
        }

        /// <summary>
        /// Spawns a player with a Ckey
        /// </summary>
        /// <param name="playerUnique user object</param>
        [Server]
        private void SpawnPlayer(Player player)
        {
            MindSubSystem mindSystem = SubSystems.Get<MindSubSystem>();
            mindSystem.TryCreateMind(player, out Mind createdMind);

            Entity entity = Instantiate(_humanPrefab[Random.Range(0, _humanPrefab.Count)], _spawnPoint.position, Quaternion.identity);
            ServerManager.Spawn(entity.NetworkObject, player.Owner);

            createdMind.SetPlayer(player);
            entity.SetMind(createdMind);

            SubSystems.Get<RoleSubSystem>().GiveRoleLoadoutToPlayer(entity);

            _spawnedPlayers.Add(entity);

            RpcInvokeClientSpawned(entity.Owner);

            Log.Information(this, "Spawning mind {createdMind} on {entity}", Logs.ServerOnly, createdMind.name, entity.name);
        }

        /// <summary>
        /// Spawns all the players that are ready when the round starts
        /// </summary>
        /// <param name="players"></param>
        [Server]
        private void SpawnReadyPlayers(List<Player> players)
        {
            if (_hasSpawnedInitialPlayers) return;

            if (players.Count == 0)
            {
                Log.Information(this, "No players to spawn", Logs.ServerOnly);
            }

            foreach (Player ckey in players)
            {
                SpawnPlayer(ckey);
            }

            _hasSpawnedInitialPlayers = true;

            new InitialPlayersSpawned(SpawnedPlayers).Invoke(this);
        }

        /// <summary>
        /// Destroys all spawned players
        /// </summary>
        [Server]
        private void DestroySpawnedPlayers()
        {
            foreach (Entity player in SpawnedPlayers)
            {
                ServerManager.Despawn(player.NetworkObject);
                player.GameObject.Dispose(true);
            }

            _hasSpawnedInitialPlayers = false;
            _spawnedPlayers.Clear();
        }

        [Server]
        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            RoundState roundState = e.RoundState;

            if (roundState != RoundState.Stopped)
            {
                return;
            }

            DestroySpawnedPlayers();
        }

        [Server]
        private void HandleSpawnReadyPlayers(ref EventContext context, in SpawnReadyPlayersEvent e)
        {
            List<Player> playersToSpawn = e.ReadyPlayers;

            SpawnReadyPlayers(playersToSpawn);
        }

        private void HandleSpawnedPlayersChanged(SyncListOperation op, int index, Entity old, Entity @new, bool asServer)
        {
            if (op == SyncListOperation.Complete)
            {
                return;
            }

			if(op == SyncListOperation.Set)
			{
				return;
			}

            if (!asServer && IsHost)
            {
                return;
            }

            SyncSpawnedPlayers();
        }

        private void SyncSpawnedPlayers()
        {
            if (SpawnedPlayers.IsNullOrEmpty())
            {
                return;
            }

            SpawnedPlayersUpdated spawnedPlayersUpdated = new(SpawnedPlayers);
            spawnedPlayersUpdated.Invoke(this);
        }

        private void SyncHasSpawnedInitialPlayers(bool oldValue, bool newValue, bool asServer)
        {
            if (!asServer && IsHost)
            {
                return;
            }
        }

		public bool TryTransferEntity(Entity oldEntity, Entity newEntity)
		{
			int index = _spawnedPlayers.FindIndex(x => x == oldEntity);
            if (index == -1)
            {
                Log.Warning(this, $"could not find entity {oldEntity} in the list of spawned entity controlled by players");
                return false;
            }
			_spawnedPlayers[index] = newEntity;
            return true;
		}

        [TargetRpc]
        private void RpcInvokeClientSpawned(NetworkConnection target)
        {
            OnClientSpawn?.Invoke();
        }


	}
}
