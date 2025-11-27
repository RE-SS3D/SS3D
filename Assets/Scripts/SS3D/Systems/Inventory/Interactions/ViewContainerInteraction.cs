using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.UI;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    public class ViewContainerInteraction : ContinuousInteraction
    {
        private readonly AttachedContainer _attachedContainer;

        public ViewContainerInteraction(AttachedContainer attachedContainer)
        {
            _attachedContainer = attachedContainer;
        }

        public override InteractionType InteractionType => InteractionType.None;

        public float MaxDistance { get; set; }

        public override string GetGenericName() => "View Container";

        public override IClientInteraction CreateClient(InteractionEvent interactionEvent) => null;

        public override string GetName(InteractionEvent interactionEvent) => "View " + _attachedContainer.ContainerName;

        public override Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIcons.Open;

        public override bool CanInteract(InteractionEvent interactionEvent)
        {
            if (!InteractionExtensions.RangeCheck(interactionEvent) || _attachedContainer == null)
            {
                return false;
            }

            return !ViewLocator.Get<ContainerView>()[0].ContainerIsDisplayed(_attachedContainer);
        }

        public override void Cancel(InteractionEvent interactionEvent, InteractionReference reference)
        {
            ContainerView containerView = ViewLocator.Get<ContainerView>()[0];
            containerView.RpcCloseContainer(interactionEvent.Source.NetworkObject.Owner, _attachedContainer);
        }

        protected override bool CanKeepInteracting(InteractionEvent interactionEvent, InteractionReference reference)
        {
            return InteractionExtensions.RangeCheck(interactionEvent) && _attachedContainer != null;
        }

        protected override bool StartImmediately(InteractionEvent interactionEvent, InteractionReference reference)
        {
            ContainerView containerView = ViewLocator.Get<ContainerView>()[0];
            containerView.RpcOpenContainer(interactionEvent.Source.NetworkObject.Owner, _attachedContainer, interactionEvent.Source.NetworkObject);
            return true;
        }

        protected override void StartDelayed(InteractionEvent interactionEvent, InteractionReference reference)
        {
        }
    }
}
