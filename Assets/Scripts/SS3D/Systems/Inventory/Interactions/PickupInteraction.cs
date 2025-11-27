using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.PlayerControl;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    // A pickup interaction is when you pick an item and
    // add it into a container (in this case, the hands)
    // you can only pick things that are not in a container
    public sealed class PickupInteraction : DelayedInteraction
    {
        private bool _hasItemInHand;

        public PickupInteraction(float timeToMoveBackItem, float timeToReachItem)
        {
            TimeToMoveBackItem = timeToMoveBackItem;
            TimeToReachItem = timeToReachItem;
            Delay = TimeToReachItem;
        }

        public override InteractionType InteractionType => InteractionType.Pickup;

        private float TimeToMoveBackItem { get; }

        private float TimeToReachItem { get; }

        [NotNull]
        public override string GetName(InteractionEvent interactionEvent) => "Pick up";

        [NotNull]
        public override string GetGenericName() => "Pickup";

        public override Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Take;

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            if (target is not IGameObjectProvider targetBehaviour || source is not IContainerProvider containerProvider || source is not IItemHolder itemHolder)
            {
                return false;
            }

            // check that the item is within range
            bool isInRange = InteractionExtensions.RangeCheck(interactionEvent);

            if (!isInRange)
            {
                return false;
            }

            // try to get the Item component from the GameObject we just interacted with
            // you can only pickup items (for now, TODO: we have to consider people too), which makes sense
            Item item = targetBehaviour.GameObject.GetComponent<Item>();

            if (!item)
            {
                return false;
            }

            // check that our hand is empty
            if (!itemHolder.Empty)
            {
                return false;
            }

            // check the item is not in a container
            return !item.IsInContainer || item.Container == containerProvider.Container;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            // We don't want to cancel the interaction if the item is already in hand
            if (_hasItemInHand)
            {
                return;
            }

            if (interactionEvent.Source is IInteractionSourceAnimate hand)
            {
                hand.CancelSourceAnimation(InteractionType.Pickup, interactionEvent.Target.GetComponent<NetworkObject>(), TimeToMoveBackItem + TimeToReachItem);
            }
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            _hasItemInHand = true;
            IInteractionTarget target = interactionEvent.Target;
            IInteractionSource source = interactionEvent.Source;

            if (target is IGameObjectProvider targetBehaviour && source is IContainerProvider hand)
            {
                Item item = targetBehaviour.GameObject.GetComponent<Item>();
                hand.Container.AddItem(item);
            }
        }

        protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
        {
            // remember that when we call this Start, we are starting the interaction per se
            // so we check if the source of the interaction is a Hand, and if the target is an Item
            if (interactionEvent.Source is IInteractionSourceAnimate hand && interactionEvent.Source is NetworkBehaviour networkBehaviour && interactionEvent.Target is Item target)
            {
                target.GiveOwnership(networkBehaviour.Owner);
                hand.PlaySourceAnimation(InteractionType.Pickup, target.GetComponent<NetworkObject>(), target.transform.position, TimeToMoveBackItem + TimeToReachItem);

                try
                {
                    string ckey = Subsystems.Get<PlayerSubSystem>().GetCkey(networkBehaviour.Owner);

                    // and call the event for picking up items for the Game Mode SubSystem
                    new ItemPickedUpEvent(target, ckey).Invoke(this);
                }
                catch
                {
                    Debug.Log("Couldn't get Player Ckey");
                }
            }

            return true;
        }
    }
}
