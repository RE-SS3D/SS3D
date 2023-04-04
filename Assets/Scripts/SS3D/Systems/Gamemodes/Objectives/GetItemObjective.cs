using Coimbra.Services.Events;
using SS3D.Systems.GameModes.Events;
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
        [FormerlySerializedAs("_targetItemIdId")] [FormerlySerializedAs("_targetItemId")] [SerializeField] private Data.Enums.ItemId _targetItemIdId;

        /// <summary>
        /// The item that was picked up.
        /// </summary>
        private Data.Enums.ItemId _caughtItemIdId;

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
            if (!_caughtItemIdId.Equals(_targetItemIdId) || !_caughtPlayerCkey.Equals(AssigneeCkey))
            {
                return;
            }
            Succeed();
        }

        private void HandleItemPickedUpEvent(ref EventContext context, in ItemPickedUpEvent e)
        {
            Data.Enums.ItemId itemIdId = e.Item.ItemId;
            string playerCkey = e.Player;

            if (itemIdId == _targetItemIdId  && playerCkey.Equals(AssigneeCkey))
            {
                _caughtItemIdId = itemIdId;
                _caughtPlayerCkey = playerCkey;

                FinalizeObjective();
            }
        }
    }
}