using UnityEngine;
using System.Collections;
using Inventory;

namespace Interactions.Custom
{
    /**
     * <summary>Allows a player to 'open' a container in the UI and interact with it.</summary>
     */
    [CreateAssetMenu(fileName = "ViewContainer", menuName = "Interactions2/ViewContainer")]
    public class ViewContainer : Core.InteractionSO
    {
        public float maxDistance = 5.0f;
        public override string Name => "Open Container";

        public override bool CanInteract()
        {
            return Event.target.GetComponent<Container>() && Vector3.Distance(Event.tool.transform.position, Event.target.transform.position) < maxDistance;
        }

        public override void Interact()
        {
            var inventory = Event.Player.GetComponent<Inventory.Inventory>();

            if(!inventory.HasContainer(Event.target)) {
                // Use the Container Attachment component, which will automatically disconnect the container if the player leaves range.
                var attacher = Event.Player.AddComponent<ContainerAttachment>();
                attacher.container = Event.target.GetComponent<Container>();
                attacher.inventory = inventory;
                attacher.range = maxDistance;
            }
            else {
                // If they have it open and they click on the target, we close the ui.
                inventory.RemoveContainer(Event.target);
            }
        }
    }
}
