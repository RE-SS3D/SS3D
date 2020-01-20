using System;
using Inventory.Custom;
using UnityEngine;

namespace Interaction
{
    public class Pickupable : MonoBehaviour, IInteractable
    {
        
        public void Advertise(Interactable interactable)
        {
            interactable.Subscribe(InteractionKind.Click, this);
        }

        public void Handle(InteractionEvent e)
        {
            Debug.Log(e);
            e.sender.GetComponent<Hands>().Pickup(gameObject);
        }
    }
}