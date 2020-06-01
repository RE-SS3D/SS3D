using System;
using System.Linq;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using SS3D.Engine.Interactions.Extensions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SS3D.Content.Furniture.Machines.Vendors
{
    public class Vendor : InteractionTargetBehaviour
    {
        /// <summary>
        /// The point where items will be dispensed at
        /// </summary>
        public Transform EjectionPoint;
        /// <summary>
        /// The velocity at which the item is launched
        /// </summary>
        public Vector3 EjectionVelocity;
        /// <summary>
        /// The items in this vendor
        /// </summary>
        public VendorItem[] Stock
        {
            get => stock;
            set => stock = value;
        }
        [SerializeField]
        private VendorItem[] stock;

        private AudioSource audioSource;

        [Serializable]
        public class VendorItem
        {
            public GameObject Prefab;
            public uint Stock;
            public int Price;
        }

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public override IInteraction[] GenerateInteractions(InteractionEvent interactionEvent)
        {
            var interaction = new SimpleInteraction
            {
                Name = "Dispense", CanInteractCallback = InteractionExtensions.RangeCheck, Interact = Dispense
            };
            return new IInteraction[] {interaction};
        }

        private void Dispense(InteractionEvent interactionEvent, InteractionReference reference)
        {
            VendorItem item = null;
            // Find random item with at least 1 in stock
            bool[] checkedSlots = new bool[stock.Length];
            do
            {
                int i = Random.Range(0, stock.Length);
                item = stock[i];
                checkedSlots[i] = true;
            } while (!checkedSlots.All(x => x) && item.Stock < 1);

            if (item.Stock < 1)
            {
                // No more items left :(
                return;
            }

            // Decrease item stock
            item.Stock--;
            
            // Create item
            GameObject gameObject = Instantiate(item.Prefab, EjectionPoint.position, EjectionPoint.rotation);
            NetworkServer.Spawn(gameObject);
            Rigidbody body = gameObject.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.velocity = transform.TransformDirection(EjectionVelocity);
            }
            
            // Play audio cue
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }
    }
}
