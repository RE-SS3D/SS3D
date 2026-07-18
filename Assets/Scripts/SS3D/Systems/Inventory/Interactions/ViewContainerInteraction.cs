using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public class ViewContainerInteraction : IInteraction, IClientInteractionSource
    {
        public string Name;
        public Sprite Icon;

        public float MaxDistance { get; set; }

        public readonly AttachedContainer AttachedContainer;

        public int Priority => 25;

        public ViewContainerInteraction(AttachedContainer attachedContainer)
        {
            AttachedContainer = attachedContainer;
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "View " + AttachedContainer.ContainerName;
        }

        public string GetGenericName() => "View:" + AttachedContainer.ContainerName;

        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : Assets.Get<Sprite>(AssetDatabases.InteractionIcons, InteractionIcons.Open);
        }

        public bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent))
            {
                return false;
            }

            if (AttachedContainer == null)
            {
                return false;
            }

            if (!AllowsStorageAccess(AttachedContainer))
            {
                return false;
            }

            var containerViewer = interactionEvent.Source.GetComponentInParent<ContainerViewer>();

            if (containerViewer == null)
            {
                return false;
            }

            Entity entity = interactionEvent.Source.GetComponentInParent<Entity>();

            if (entity == null)
            {
                return false;
            }

            if (!AttachedContainer.IsAccessibleBy(interactionEvent.Source.GetComponentInParent<HumanInventory>()))
            {
                return false;
            }

            return !containerViewer.HasContainer(AttachedContainer) && entity.GetComponent<Hands>().SelectedHand.CanInteract(AttachedContainer.gameObject);
        }

        private static bool AllowsStorageAccess(AttachedContainer container)
        {
            IStorageAccessGate gate = container.GetComponentInParent<IStorageAccessGate>();
            return gate == null || gate.AllowsStorageAccess;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            var containerViewer = interactionEvent.Source.GetComponentInParent<ContainerViewer>();

            containerViewer.ShowContainerUI(AttachedContainer);

            return false;
        }
    }
}