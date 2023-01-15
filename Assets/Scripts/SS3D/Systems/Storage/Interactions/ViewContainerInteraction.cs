using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Entities;
using SS3D.Systems.Storage.Containers;
using UnityEngine;

namespace SS3D.Systems.Storage.Interactions
{
    public class ViewContainerInteraction : Interaction
    {
        public float MaxDistance { get; set; }

        public readonly ContainerDescriptor ContainerDescriptor;

        public ViewContainerInteraction(ContainerDescriptor containerDescriptor)
        {
            ContainerDescriptor = containerDescriptor;
        }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "View " + ContainerDescriptor.ContainerName;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : AssetData.Get(InteractionIcons.Open);
        }

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            AttachedContainer container = ContainerDescriptor.AttachedContainer;
            if (container == null)
            {
                return false;
            }

            Inventory inventory = interactionEvent.Source.GetComponentInTree<Inventory>();
            if (inventory == null)
            {
                return false;
            }

            PlayerControllable entity = interactionEvent.Source.GetComponentInTree<PlayerControllable>();
            if (entity == null)
            {
                return false;
            }
            return !inventory.HasContainer(container) && entity.GetComponent<Hands>().CanInteract(container.gameObject);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Inventory inventory = interactionEvent.Source.GetComponentInTree<Inventory>();
            AttachedContainer attachedContainer = ContainerDescriptor.AttachedContainer;

            inventory.OpenContainer(attachedContainer);

            return false;
        }
    }
}