using UnityEngine;
using System.Collections;
using Inventory;

namespace Interactions2.Custom
{
    /**
     * <summary>Allows a player to 'open' a container in the UI and interact with it.</summary>
     */
    [CreateAssetMenu(fileName = "ViewContainer", menuName = "Interactions2/ViewContainer")]
    public class ViewContainer : Core.InteractionSO
    {
        public float maxDistance = 5.0f;

        public override bool CanInteract(GameObject tool, GameObject target, RaycastHit at)
        {
            return target.GetComponent<Container>() && Vector3.Distance(tool.transform.position, target.transform.position) < maxDistance;
        }

        public override void Interact(GameObject tool, GameObject target, RaycastHit at)
        {
            var player = tool.transform.root.gameObject;
            var inventory = player.GetComponent<Inventory.Inventory>();

            if(!inventory.HasContainer(target)) {
                // Use the Container Attachment component, which will automatically disconnect the container if the player leaves range.
                var attacher = player.AddComponent<ContainerAttachment>();
                attacher.container = target.GetComponent<Container>();
                attacher.inventory = inventory;
                attacher.range = maxDistance;
            }
            else {
                // If they have it open and they click on the target, we close the ui.
                inventory.RemoveContainer(target);
            }
        }
    }
}
