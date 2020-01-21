using System;
using Interaction.Core;
using Inventory.Custom;
using UnityEngine;

namespace Interaction
{
    public class Pickupable : MonoBehaviour, IInteractable
    {
        
        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen("pickup");
        }

        public bool Handle(InteractionEvent e)
        {
            e.sender.GetComponent<Hands>().Pickup(gameObject);

            return true;
        }
    }
}