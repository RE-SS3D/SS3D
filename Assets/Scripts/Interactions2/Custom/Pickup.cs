using UnityEngine;
using Interactions2.Core;
using Inventory;
using Inventory.Custom;

namespace Interactions2.Custom
{
    /**
     * <remarks>
     * Pick up an item from the ground. <br/>
     * This should be universal.
     * </remarks>
     * <inheritdoc cref="Core.Interaction"/>
     */
    [CreateAssetMenu(fileName = "Pickup", menuName = "Interactions2/Pickup")]
    public class Pickup : Core.InteractionSO
    {
        public override bool CanInteract(GameObject tool, GameObject target, RaycastHit at)
        {
            // TODO: Should also be within certain range.
            return target.GetComponent<Item>() != null
                && tool.transform.root.GetComponent<Hands>().GetItemInHand() == null;
        }

        public override void Interact(GameObject tool, GameObject target, RaycastHit at)
        {
            tool.transform.root.GetComponent<Hands>().Pickup(target);
        }
    }
}
