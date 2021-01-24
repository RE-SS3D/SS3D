using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using SS3D.Engine.Interactions;
using UnityEngine.Serialization;

namespace SS3D.Engine.Inventory.Extensions
{
    [RequireComponent(typeof(Inventory))]
    public class Hands : InteractionSourceNetworkBehaviour, IToolHolder, IInteractionRangeLimit, IInteractionOriginProvider
    {
        [SerializeField] public AttachedContainer[] HandContainers;
        [SerializeField] private float handRange;

        [NonSerialized]
        public Inventory Inventory;
        public int SelectedHandIndex { get; private set; }
        public RangeLimit range = new RangeLimit(1.5f, 1);
        public Transform interactionOrigin;

        public Sprite pickupIcon;

        /// <summary>
        /// Called when the active hand gets changed
        /// </summary>
        public event Action<int> HandChanged;
        
        /// <summary>
        /// The item held in the active hand
        /// </summary>
        public Item ItemInHand => SelectedHandContainer?.Items.FirstOrDefault();
        
        /// <summary>
        /// The currently active hand
        /// </summary>
        public AttachedContainer SelectedHand => SelectedHandIndex < HandContainers.Length ? HandContainers[SelectedHandIndex] : null;
        
        /// <summary>
        /// The container of the currently active hand
        /// </summary>
        public Container SelectedHandContainer => SelectedHand != null ? SelectedHand.Container : null;

        /// <summary>
        /// If the selected hand is empty
        /// </summary>
        public bool SelectedHandEmpty => SelectedHandContainer.Empty;

        public void Start()
        {
            SupportsMultipleInteractions = true;
            
            // Initialize hand containers
            foreach (AttachedContainer attachedContainer in HandContainers)
            {
                attachedContainer.Container = new Container
                {
                    Size = new Vector2Int(5, 5)
                };
            }
        }

        [Server]
        public void Pickup(Item item)
        {
            if (SelectedHandEmpty)
            {
                SelectedHandContainer.AddItem(item);
            }
        }

        public bool IsEmpty()
        {
            return SelectedHandContainer.Empty;
        }

        /*
         * Command wrappers for inventory actions using the currently held item
         */
        [Server]
        public void DropHeldItem()
        {
            if (SelectedHandEmpty)
            {
                return;
            }
            
            SelectedHandContainer.Dump();
        }

        [Server]
        public void PlaceHeldItem(Vector3 position, Quaternion rotation)
        {
            if (SelectedHandEmpty)
            {
                return;
            }

            Item item = ItemInHand;
            item.Container = null;
            ItemUtility.Place(item, position, rotation, transform);
        }

        public override void Update()
        {
            base.Update();
            
            if (!isLocalPlayer)
                return;

            // Hand-related buttons
            if (Input.GetButtonDown("Swap Active") && HandContainers.Length > 0)
            {
                SelectedHandIndex = (SelectedHandIndex + 1) % HandContainers.Length;
                HandChanged?.Invoke(SelectedHandIndex);
                CmdSetActiveHand(SelectedHandIndex);
            }

            if (Input.GetButtonDown("Drop Item"))
            {
                CmdDropHeldItem();
            }
        }

        [Command]
        private void CmdDropHeldItem()
        {
            DropHeldItem();
        }

        [Command]
        private void CmdSetActiveHand(int selectedHand)
        {
            if (selectedHand >= 0 && selectedHand < HandContainers.Length)
            {
                SelectedHandIndex = selectedHand;
            }
            else
            {
                Debug.Log($"Invalid hand index {selectedHand}");   
            }
        }

        public IInteractionSource GetActiveTool()
        {
            Item itemInHand = ItemInHand;
            if (itemInHand == null)
            {
                return null;
            }

            IInteractionSource interactionSource = itemInHand.prefab.GetComponent<IInteractionSource>();
            if (interactionSource != null)
            {
                interactionSource.Parent = this;
            }
            return interactionSource;
        }
        public RangeLimit GetInteractionRange()
        {
            return range;
        }

        public Vector3 InteractionOrigin => interactionOrigin.position;
    }
}