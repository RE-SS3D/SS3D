using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// The interaction to dispense a product on a VendingMachine.
    /// </summary>
    public class DispenseProductInteraction : IInteraction, IClientInteractionSource
    {
        private static AssetHandle<Sprite> DefaultIconHandle;
        private static bool TryingToLoadIcon;
        
        public string Name;
        public Sprite Icon;
        public string ProductName;
        public int ProductStock;
        public int ProductIndex;
        
        public DispenseProductInteraction()
        {
            if (!TryingToLoadIcon && !DefaultIconHandle)
            {
                AcquireDefaultIcon();
            }
        }

        /// <inheritdoc />
        public string GetName(InteractionEvent interactionEvent)
        {
            return $"Dispense {ProductName} (x{ProductStock})";
        }

        public string GetGenericName() => throw new System.NotImplementedException();

        /// <inheritdoc />
        public bool CanInteract(InteractionEvent interactionEvent)
        {
            IInteractionTarget target = interactionEvent.Target;

            bool inRange = InteractionExtensions.RangeCheck(interactionEvent);

            if (!inRange)
            {
                return false;
            }

            return target is VendingMachine;
        }

        /// <inheritdoc />
        public Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon ? Icon : DefaultIconHandle?.Asset;
        }

        /// <inheritdoc />
        public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionTarget target = interactionEvent.Target;

            if (target is VendingMachine vendingMachine)
            {
                vendingMachine.DispenseProduct(ProductIndex);
            }

            return false;
        }
        
        private static async void AcquireDefaultIcon()
        {
            TryingToLoadIcon = true;
            DefaultIconHandle = await new AssetRequest<Sprite>(InteractionIcons.Take).LoadAsync();

            if (!DefaultIconHandle)
            {
                AssetHandle.Release(ref DefaultIconHandle);
            }
            else
            {
                UnityEngine.Application.quitting += OnApplicationQuit;
            }

            TryingToLoadIcon = false;
        }

        private static void OnApplicationQuit()
        {
            AssetHandle.Release(ref DefaultIconHandle);
            UnityEngine.Application.quitting -= OnApplicationQuit;
        }
    }
}