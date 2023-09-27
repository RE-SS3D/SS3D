using FishNet.Object;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Enums;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Simple implementation of a vending machine. This has many todos.
    ///
    /// TODO: Make proper UI for this.
    /// TODO: Dispensing products should reduce their stock.
    /// </summary>
    public class VendingMachine : InteractionSource, IInteractionTarget
    {
        /// <summary>
        /// The product to dispense.
        /// TODO: Make a new struct for different products, and to support multiple products.
        /// </summary>
        [SerializeField]
        private GameObject _productToDispense;

        /// <summary>
        /// The transform representation of where the dispensed products should spawn at.
        /// </summary>
        [SerializeField]
        private Transform _dispensingTransform;

        /// <summary>
        /// Requests the server to dispense a product.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CmdDispenseProduct()
        {
            DispenseProduct();
        }

        /// <summary>
        /// Dispenses the vending machine product at the dispensing transform position with a random rotation.
        /// </summary>
        [Server]
        public void DispenseProduct()
        {
            ItemSystem itemSystem = Subsystems.Get<ItemSystem>();
            Quaternion quaternion = Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));

            itemSystem.SpawnItem(_productToDispense.name, _dispensingTransform.position, quaternion);
        }

        /// <inheritdoc />
        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[]
            {
                new DispenseProductInteraction
                {
                    Icon = InteractionIcons.Take
                }
            };
        }
    }
}