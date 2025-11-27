using FishNet.Object;
using JetBrains.Annotations;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    // a drop interaction is when we remove an item from the hand
    [Serializable]
    public class PlaceInteraction : DelayedInteraction
    {
        /// <summary>
        /// The maximum angle of surface the item will allow being dropped on
        /// </summary>
        private float _maxSurfaceAngle = 10;

        /// <summary>
        /// Only raycast the default layer for seeing if we are vision blocked
        /// </summary>
        private LayerMask _defaultMask = LayerMask.GetMask("Default");

        public PlaceInteraction(float timeToMoveBackHand, float timeToReachDropPlace)
        {
            TimeToReachDropPlace = timeToReachDropPlace;
            TimeToMoveBackHand = timeToMoveBackHand;
            Delay = TimeToReachDropPlace;
        }

        public float TimeToMoveBackHand { get; private set; }

        public float TimeToReachDropPlace { get; private set; }

        public override InteractionType InteractionType => InteractionType.Place;

        [NotNull]
        public override string GetName(InteractionEvent interactionEvent) => "Place";

        [NotNull]
        public override string GetGenericName() => "Place";

        public override Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Discard;

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            // If item is not in hand return false
            if (interactionEvent.Source.GetRootSource() is not IItemHolder)
            {
                return false;
            }

            // Confirm the entities ViewPoint can see the drop point
            Vector3 direction = (interactionEvent.Point - interactionEvent.Source.GameObject.transform.position).normalized;
            bool raycast = Physics.Raycast(interactionEvent.Source.GameObject.transform.position, direction, out RaycastHit hit, Mathf.Infinity, _defaultMask);
            if (!raycast)
            {
                return false;
            }

            // Consider if the surface is facing up
            float angle = Vector3.Angle(interactionEvent.Normal, Vector3.up);
            if (angle > _maxSurfaceAngle)
            {
                return false;
            }

            bool rangeCheck = InteractionExtensions.RangeCheck(interactionEvent);

            return rangeCheck;
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source.GetRootSource() is IItemHolder itemHolder && itemHolder.ItemHeld && interactionEvent.Source.GetRootSource() is IInteractionSourceAnimate itemHolderAnimated)
            {
                itemHolderAnimated.CancelSourceAnimation(InteractionType.Place, interactionEvent.Target.GetComponent<NetworkObject>(), TimeToMoveBackHand + TimeToReachDropPlace);
            }
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source.GetRootSource() is not IItemHolder hand)
            {
                return;
            }

            Item item = hand.ItemHeld;
            item.Container.RemoveItem(item);
            item.GiveOwnership(null);
            item.transform.parent = null;
        }

        protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
        {
            if (interactionEvent.Source.GetRootSource() is not IInteractionSourceAnimate hand)
            {
                return true;
            }

            hand.PlaySourceAnimation(InteractionType.Place, interactionEvent.Source.GetComponent<NetworkObject>(), interactionEvent.Point, TimeToMoveBackHand + TimeToReachDropPlace);
            return true;
        }
    }
}
