﻿using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public sealed class StoreInteraction : Interaction
    {
        private readonly AttachedContainer _attachedContainer;

        public StoreInteraction(AttachedContainer attachedContainer)
        {
            _attachedContainer = attachedContainer;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "Store in " + _attachedContainer.ContainerName;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : Assets.Get(InteractionIcons.Discard);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            IInteractionSource source = interactionEvent.Source;

            if(source is IGameObjectProvider sourceGameObjectProvider)
            {
                var hands = sourceGameObjectProvider.GameObject.GetComponentInParent<Hands>();
                if (hands != null && _attachedContainer != null)
                {
                    return !hands.SelectedHandEmpty && CanStore(interactionEvent.Source.GetComponent<Item>(), _attachedContainer);
                }
            }

            return false;
        }

        private bool CanStore(Item item, AttachedContainer target)
        {
            Container container = target.Container;
            return container.CanStoreItem(item) && container.CanHoldItem(item);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionSource source = interactionEvent.Source;
            if (source is IGameObjectProvider sourceGameObjectProvider)
            {
                var hands = sourceGameObjectProvider.GameObject.GetComponentInParent<Hands>();
                _attachedContainer.Container.AddItem(hands.ItemInHand);
                return true;
            }
            return false;
        }
    }
}