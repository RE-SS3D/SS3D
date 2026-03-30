using JetBrains.Annotations;
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
        private static AssetHandle<Sprite> DefaultIconHandle;
        private static bool TryingToLoadIcon;

        public string Name;
        public Sprite Icon;

        public float MaxDistance { get; set; }

        public readonly AttachedContainer AttachedContainer;

        public ViewContainerInteraction(AttachedContainer attachedContainer)
        {
            AttachedContainer = attachedContainer;

            if (!TryingToLoadIcon && !DefaultIconHandle)
            {
                AcquireDefaultIcon();
            }
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "View " + AttachedContainer.ContainerName;
        }

        public string GetGenericName() => throw new System.NotImplementedException();

        [CanBeNull]
        public Sprite GetIcon(InteractionEvent interactionEvent) => Icon ? Icon : DefaultIconHandle?.Asset;

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

            return !containerViewer.HasContainer(AttachedContainer) && entity.GetComponent<Hands>().SelectedHand.CanInteract(AttachedContainer.gameObject);
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            var containerViewer = interactionEvent.Source.GetComponentInParent<ContainerViewer>();

            containerViewer.ShowContainerUI(AttachedContainer);

            return false;
        }

        private static async void AcquireDefaultIcon()
        {
            TryingToLoadIcon = true;
            DefaultIconHandle = await new AssetRequest<Sprite>(InteractionIcons.Open).ExecuteAsync();

            if (DefaultIconHandle)
            {
                UnityEngine.Application.quitting += OnApplicationQuit;
            }
            else
            {
                ReleaseDefaultIcon();
            }

            TryingToLoadIcon = false;
        }

        private static void OnApplicationQuit()
        {
            ReleaseDefaultIcon();
            UnityEngine.Application.quitting -= OnApplicationQuit;
        }

        private static void ReleaseDefaultIcon()
        {
            DefaultIconHandle?.Dispose();
            DefaultIconHandle = null;
        }
    }
}