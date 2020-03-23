using UnityEngine;
using SS3D.Engine.Interactions.Extensions;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;

namespace SS3D.Content.Systems.Interactions
{
    /**
     * <remarks>
     * Pick up an item from the ground. <br/>
     * This should be universal.
     * </remarks>
     * <inheritdoc cref="Core.Interaction"/>
     */
    [CreateAssetMenu(fileName = "Pickup", menuName = "Interactions2/Pickup")]
    public class Pickup : InteractionSO
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
