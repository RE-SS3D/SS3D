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
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Entities.Messages;
using SS3D.Systems.PlayerControl;
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
        [Header("Settings")]
        [SerializeField]
        private List<PlayerControllable> _tempHuman;

        [SerializeField] 
        private Transform _tempSpawnPoint;

        [SyncObject]
        private readonly SyncList<PlayerControllable> _spawnedPlayers = new();

        private bool _alreadySpawnedInitialPlayers;

        public bool IsPlayedSpawned(string ckey) => _spawnedPlayers.Find(controllable => controllable.ControllingSoul.Ckey == ckey);
        public bool IsPlayedSpawned(NetworkConnection networkConnection) => _spawnedPlayers.Find(controllable => controllable.Owner == networkConnection);
        public List<PlayerControllable> SpawnedPlayers => _spawnedPlayers.ToList();
        public PlayerControllable LastSpawned => _spawnedPlayers.Count != 0 ? _spawnedPlayers.Last() : null;

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

            ServerManager.RegisterBroadcast<RequestEmbarkMessage>(HandleRequestEmbark);
            ServerManager.RegisterBroadcast<RequestMindSwap>(HandleRequestMindSwap);

            SpawnReadyPlayersEvent.AddListener(HandleSpawnReadyPlayers);
            RoundStateUpdated.AddListener(HandleRoundStateUpdated);
        }

        private void HandleRequestMindSwap(NetworkConnection conn, RequestMindSwap m)
        {
            ProcessMindSwap(m.Origin, m.Target);
        }

        [Server]
        private void ProcessMindSwap(GameObject origin, GameObject target)
        {
            PlayerControllable originControllable = origin.GetComponent<PlayerControllable>();
            PlayerControllable targetControllable = target.GetComponent<PlayerControllable>();

            Soul originSoul = originControllable.ControllingSoul;
            Soul targetSoul = targetControllable.ControllingSoul;

            originControllable.SetControllingSoul(targetSoul);
            targetControllable.SetControllingSoul(originSoul);
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
            List<string> playersToSpawn = e.ReadyPlayers;

            SpawnReadyPlayers(playersToSpawn);
        }

        [Server]
        private void HandleRequestEmbark(NetworkConnection conn, RequestEmbarkMessage m)
        {
            string ckey = m.Ckey;

            SpawnLatePlayer(ckey);
        }

        /// <summary>
        /// Spawns a player after the round has started
        /// </summary>
        /// <param name="ckey"></param>
        [Server]
        private void SpawnLatePlayer(string ckey)
        {
            if (!_spawnedPlayers.Find(controllable => controllable.ControllingSoul.Ckey == ckey) && _alreadySpawnedInitialPlayers)
            {
                SpawnPlayer(ckey);
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

            if (players.Count == 0)
            {
                Punpun.Say(this, "No players to spawn", Logs.ServerOnly);
            }

            foreach (string ckey in players)
            {
                SpawnPlayer(ckey);
            }

            _alreadySpawnedInitialPlayers = true;
            InitialPlayersSpawnedEvent initialPlayersSpawnedEvent = new(SpawnedPlayers);
            initialPlayersSpawnedEvent.Invoke(this);
        }

        /// <summary>
        /// Destroys all spawned players
        /// </summary>
        [Server]
        private void DestroySpawnedPlayers()
        {
            foreach (PlayerControllable player in SpawnedPlayers)
            {
                ServerManager.Despawn(player.NetworkObject);
                player.GameObjectCache.Destroy();
            }

            _alreadySpawnedInitialPlayers = false;
            _spawnedPlayers.Clear(); 
        }

        /// <summary>
        /// Spawns a player with a Ckey
        /// </summary>
        /// <param name="ckey">Unique user key</param>
        [Server]
        private void SpawnPlayer(string ckey)
        {
            PlayerControlSystem playerControlSystem = SystemLocator.Get<PlayerControlSystem>();

            Soul soul = playerControlSystem.GetSoul(ckey);
            PlayerControllable controllable = Instantiate(_tempHuman[Random.Range(0, _tempHuman.Count)], _tempSpawnPoint.position, Quaternion.identity);

            ServerManager.Spawn(controllable.NetworkObject, soul.Owner);
            controllable.SetControllingSoul(soul);

            _spawnedPlayers.Add(controllable);

            string message = $"Spawning player {soul.Ckey} on {controllable.name}";
            Punpun.Say(this, message, Logs.ServerOnly); 
        }

        private void HandleSpawnedPlayersChanged(SyncListOperation op, int index, PlayerControllable old, PlayerControllable @new, bool asServer)
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
    }
}
