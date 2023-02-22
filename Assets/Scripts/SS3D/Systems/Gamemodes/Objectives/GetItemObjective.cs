using System.Collections.Generic;
using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Data.Enums;
using SS3D.Systems.GameModes.Events;
using SS3D.Systems.Items;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Storage.Items;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Gamemodes.Objectives
{
    /// <summary>
    /// An objective which the goal is to get a particular item in hand.
    /// A GamemodeObjective that listens to the ItemPickedUpEvent.
    /// </summary>
    [CreateAssetMenu(menuName = "Gamemode/Objectives/GetItem", fileName = "GetItem")]
    public class GetItemObjective : GamemodeObjective
    {
        /// <summary>
        /// The item id required to complete the objective.
        /// </summary>
        [SerializeField] private ItemIDs _targetItemId;

        /// <summary>
        /// The item that was picked up.
        /// </summary>
        private ItemIDs _caughtItemId;

        /// <summary>
        /// The player that picked up the item.
        /// </summary>
        private string _caughtPlayerCkey;

        /// <inheritdoc />
        public override void AddEventListeners()
        {
            ItemPickedUpEvent.AddListener(HandleItemPickedUpEvent);
        }

        /// <inheritdoc />
        public override void FinalizeObjective()
        {
            // Confirm correct item has been picked up by the correct player
            if (!_caughtItemId.Equals(_targetItemId) || !_caughtPlayerCkey.Equals(AssigneeCkey))
            {
                return;
            }
            Succeed();
        }

        private void HandleItemPickedUpEvent(ref EventContext context, in ItemPickedUpEvent e)
        {
            ItemIDs itemId = e.Item.ItemID;
            string playerCkey = e.Player;

            if (itemId == _targetItemId  && playerCkey.Equals(AssigneeCkey))
            {
                _caughtItemId = itemId;
                _caughtPlayerCkey = playerCkey;

                FinalizeObjective();
            }
        }
    }
}