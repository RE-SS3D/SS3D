using System;
using Interaction.Core;
using Inventory.Custom;
using UnityEngine;
using Event = Interaction.Core.Event;

namespace Interaction
{
    [RequireComponent(typeof(InteractionReceiver))]
    public class PlaceableSurface : MonoBehaviour, IInteraction
    {
        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen("place");
        }

        public bool Handle(Event e)
        {
            var item = e.sender.GetComponent<Item>();
            item.container.RemoveItem(item.gameObject);
            item.transform.position = e.worldPosition;
            item.gameObject.SetActive(true);

            return true;
        }
    }
}