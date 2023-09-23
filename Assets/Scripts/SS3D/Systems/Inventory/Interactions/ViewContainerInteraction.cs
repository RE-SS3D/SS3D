using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public class ViewContainerInteraction : Interaction
    {
        public readonly AttachedContainer AttachedContainer;

        public ViewContainerInteraction(AttachedContainer attachedContainer)
        {
            AttachedContainer = attachedContainer;
        }

        public float MaxDistance { get; set; }

        public override string GetName(InteractionEvent interactionEvent)
        {
            return "View " + AttachedContainer.ContainerName;
        }

        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : InteractionIcons.Open;
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

            ContainerViewer containerViewer = interactionEvent.Source.GetComponentInParent<ContainerViewer>();
            if (containerViewer == null)
            {
                return false;
            }

            Entity entity = interactionEvent.Source.GetComponentInParent<Entity>();
            if (entity == null)
            {
                return false;
            }

            return !containerViewer.HasContainer(AttachedContainer) && entity.GetComponent<Hands>().SelectedHand.CanInteract(AttachedContainer.gameObject);
        }

        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            ContainerViewer containerViewer = interactionEvent.Source.GetComponentInParent<ContainerViewer>();

            containerViewer.ShowContainerUI(AttachedContainer);

            return false;
        }
    }
}