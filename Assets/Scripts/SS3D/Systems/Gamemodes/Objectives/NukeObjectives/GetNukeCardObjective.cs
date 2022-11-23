using Coimbra.Services.Events;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.GameModes;
using SS3D.Systems.GameModes.Modes;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.GameModes.Objectives;
using SS3D.Systems.Items;
using SS3D.Systems.Storage.Items;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.Objectives.NukeObjectives
{
    [CreateAssetMenu(menuName = "Gamemodes/Objectives/GetNukeCard", fileName = "GetNukeCard")]
    public class GetNukeCardObjective : GamemodeObjective
    {
        private Item ItemRef;
        private string Player;

        [Server]
        public override void InitializeObjective()
        {
            ItemPickedUpEvent.AddListener(HandleItemPickedUpEvent);
            Title = "Retrieve the Nuclear Authentication Disk";
            Status = ObjectiveStatus.InProgress;
        }

        [Server]
        public override void CheckCompleted()
        {
            if (ItemRef is NukeCard)
            {
                List<string> Traitors = GameSystems.Get<GamemodeSystem>().Gamemode.Traitors;
                if (Traitors.Contains(Player))
                {
                    Success();
                }
            }
        }

        private void HandleItemPickedUpEvent(ref EventContext context, in ItemPickedUpEvent e)
        {
            ItemRef = e.ItemRef;
            Player = e.Player;

            CheckCompleted();
        }
    }
}
