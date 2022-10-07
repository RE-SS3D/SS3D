using System.Collections.Generic;
using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds.Events;
using UnityEngine;
using LogType = SS3D.Logging.LogType;

namespace SS3D.Systems.Entities
{
    public class EntitySpawnSystem : NetworkedSystem
    {
        [SerializeField] private PlayerControllable _tempHuman;
        [SerializeField] private Transform _tempSpawnPoint;

        public override void OnStartServer()
        {
            base.OnStartServer();

            SpawnReadyPlayersEvent.AddListener(HandleSpawnReadyPlayers);
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
            if (players == null || players.Count == 0)
            {
                Punpun.Say(this, "No players to spawn", LogType.ServerOnly);
                return;
            }

            PlayerControlSystem playerControlSystem = GameSystems.Get<PlayerControlSystem>();

            foreach (string ckey in players)
            {
                Soul soul = playerControlSystem.GetSoul(ckey);

                PlayerControllable controllable = Instantiate(_tempHuman, _tempSpawnPoint);

                ServerManager.Spawn(controllable.GameObjectCache, soul.Owner);
                controllable.ControllingSoul = soul.Owner;

                string message = $"Spawning played {soul.Ckey} on {controllable.name}";
                Punpun.Say(this, message, LogType.ServerOnly);
            }
        }
    }
}
