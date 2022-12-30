using System.Collections.Generic;
using Coimbra.Services.Events;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.Items;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Storage.Items;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.Objectives
{
    /// <summary>
    /// An objective which the goal is to get the nuke card in hand.
    /// A GamemodeObjective that listens to the ItemPickedUpEvent.
    /// </summary>
    [CreateAssetMenu(menuName = "Gamemode/Objectives/GetNukeCard", fileName = "GetNukeCard")]
    public class GetNukeCardObjective : GamemodeObjective
    {
        /// <summary>
        /// The item that was caught when the objective was completed.
        /// </summary>
        private Item _caughtItem;

        private const string ObjectiveTitle = "Retrieve the Nuclear Authentication Disk";

        /// <inheritdoc />
        public override void InitializeObjective()
        {
            base.InitializeObjective();

            SetTitle(ObjectiveTitle); 
        }

        /// <inheritdoc />
        public override void AddEventListeners()
        {
            ItemPickedUpEvent.AddListener(HandleItemPickedUpEvent);
        }

        /// <inheritdoc />
        public override void FinalizeObjective()
        {
            if (_caughtItem is not NukeCard)
            {
                return;
            }

            List<string> traitors = SystemLocator.Get<GamemodeSystem>().Antagonists;

            if (traitors.Contains(AssigneeCkey))
            {
                Succeed();
            }
        }

        private void HandleItemPickedUpEvent(ref EventContext context, in ItemPickedUpEvent e)
        {
            Item item = e.Item;

            if (item is NukeCard)
            {
                _caughtItem = item;
                FinalizeObjective();
            }
        }
    }
}