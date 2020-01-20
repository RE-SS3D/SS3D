using Inventory.Custom;
using UnityEngine;

namespace Interaction
{
    [RequireComponent(typeof(Interactable))]
    public class PlaceableSurface : MonoBehaviour, IInteractable
    {
        public void Advertise(Interactable interactable)
        {
            interactable.Subscribe("place", this);
        }

        public void Handle(InteractionEvent e)
        {
            var item = e.sender.GetComponent<Item>();
            item.container.RemoveItem(item.gameObject);
            item.transform.position = e.worldPosition;
            item.gameObject.SetActive(true);
        }
    }
}