using System.Collections.Generic;
using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Rounds.Messages;
using UnityEngine;

namespace SS3D.Systems.Entities
{
    public class EntitySpawnSystem : NetworkedSystem
    {
        [SerializeField] private PlayerControllable _tempHuman;
        [SerializeField] private Transform _tempSpawnPoint;

        private bool _alreadySpawnedInitialPlayers;
        private readonly List<Soul> _spawnedPlayers = new();

        public override void OnStartServer()
        {
            base.OnStartServer();

            SpawnReadyPlayersEvent.AddListener(HandleSpawnReadyPlayers);
            RoundStateUpdated.AddListener(HandleRoundStateUpdated);
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            RoundState roundState = e.RoundState;

            if (roundState == RoundState.Stopped)
            {
                _alreadySpawnedInitialPlayers = false;
            }
        }

        [Server]
        private void HandleSpawnReadyPlayers(ref EventContext context, in SpawnReadyPlayersEvent e)
        {
            List<string> playersToSpawn = e.ReadyPlayers;

            SpawnReadyPlayers(playersToSpawn);
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

            PlayerControlSystem playerControlSystem = GameSystems.Get<PlayerControlSystem>();

            foreach (string ckey in players)
            {
                Soul soul = playerControlSystem.GetSoul(ckey);
                _spawnedPlayers.Add(soul);

                PlayerControllable controllable = Instantiate(_tempHuman, _tempSpawnPoint.position, Quaternion.identity);
                ServerManager.Spawn(controllable.NetworkObject, soul.Owner);
                
                controllable.GiveOwnership(soul.Owner);
                controllable.ControllingSoul = soul.Owner;

                string message = $"Spawning player {soul.Ckey} on {controllable.name}";
                Punpun.Say(this, message, Logs.ServerOnly);
            }

            _alreadySpawnedInitialPlayers = true;
        }
    }
}
