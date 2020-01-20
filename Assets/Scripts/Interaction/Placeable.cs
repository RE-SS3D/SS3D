using Inventory.Custom;
using UnityEngine;

namespace Interaction
{
    [RequireComponent(typeof(Interactable))]
    public class Placeable : MonoBehaviour, IInteractable
    {
        public void Advertise(Interactable interactable)
        {
            interactable.Subscribe("use", this);
        }

        public void Handle(InteractionEvent e)
        {
            if (!e.forwardTo) return;
            e.forwardTo.Trigger(e.Forward("place", transform));
        }
    }
}