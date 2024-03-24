using FishNet.Object;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Audio;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Furniture
{
    /// <summary>
    /// Simple implementation of a vending machine. This has many todos.
    ///
    /// TODO: Make proper UI for this.
    /// </summary>
    public class VendingMachine : InteractionSource, IInteractionTarget
    {
        /// <summary>
        /// The products available to dispense and their stock.
        /// </summary>
        [SerializeField]
        private VendingMachineProductStock[] _productsToDispense;

        /// <summary>
        /// The transform representation of where the dispensed products should spawn at.
        /// </summary>
        [SerializeField]
        private Transform _dispensingTransform;

        /// <summary>
        /// Requests the server to dispense a specific product.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CmdDispenseProduct(int productIndex)
        {
            DispenseProduct(productIndex);
        }

        /// <summary>
        /// Dispenses a specific vending machine product at the dispensing transform position with a random rotation.
        /// If there's not enough stock, a sound is played and the product isn't dispensed.
        /// </summary>
        [Server]
        public void DispenseProduct(int productIndex)
        {
            if (productIndex >= _productsToDispense.Length)
            {
                Debug.LogError($"Product with index {productIndex} not found in products to dispense in {gameObject.name}. Max possible index is {_productsToDispense.Length - 1}");
                return;
            }

            VendingMachineProductStock productToDispenseStock = _productsToDispense[productIndex];
            if (productToDispenseStock.Stock <= 0)
            {
                Subsystems.Get<AudioSystem>().PlayAudioSource(Audio.AudioType.Sfx, Sounds.BikeHorn, Position, NetworkObject,
                    false, 0.7f, 1, 1, 3);
                return;
            }

            productToDispenseStock.Stock--;
            Subsystems.Get<AudioSystem>().PlayAudioSource(Audio.AudioType.Sfx, Sounds.Can1, Position, NetworkObject,
                false, 0.7f, 1, 1, 3);
            
            ItemSystem itemSystem = Subsystems.Get<ItemSystem>();
            Quaternion quaternion = Quaternion.Euler(new Vector3(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)));

            itemSystem.SpawnItem(productToDispenseStock.Product.name, _dispensingTransform.position, quaternion);
        }

        /// <inheritdoc />
        public IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            IInteraction[] interactions = new IInteraction[_productsToDispense.Length];
            for (int i = 0; i < _productsToDispense.Length; i++)
            {
                interactions[i] = new DispenseProductInteraction
                {
                    ProductName = _productsToDispense[i].Product.NameString,
                    ProductIndex = i,
                    ProductStock = _productsToDispense[i].Stock,
                    Icon = InteractionIcons.Take
                };
            }

            return interactions;
        }
    }
}