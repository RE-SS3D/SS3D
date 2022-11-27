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
    [CreateAssetMenu(menuName = "Gamemodes/Objectives/GetNukeCard", fileName = "GetNukeCard")]
    public class GetNukeCardObjective : GamemodeObjective
    {
        /// <summary>
        /// The item that was caught when the objective was completed.
        /// </summary>
        private Item _caughtItem;

        private const string ObjectiveTitle = "Retrieve the Nuclear Authentication Disk"; 

        public override void InitializeObjective()
        {
            ItemPickedUpEvent.AddListener(HandleItemPickedUpEvent);

            Title = ObjectiveTitle;
            Status = ObjectiveStatus.InProgress;
        }

        public override void FinalizeObjective()
        {
            if (_caughtItem is not NukeCard)
            {
                return;
            }

            List<string> traitors = SystemLocator.Get<GamemodeSystem>().Antagonists;
            string ckey = SystemLocator.Get<PlayerControlSystem>().GetCkey(Assignee);

            if (traitors.Contains(ckey))
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

        public GetNukeCardObjective(int id, string title, ObjectiveStatus status, NetworkConnection author) : base(id, title, status, author) { }
    }
}