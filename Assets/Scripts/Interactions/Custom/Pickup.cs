using UnityEngine;
using Interactions.Core;
using Inventory;
using Inventory.Custom;

namespace Interactions.Custom
{
    /**
     * <remarks>
     * Pick up an item from the ground. <br/>
     * This should be universal.
     * </remarks>
     * <inheritdoc cref="Core.Interaction"/>
     */
    [CreateAssetMenu(fileName = "Pickup", menuName = "Interactions2/Pickup")]
    public class Pickup : Core.Extensions.InteractionSO
    {
        public override string Name => "Pick Up";

        public override bool CanInteract()
        {
            // TODO: Should also be within certain range.
            return Event.target.GetComponent<Item>() != null
                && Event.Player.GetComponent<Hands>().GetItemInHand() == null;
        }

        public override void Interact()
        {
            Event.Player.GetComponent<Hands>().Pickup(Event.target);
        }
    }
}
