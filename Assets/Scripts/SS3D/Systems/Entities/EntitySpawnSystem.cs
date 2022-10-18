using System.Collections.Generic;
using System.Linq;
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
    public class EntitySpawnSystem : NetworkedSystem
    {
        [SerializeField] private List<PlayerControllable> _tempHuman;
        [SerializeField] private Transform _tempSpawnPoint;

        [SyncObject]
        private readonly SyncList<string> _spawnedPlayers = new();

        private readonly List<PlayerControllable> _serverSpawnedPlayers = new();
        private bool _alreadySpawnedInitialPlayers;

        public bool IsPlayedSpawned(string ckey) => _spawnedPlayers.Contains(ckey);

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

        private void HandleSpawnedPlayersChanged(SyncListOperation op, int index, string olditem, string newitem, bool asserver)
        {
            SyncSpawnedPlayers();
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

            _alreadySpawnedInitialPlayers = false;
            _spawnedPlayers.Clear();  

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

        [Server]
        private void SpawnLatePlayer(string ckey)
        {
            if (!_spawnedPlayers.Contains(ckey) && _alreadySpawnedInitialPlayers)
            {
                SpawnPlayer(ckey);
            }
        }

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

        [Server]
        private void DestroySpawnedPlayers()
        {
            foreach (PlayerControllable player in _serverSpawnedPlayers)
            {
                player.ProcessDespawn();
            }

            _serverSpawnedPlayers.Clear();
        }

        private void SpawnPlayer(string ckey)
        {
            PlayerControlSystem playerControlSystem = GameSystems.Get<PlayerControlSystem>();

            Soul soul = playerControlSystem.GetSoul(ckey);
            _spawnedPlayers.Add(ckey);

            PlayerControllable controllable = Instantiate(_tempHuman[Random.Range(0, _tempHuman.Count)], _tempSpawnPoint.position, Quaternion.identity);
            _serverSpawnedPlayers.Add(controllable);

            ServerManager.Spawn(controllable.NetworkObject, soul.Owner);
            controllable.SetControllingSoul(soul);

            string message = $"Spawning player {soul.Ckey} on {controllable.name}";
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
