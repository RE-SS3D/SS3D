using UnityEngine;
using SS3D.Engine.Inventory;
using SS3D.Engine.Inventory.Extensions;
using SS3D.Engine.Interactions;

namespace SS3D.Content.Code.Interactions
{
    /**
     * <summary>
     * Stores the held item into a container, if that container supports instant-storage. <br />
     * Attach to the target.
     * </summary>
     * <remarks>
     * The container attached must be able to handle <see cref="Container.AddItem(GameObject)"/>.
     * </remarks>
     * <inheritdoc cref="Core.Interaction"/>
     */
    [RequireComponent(typeof(Container))]
    public class Storeable : MonoBehaviour, Interaction
    {
        /// <summary>Only allows storing when this object is open. Assumes that this object has Openable</summary>
        [SerializeField]
        private bool onlyWhenOpen = false;

        public InteractionEvent Event { get; set; }
        public string Name => "Store";

        public bool CanInteract()
        {
            // TODO: Should also be within certain range.
            playerHands = Event.Player.GetComponent<Hands>();
            return Event.target == gameObject && CanStore() && playerHands.GetItemInHand() != null;
        }

        public void Interact()
        {
            Event.Player.GetComponent<Engine.Inventory.Inventory>().MoveItem(playerHands.ContainerObject, playerHands.HeldSlot, Event.target);
        }

        private bool CanStore()
        {
            if(onlyWhenOpen)
                return GetComponent<Openable>()?.Open ?? false;
            return true;
        }

        // Set in CanInteract
        private Hands playerHands;
    }
}
