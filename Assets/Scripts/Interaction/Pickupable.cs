using System;
using Interaction.Core;
using Inventory.Custom;
using UnityEngine;
using Event = Interaction.Core.Event;

namespace Interaction
{
    public class Pickupable : MonoBehaviour, IInteraction
    {
        
        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen("pickup");
        }

        public bool Handle(Event e)
        {
            e.sender.GetComponent<Hands>().Pickup(gameObject);

            return true;
        }
    }
}