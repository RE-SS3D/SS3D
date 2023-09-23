using UnityEngine;
using SS3D.Systems.Inventory.Items;
using System.Linq;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using FishNet.Object;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// A hand is what an entity uses to grab and hold items, to interact with things in range.
    /// </summary>
    public class Hand : InteractionSource, IInteractionRangeLimit, IInteractionOriginProvider
    {
        public delegate void HandEventHandler(Hand hand);

        public event HandEventHandler OnHandDisabled;

        /// <summary>
        /// Container linked to this hand, necessary to hold stuff.
        /// </summary>
        public AttachedContainer Container;

        /// <summary>
        /// the hands script controlling this hand.
        /// </summary>
        public Hands HandsController;

        /// <summary>
        /// Horizontal and vertical max distance to interact with stuff.
        /// </summary>
        [SerializeField]
        private RangeLimit _range = new(1.5f, 2);

        /// <summary>
        /// Point from where distances for interaction is computed.
        /// </summary>
        [SerializeField]
        private Transform _interactionOrigin;

        /// <summary>
        /// The item held in this hand, if it exists
        /// </summary>
        public Item ItemInHand => Container.Items.FirstOrDefault();

        public Vector3 InteractionOrigin => _interactionOrigin.position;

        public bool IsEmpty()
		{
			return Container.Empty;
		}

        /// <summary>
        /// Get the interaction source from stuff in hand if there's any.
        /// Also sets the source of the IInteraction source to be this hand.
        /// </summary>
        public IInteractionSource GetActiveTool()
        {
            Item itemInHand = ItemInHand;
            if (itemInHand == null)
            {
                return null;
            }

            if (itemInHand.TryGetComponent<IInteractionSource>(out IInteractionSource interactionSource))
            {
                interactionSource.Source = this;
            }

            return interactionSource;
        }

        public RangeLimit GetInteractionRange()
        {
            return _range;
        }

        [Server]
        public void Pickup(Item item)
        {
            item.GiveOwnership(Owner);
            if (!IsEmpty())
            {
                return;
            }

			if (item.Container != null && item.Container != Container)
			{
				item.Container.RemoveItem(item);
			}

			Container.AddItem(item);
		}

        [ServerRpc]
        public void CmdDropHeldItem()
        {
            if (IsEmpty())
            {
                return;
            }

			Container.Dump();
            ItemInHand?.GiveOwnership(null);
		}

        /// <summary>
        /// Place item on the floor, or on any other surface, place it out of its container.
        /// </summary>
        [Server]
        public void PlaceHeldItemOutOfHand(Vector3 position, Quaternion rotation)
        {
            if (IsEmpty())
            {
                return;
            }

            ItemInHand.GiveOwnership(null);
            Item item = ItemInHand;
            item.Container.RemoveItem(item);
            ItemUtility.Place(item, position, rotation, transform);
        }

        /// <summary>
        /// Checks if the creature can interact with an object
        /// </summary>
        /// <param name="otherObject">The game object to interact with</param>
        public bool CanInteract(GameObject otherObject)
        {
            return GetInteractionRange().IsInRange(InteractionOrigin, otherObject.transform.position);
        }

        protected override void OnDisabled()
        {
            if (!IsServer)
            {
                return;
            }

            OnHandDisabled?.Invoke(this);
        }
    }
}
