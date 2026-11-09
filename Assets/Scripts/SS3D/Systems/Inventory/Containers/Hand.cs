using UnityEngine;
using SS3D.Systems.Combat;
using SS3D.Systems.Inventory.Items;
using System.Linq;
using SS3D.Interactions.Interfaces;
using SS3D.Interactions;
using FishNet.Object;
using SS3D.Systems.Tile;
using SS3D.Core;
using SS3D.Systems.Stamina;
using SS3D.Systems.Health;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Interactions;
using SS3D.Systems.Entities.Humanoid.Body;

namespace SS3D.Systems.Inventory.Containers
{
    public enum HandSide : byte
    {
        Right = 0,
        Left = 1,
    }

    /// <summary>
    /// A hand is what an entity uses to grab and hold items, to interact with things in range.
    /// </summary>
    public class Hand : InteractionSource, IInteractionRangeLimit, IInteractionOriginProvider
    {
        /// <summary>
        /// Container linked to this hand, necessary to hold stuff.
        /// </summary>
        public AttachedContainer Container;

        /// <summary>
        /// Horizontal and vertical max distance to interact with stuff.
        /// </summary>
        [SerializeField] private RangeLimit _range = new(1.5f, 2);

        // pickup icon that this hand uses when there's a pickup interaction
        // TODO: When AssetData is on, we should update this to not use this
        [SerializeField] private Sprite _pickupIcon;

        /// <summary>Left vs right — drives upper-body animation Mirror for Mixamo holds/swings.</summary>
        [SerializeField] private HandSide _side = HandSide.Right;

        /// <summary>
        /// Point from where distances for interaction is computed.
        /// </summary>
        [SerializeField] private Transform _interactionOrigin;

        /// <summary>
        /// the hands script controlling this hand.
        /// </summary>
        public Hands HandsController;

        public HandSide Side => _side;

        public bool CanUseHand
        {
            get
            {
                HumanHealthController health = GetComponentInParent<HumanHealthController>();
                Ragdoll ragdoll = GetComponentInParent<Ragdoll>();
                return (health == null || health.Snapshot.CanUseArms)
                    && (ragdoll == null || !ragdoll.IsKnockedDown);
            }
        }

        /// <summary>
        /// The item held in this hand, if it exists
        /// </summary>
        public Item ItemInHand => Container.Items.FirstOrDefault();

        public Vector3 InteractionOrigin => _interactionOrigin.position;

        public delegate void HandEventHandler(Hand hand);
        public event HandEventHandler OnHandDisabled;

        protected override void OnDisabled()
        {
            if (!IsServer)
            {
                return;
            }

            OnHandDisabled?.Invoke(this);
        }

		public bool IsEmpty()
		{
			return Container.Empty;
		}

        /// <summary>
        /// Get the interaction source from stuff in hand if there's any.
        /// Also sets the source of the IInteraction source to be this hand.
        /// </summary>
        /// <returns></returns>
        public IInteractionSource GetActiveTool()
        {
            Item itemInHand = ItemInHand;
            if (itemInHand == null)
            {
                return null;
            }

            IInteractionSource interactionSource = itemInHand.GetComponent<IInteractionSource>();
            if (interactionSource != null)
            {
                interactionSource.Source = this;
            }
            return interactionSource;
        }

        public RangeLimit GetInteractionRange()
        {
            return _range;
        }

        public override bool CanExecuteInteraction(IInteraction interaction)
        {
            StaminaController stamina = GetComponentInParent<StaminaController>();

            if (stamina != null && !stamina.CanCommenceInteraction)
            {
                return false;
            }

            if (!CanUseHand)
            {
                return false;
            }

            return base.CanExecuteInteraction(interaction);
        }

        public override bool CanContinueInteraction()
        {
            StaminaController stamina = GetComponentInParent<StaminaController>();

            return CanUseHand && (stamina == null || stamina.CanContinueInteraction);
        }

        [Server]
        public void Pickup(Item item)
        {
            if (!CanUseHand)
                return;

            Hands hands = HandsController;
            if (hands == null)
            {
                hands = GetComponentInParent<Hands>();
            }

            // Two-hand rifles auto-route to RequiredHand when the off-hand is active.
            if (!TwoHandedWeaponRules.TryResolveHandForItem(hands, item, this, out Hand target) || target == null)
            {
                return;
            }

            if (target.Container == null || !target.Container.CanContainItem(item))
            {
                return;
            }

            item.GiveOwnership(target.Owner);

			if (item.Container != null && item.Container != target.Container)
			{
				item.Container.RemoveItem(item);
			}

			target.Container.AddItem(item);
		}

        [ServerRpc]
        public void CmdDropHeldItem()
        {
            if (IsEmpty())
            {
                return;
            }

            InteractionController interactions = GetComponentInParent<InteractionController>();
            if (interactions == null)
            {
                DumpHeldItem();
                return;
            }

            // The item leaves the hand when the put-down reaches the floor (animation pass Phase 4).
            Item item = ItemInHand;
            interactions.ServerRunHandAction(
                InteractionAnimation.FloorPutDown,
                InteractionAnimation.DropPointAhead(interactions.transform),
                () => ItemInHand == item,
                DumpHeldItem);
		}

        [Server]
        private void DumpHeldItem()
        {
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
                return;
            
            ItemInHand.GiveOwnership(null);
            Item item = ItemInHand;
            item.Container.RemoveItem(item);
            ItemUtility.Place(item, position, rotation);

            ItemObjectSo itemObjectSo = SubSystems.Get<TileSubSystem>().GetAsset(item.Asset) as ItemObjectSo;
            if (itemObjectSo != null)
            {
                // Pass the lifted pose from ItemUtility.Place; the raw surface point sinks the item.
                SubSystems.Get<TileSubSystem>().Construction.TryPlaceItem(
                    itemObjectSo,
                    item.transform.position,
                    item.transform.rotation,
                    item.gameObject);
            }
        }

        /// <summary>
        /// Checks if the creature can interact with an object
        /// </summary>
        /// <param name="otherObject">The game object to interact with</param>
        public bool CanInteract(GameObject otherObject)
        {
            return GetInteractionRange().IsInRange(InteractionOrigin, otherObject.transform.position);
        }
    }
}
