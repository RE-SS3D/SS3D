using UnityEngine;
using Inventory;
using Inventory.Custom;

namespace Interactions2.Custom
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
    public class Storeable : Core.InteractionComponent
    {
        /// <summary>Only allows storing when this object is open. Assumes that this object has Openable</summary>
        [SerializeField]
        private bool onlyWhenOpen = false;

        public override bool CanInteract(GameObject tool, GameObject target, RaycastHit at)
        {
            // TODO: Should also be within certain range.
            return target == gameObject && CanStore() && tool.transform.root.GetComponent<Hands>().GetItemInHand() != null;
        }

        public override void Interact(GameObject tool, GameObject target, RaycastHit at)
        {
            var hands = tool.transform.root.GetComponent<Hands>();
            tool.transform.root.GetComponent<Inventory.Inventory>().MoveItem(hands.ContainerObject, hands.HeldSlot, target);
        }

        private bool CanStore()
        {
            if(onlyWhenOpen)
                return GetComponent<Openable>()?.Open ?? false;
            return true;
        }
    }
}
