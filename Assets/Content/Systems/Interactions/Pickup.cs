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

        public float pickupDistance = 1.5f;

        public override bool CanInteract()
        {
            // Below are some failure conditions for interactions:

            if (Event.target.GetComponent<Item>() == null)
            {
                return false;
            }

            if (Event.Player.GetComponent<Hands>().GetItemInHand() != null)
            {
                return false;
            }

            if (Vector3.Distance(Event.Player.transform.position, Event.target.transform.position) > pickupDistance)
            {
                return false;
            }

            return true;
        }

        public override void Interact()
        {
            Event.Player.GetComponent<Hands>().Pickup(Event.target);
        }
    }
}
