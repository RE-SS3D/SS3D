using System;
using Interaction.Core;
using Inventory.Custom;
using UnityEngine;

namespace Interaction
{
    [RequireComponent(typeof(Interactable))]
    public class PlaceableSurface : MonoBehaviour, IInteractable
    {
        public void Setup(Action<string> listen, Action<string> blocks)
        {
            listen("place");
        }

        public bool Handle(InteractionEvent e)
        {
            var item = e.sender.GetComponent<Item>();
            item.container.RemoveItem(item.gameObject);
            item.transform.position = e.worldPosition;
            item.gameObject.SetActive(true);

            return true;
        }
    }
}