using JetBrains.Annotations;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Inventory.Interactions
{
    // This Interaction takes the first available item inside a container
    public sealed class TakeFirstInteraction : IInteraction, IClientInteractionSource
    {
        private static AssetHandle<Sprite> DefaultIconHandle;
        private static bool TryingToLoadIcon;

        public string Name;
        public Sprite Icon;
        private readonly AttachedContainer _attachedContainer;

        public TakeFirstInteraction(AttachedContainer attachedContainer)
        {
            _attachedContainer = attachedContainer;

            if (!TryingToLoadIcon || !DefaultIconHandle)
            {
                AcquireDefaultIcon();
            }
        }

        public string GetName(InteractionEvent interactionEvent)
        {
            return "Take in " + _attachedContainer.ContainerName;
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

            // Will only appear if the current hand is empty and the container isn't empty
            if (interactionEvent.Source is Hand hand && _attachedContainer != null)
            {
                return hand.IsEmpty() && !_attachedContainer.Empty;
            }

            return false;
        }

        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            Hand hand = (Hand)interactionEvent.Source;

            Item pickupItem = _attachedContainer.Items.First();

            if (pickupItem != null)
            {
                hand.Pickup(pickupItem);
            }

            return false;
        }

        private static async void AcquireDefaultIcon()
        {
            TryingToLoadIcon = true;
            DefaultIconHandle = await new AssetRequest<Sprite>(InteractionIcons.Take).ExecuteAsync();

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