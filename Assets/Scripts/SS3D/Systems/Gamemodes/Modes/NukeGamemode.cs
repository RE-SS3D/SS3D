using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Logging;
using SS3D.Systems.Rounds.Events;
using System;
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
            base.InitializeGamemode();
            SpawnReadyPlayersEvent.AddListener(HandleReadyPlayersChanged);
        }

        [Server]
        private void HandleReadyPlayersChanged(ref EventContext context, in SpawnReadyPlayersEvent spawnReadyPlayersEvent)
        {
            if (Traitors.Count != 0)
                return;

            List<string> players = spawnReadyPlayersEvent.ReadyPlayers;

            var random = new System.Random();
            int index = random.Next(players.Count);

            Traitors.Add(players[index]);
        }
    }
}
