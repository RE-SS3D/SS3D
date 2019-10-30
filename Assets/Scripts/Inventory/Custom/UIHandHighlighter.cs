using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

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
        var hands = NetworkClient.connection.playerController.GetComponent<Hands>();
        hands.onHandChange += OnHandChange;
        OnHandChange(hands.selectedHand);
    }

    private void OnHandChange(int selectedHand)
    {
        for (int i = 0; i < handSlots.Length; ++i)
            handSlots[i].SetHighlight(selectedHand == i);
    }
}
