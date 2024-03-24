using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// The interaction to dispense a product on a VendingMachine.
    /// </summary>
    public class DispenseProductInteraction : Interaction
    {
        public string ProductName;
        public int ProductStock;
        public int ProductIndex;
        
        /// <inheritdoc />
        public override string GetName(InteractionEvent interactionEvent)
        {
            return $"Dispense {ProductName} (x{ProductStock})";
        }

        /// <inheritdoc />
        public override bool CanInteract(InteractionEvent interactionEvent)
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
        public override Sprite GetIcon(InteractionEvent interactionEvent)
        {
            return Icon != null ? Icon : InteractionIcons.Take;
        }

        /// <inheritdoc />
        public override bool Start(InteractionEvent interactionEvent, InteractionReference reference)
        {
            IInteractionTarget target = interactionEvent.Target;

            if (target is VendingMachine vendingMachine)
            {
                vendingMachine.DispenseProduct(ProductIndex);
            }
            return false;
        }
    }
}