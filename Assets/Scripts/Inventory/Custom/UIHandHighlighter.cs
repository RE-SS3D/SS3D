using UnityEngine;
using Mirror;
using Inventory.UI;

namespace Inventory.Custom
{
    /**
     * Highlights the hand the player has selected.
     * Note: This should be pretty easily extensible to other creatures
     */
    public class UIHandHighlighter : MonoBehaviour
    {
        public UIItemSlot[] handSlots;

        private void Start()
        {
            // Find the hand component attached to the local player
            var hands = NetworkClient.connection.identity.GetComponent<Hands>();
            hands.onHandChange += OnHandChange;
            OnHandChange(hands.SelectedHand);
        }

        private void OnHandChange(int selectedHand)
        {
            for(int i = 0; i < handSlots.Length; i++)
                handSlots[i].Selected = selectedHand == i;
        }
    }
}
