using System.Collections.Generic;
using System.Linq;
using Coimbra;
using Coimbra.Services.Events;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Entities.Messages;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Rounds.Messages;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Controls player spawning.
    /// </summary>
    public class EntitySpawnSystem : NetworkSystem
    {
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

        /// <summary>
        /// Returns true if the player is controlling an entity.
        /// </summary>
        /// <param name="soul">The player's ckey</param>
        /// <returns>Is the player is controlling an entity</returns>
        public bool IsPlayedSpawned(Soul soul)
        {
            Entity isPlayedSpawned = _spawnedPlayers.Find(entity => entity.Mind.Soul == soul);

            return isPlayedSpawned;
        }

        /// <summary>
        /// Returns true if the networkConnection is controlling an entity.
        /// </summary>
        /// <param name="networkConnection">The player's connection</param>
        /// <returns>Is the player is controlling an entity</returns>
        public bool IsPlayedSpawned(NetworkConnection networkConnection)
        {
            return _spawnedPlayers.Find(controllable => controllable.Owner == networkConnection);
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

            AddEventListeners();
        }

        private void AddEventListeners()
        {
            ServerManager.RegisterBroadcast<RequestEmbarkMessage>(HandleRequestEmbark);
            ServerManager.RegisterBroadcast<RequestMindSwapMessage>(HandleRequestMindSwap);

            AddHandle(SpawnReadyPlayersEvent.AddListener(HandleSpawnReadyPlayers));
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
        }

        /// <summary>
        /// Executes a mind swap.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="target"></param>
        [Server]
        private static void ExecuteMindSwap(GameObject origin, GameObject target)
        {
            Entity originControllable = origin.GetComponent<Entity>();
            Entity targetControllable = target.GetComponent<Entity>();

            Mind originSoul = originControllable.Mind;
            Mind targetSoul = targetControllable.Mind;

            originControllable.SetMind(targetSoul);
            targetControllable.SetMind(originSoul);
        }

        /// <summary>
        /// Spawns a player after the round has started
        /// </summary>
        /// <param name="ckey">The player's ckey</param>
        [Server]
        private void SpawnLatePlayer(Soul ckey)
        {
            if (!IsPlayedSpawned(ckey) && _hasSpawnedInitialPlayers)
            {
                SpawnPlayer(ckey);
            }
        }

        /// <summary>
        /// Spawns all the players that are ready when the round starts
        /// </summary>
        /// <param name="players"></param>
        [Server]
        private void SpawnReadyPlayers(List<Soul> players)
        {
            if (_hasSpawnedInitialPlayers)
            {
                return;
            }

            if (players.Count == 0)
            {
                Punpun.Say(this, "No players to spawn", Logs.ServerOnly);
            }

            foreach (Soul ckey in players)
            {
                SpawnPlayer(ckey);
            }

            _hasSpawnedInitialPlayers = true;

            InitialPlayersSpawnedEvent initialPlayersSpawnedEvent = new(SpawnedPlayers);
            initialPlayersSpawnedEvent.Invoke(this);
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
                player.GameObjectCache.Destroy();
            }

            _hasSpawnedInitialPlayers = false;
            _spawnedPlayers.Clear();
        }

        /// <summary>
        /// Spawns a player with a Ckey
        /// </summary>
        /// <param name="soul">Unique user object</param>
        [Server]
        private void SpawnPlayer(Soul soul)
        {
            MindSystem mindSystem = SystemLocator.Get<MindSystem>();
            mindSystem.TryCreateMind(soul.Owner, out Mind createdMind);

            Entity entity = Instantiate(_humanPrefab[Random.Range(0, _humanPrefab.Count)], _spawnPoint.position, Quaternion.identity);
            ServerManager.Spawn(entity.NetworkObject, soul.Owner);

            createdMind.SetSoul(soul);
            entity.SetMind(createdMind);

            _spawnedPlayers.Add(entity);

            string message = $"Spawning player {soul.Ckey} on {entity.name}";
            Punpun.Say(this, message, Logs.ServerOnly);
        }

        private void HandleRequestMindSwap(NetworkConnection conn, RequestMindSwapMessage m)
        {
            ExecuteMindSwap(m.Origin, m.Target);
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
            List<Soul> playersToSpawn = e.ReadyPlayers;

            SpawnReadyPlayers(playersToSpawn);
        }

        [Server]
        private void HandleRequestEmbark(NetworkConnection conn, RequestEmbarkMessage m)
        {
            Soul soul = m.Soul;

            SpawnLatePlayer(soul);
        }

        private void HandleSpawnedPlayersChanged(SyncListOperation op, int index, Entity old, Entity @new, bool asServer)
        {
            if (op == SyncListOperation.Complete)
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
            SpawnedPlayersUpdated spawnedPlayersUpdated = new(SpawnedPlayers);
            spawnedPlayersUpdated.Invoke(this);
        }

        void SyncHasSpawnedInitialPlayers(bool oldValue, bool newValue, bool asServer)
        {
        }
    }
}
