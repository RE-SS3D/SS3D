﻿using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public class ViewContainerInteraction : Interaction
    {
        public float MaxDistance { get; set; }

        public readonly AttachedContainer AttachedContainer;

        public ViewContainerInteraction(AttachedContainer attachedContainer)
        {
            AttachedContainer = attachedContainer;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "View " + AttachedContainer.ContainerName;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : Assets.Get(InteractionIcons.Open);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            if (AttachedContainer == null)
            {
                return false;
            }

            var inventory = interactionEvent.Source.GetComponentInTree<Containers.Inventory>();
            if (inventory == null)
            {
                return false;
            }

            Entity entity = interactionEvent.Source.GetComponentInTree<Entity>();
            if (entity == null)
            {
                return false;
            }
            return !inventory.HasContainer(AttachedContainer) && entity.GetComponent<Hands>().CanInteract(AttachedContainer.gameObject);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            var inventory = interactionEvent.Source.GetComponentInTree<Containers.Inventory>();

            inventory.OpenContainer(AttachedContainer);

            return false;
        }
    }
}