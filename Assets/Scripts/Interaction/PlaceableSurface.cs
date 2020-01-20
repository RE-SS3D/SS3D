using Inventory.Custom;
using UnityEngine;

namespace Interaction
{
    [RequireComponent(typeof(Interactable))]
    public class PlaceableSurface : MonoBehaviour, IInteractable
    {
        public void Advertise(Interactable interactable)
        {
            interactable.Subscribe(InteractionKind.Click, this);
        }

        public void Handle(InteractionEvent e)
        {
            e.parent.GetComponent<Hands>().Drop();
            e.sender.position = e.worldPosition;
        }
    }
}