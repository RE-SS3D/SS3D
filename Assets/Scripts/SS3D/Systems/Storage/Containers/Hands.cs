using System;
using System.Linq;
using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Storage.Items;
using SS3D.Systems.Storage.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Systems.Storage.Containers
{
    [RequireComponent(typeof(Inventory))]
    public class Hands : InteractionSourceNetworkBehaviour, IToolHolder, IInteractionRangeLimit, IInteractionOriginProvider
    {
        [SerializeField] public AttachedContainer[] HandContainers;
        [SerializeField] private float handRange;

        [NonSerialized]
        public Inventory Inventory;

        public int SelectedHandIndex { get; private set; }
        public RangeLimit range = new(1.5f, 1);
        // the origin of an x interaction that is performed is provided by this, we use it for range checks
        public Transform interactionOrigin;
        // pickup icon that this hand uses when there's a pickup interaction
        // TODO: When AssetData is on, we should update this to not use this
        public Sprite pickupIcon;
        /// <summary>
        /// Called when the active hand gets changed
        /// </summary>
        public event Action<int> OnHandChanged;
        /// <summary>
        /// The item held in the active hand
        /// </summary>
        public Item ItemInHand => SelectedHandContainer.Items.FirstOrDefault();
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

        public HandsView HandsView { get; private set; }

        protected override void OnAwake()
        {
            base.OnAwake();
            HandsView = FindObjectOfType<HandsView>(true);
            HandsView.Hands = this;

            SupportsMultipleInteractions = true;
        }

        [Server]
        public void Pickup(Item item)
        {
            if (!SelectedHandEmpty)
            {
                return;
            }

            if (item.Container != SelectedHandContainer && item.Container != null)
            {
                item.Container.RemoveItem(item);
            }

            SelectedHandContainer.AddItem(item);
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

            if (!IsOwner)
            {
                return;
            }

            // Hand-related buttons
            if (Input.GetButtonDown("Swap Hands") && HandContainers.Length > 0)
            {
                SelectedHandIndex = (SelectedHandIndex + 1) % HandContainers.Length;
                OnHandChanged?.Invoke(SelectedHandIndex);
                CmdSetActiveHand(SelectedHandIndex);
            }

            if (Input.GetButtonDown("Drop"))
            {
                CmdDropHeldItem();
            }
        }

        /// <summary>
        /// Set the Active hand of the Player to be the AttachedContainer passed in parameter.
        /// Do nothing if the parameter is the already active parameter.
        /// </summary>
        /// <param name="selectedContainer">This AttachedContainer should only be a hand.</param>
        public void SetActiveHand(AttachedContainer selectedContainer)
        {
            if (selectedContainer == SelectedHand)
            {
                return;
            }

            SelectedHandIndex = HandContainers.ToList().IndexOf(selectedContainer);
            if (SelectedHandIndex != -1)
            {
                OnHandChanged?.Invoke(SelectedHandIndex);
                CmdSetActiveHand(SelectedHandIndex);
            }
            else
            {
                Debug.LogError("selectedContainer is not in HandContainers.");
            }
        }

        [ServerRpc]
        private void CmdDropHeldItem()
        {
            DropHeldItem();
        }

        [ServerRpc]
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

            IInteractionSource interactionSource = itemInHand.Prefab.GetComponent<IInteractionSource>();
            if (interactionSource != null)
            {
                interactionSource.Source = this;
            }
            return interactionSource;
        }
        public RangeLimit GetInteractionRange()
        {
            return range;
        }

        /// <summary>
        /// Checks if the creature can interact with an object
        /// </summary>
        /// <param name="otherObject">The game object to interact with</param>
        public bool CanInteract(GameObject otherObject)
        {
            return GetInteractionRange().IsInRange(InteractionOrigin, otherObject.transform.position);
        }

        public Vector3 InteractionOrigin => interactionOrigin.position;
    }
}