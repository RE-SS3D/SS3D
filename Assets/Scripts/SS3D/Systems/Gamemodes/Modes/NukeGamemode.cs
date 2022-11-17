using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Logging;
using SS3D.Systems.Rounds.Events;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.GameModes.Modes
{
    [CreateAssetMenu(menuName = "Gamemodes/Modes/NukeGamemode", fileName = "NukeGamemode")]
    public class NukeGamemode : Gamemode
    {
        [Server]
        public override void InitializeGamemode()
        {
            SpawnReadyPlayersEvent.AddListener(HandleReadyPlayersChanged);
        }

        [Server]
        private void HandleReadyPlayersChanged(ref EventContext context, in SpawnReadyPlayersEvent spawnReadyPlayersEvent)
        {
            List<string> players = spawnReadyPlayersEvent.ReadyPlayers;

            int index = Random.Range(0, players.Count);
            Traitors.Add(players[index]);
        }
    }
}
