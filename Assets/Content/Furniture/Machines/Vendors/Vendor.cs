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
    // TODO: Add the require component for AudioSource tag
    // Vendors are machines that can sell you stuff
    public class Vendor : InteractionTargetBehaviour
    {
        /// <summary>
        /// The point where items will be dispensed at
        /// </summary>
        public Transform EjectionPoint;
        /// <summary>
        /// The velocity at which the item is dispensed, should be able to be hacked
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

	// The stock that this vendor has, all it's products
        private VendorItem[] stock;

        private AudioSource audioSource;

	// A vendor item is a way to catalog what the machine has or hasnt
	// TODO: Add the item icon or name once we have a proper UI for it
        [Serializable]
        public class VendorItem
        {
	    // the item prefab
            public GameObject Prefab;
	    // how much we have of it
            public uint Stock;
	    // how much it costs, 0 if its free, remember to make the check later
            public int Price;
        }

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            var interaction = new SimpleInteraction
            {
                Name = "Dispense", CanInteractCallback = InteractionExtensions.RangeCheck, Interact = Dispense
            };
            return new IInteraction[] {interaction};
        }

	// Simple interaction to dispense items
        // TODO: Once we have a menu for selection, this should throw the selected VendorItem
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

	    // TODO: Move this after the item has been spawned, for consistency reasons
            // Decrease item stock
            item.Stock--;
            
            // Create item
            GameObject gameObject = Instantiate(item.Prefab, EjectionPoint.position, EjectionPoint.rotation);
	    // Spawn the item on the server
            NetworkServer.Spawn(gameObject);

	    // Adds a little bit of force to the dispensed item
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
