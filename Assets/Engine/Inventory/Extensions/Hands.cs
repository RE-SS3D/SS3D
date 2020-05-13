using System;
using System.Linq;
using Mirror;
using UnityEngine;

namespace SS3D.Engine.Inventory.Extensions
{
    [RequireComponent(typeof(Inventory))]
    public class Hands : NetworkBehaviour
    {
        [SerializeField] private Container handContainer = null;
        [SerializeField] private float handRange = 0f;

        public event Action<int> onHandChange;
        public int SelectedHand { get; private set; } = 0;

        // Use these for inventory actions
        public Container Container => handContainer;
        public GameObject ContainerObject => Container.gameObject;
        public int HeldSlot => handSlots[SelectedHand];

        [Server]
        public void Pickup(GameObject target)
        {
            if (GetItemInHand() == null)
            {
                inventory.AddItem(target, ContainerObject, HeldSlot);
            }
            else
            {
                Debug.LogWarning("Trying to pick up with a non-empty hand");
            }
        }

        /*
         * Command wrappers for inventory actions using the currently held item
         */
        [Server]
        public void DropHeldItem()
        {
            if (GetItemInHand() == null) return;

            var transform = GetItemInHand().transform;
            inventory.PlaceItem(ContainerObject, HeldSlot, transform.position);
        }
        [Server]
        public void PlaceHeldItem(Vector3 position) => inventory.PlaceItem(ContainerObject, HeldSlot, position);
        [Server]
        public void DestroyHeldItem() => inventory.DestroyItem(ContainerObject, HeldSlot);

        public Item GetItemInHand()
        {
            return handContainer.GetItem(HeldSlot);
        }

        /**
         * Attaches a container to the player's inventory.
         * Uses the ContainerAttachment component (on the server)
         * to ensure that the container is removed from the players inventory
         * when they get out of range.
         */
        [Command]
        private void CmdConnectContainer(GameObject containerObject)
        {
            Container container = containerObject.GetComponent<Container>();

            // If there's already an attachment, don't make another one
            var prevAttaches = GetComponents<ContainerAttachment>();
            if(prevAttaches.Any(attachment => attachment.container == container))
                return;

            var attach = gameObject.AddComponent<ContainerAttachment>();
            attach.inventory = GetComponent<Inventory>();
            attach.container = container;
            attach.range = handRange;
        }

        private void Awake()
        {
            inventory = GetComponent<Inventory>();

            // Find the indices in the hand container corresponding to the correct slots
            // Because we just make calls to GetSlot, which is set pre-Awake, this is safe.
            handSlots = new int[2] { -1, -1 };
            for (int i = 0; i < handContainer.Length(); ++i) {
                if (handContainer.GetSlot(i) == Container.SlotType.LeftHand)
                    handSlots[0] = i;
                else if (handContainer.GetSlot(i) == Container.SlotType.RightHand)
                    handSlots[1] = i;
            }
            if (handSlots[0] == -1 || handSlots[1] == -1)
                Debug.LogWarning("Player container does not contain slots for hands upon initialization. Maybe they were severed though?");

        }

        public override void OnStartClient()
        {
            handContainer.onChange += (a, b, c, d) =>
            {
                //UpdateTool()
            };
            if (handContainer.GetItems().Count > 0)
            {
                inventory.holdingSlot = new Inventory.SlotReference(handContainer, handSlots[SelectedHand]);
                //UpdateTool();
            }
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            // Hand-related buttons
            if (Input.GetButtonDown("Swap Active"))
            {
                SelectedHand = 1 - SelectedHand;
                inventory.holdingSlot = new Inventory.SlotReference(handContainer, handSlots[SelectedHand]);
                onHandChange?.Invoke(SelectedHand);
                CmdSetActiveHand(SelectedHand);
                //UpdateTool();
            }

            if (Input.GetButtonDown("Drop Item")) CmdDropHeldItem();
        }

        [Command]
        private void CmdDropHeldItem()
        {
            DropHeldItem();
        }

        [Command]
        private void CmdSetActiveHand(int selectedHand)
        {
            if (selectedHand >= 0 && selectedHand < handSlots.Length)
            {
                SelectedHand = selectedHand;
            }
            else
            {
                Debug.Log($"Invalid hand index {selectedHand}");   
            }
        }

        // The indices in the container that contains the hands
        private int[] handSlots;
        private Inventory inventory;
    }
}