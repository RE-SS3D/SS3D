using System.Linq;
using Mirror;
using UnityEngine;

namespace Inventory.Custom
{
    [RequireComponent(typeof(Inventory))]
    public class Hands : NetworkBehaviour
    {
        public delegate void OnHandChange(int selectedHand);

        [SerializeField] private Container handContainer = null;
        [SerializeField] private float handRange = 0f;

        [System.NonSerialized]
        public int selectedHand = 0;
        public event OnHandChange onHandChange;
        
        public void Pickup(GameObject target)
        {
            if (GetItemInHand() == null)
            {
                inventory.CmdAddItem(target, handContainer.gameObject, handSlots[selectedHand]);
            }
            else
            {
                Debug.LogWarning("Trying to pick up with a non-empty hand");
            }
        }
        
        public void Drop()
        {
            var transform = GetItemInHand().transform;
            inventory.CmdPlaceItem(handContainer.gameObject, handSlots[selectedHand], transform.position, transform.rotation);
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
        }
        public override void OnStartClient()
        {
            // Find the indices in the hand container corresponding to the correct slots
            handSlots = new int[2] { -1, -1 };
            for (int i = 0; i < handContainer.Length(); ++i)
            {
                if (handContainer.GetSlot(i) == Container.SlotType.LeftHand)
                    handSlots[0] = i;
                else if (handContainer.GetSlot(i) == Container.SlotType.RightHand)
                    handSlots[1] = i;
            }
            if (handSlots[0] == -1 || handSlots[1] == -1)
                Debug.LogWarning("Player container does not contain slots for hands upon initialization. Maybe they were severed though?");

            handContainer.onChange += (a, b, c) =>
            {
                //UpdateTool()
            };
            if (handContainer.GetItems().Count > 0)
            {
                inventory.holdingSlot = new Inventory.SlotReference(handContainer, handSlots[selectedHand]);
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
                selectedHand = 1 - selectedHand;
                inventory.holdingSlot = new Inventory.SlotReference(handContainer, handSlots[selectedHand]);
                onHandChange?.Invoke(selectedHand);
            
                //UpdateTool();
            }
            
            if (Input.GetButtonDown("Drop Item")) Drop();
        }

        public Item GetItemInHand()
        {
            return handContainer.GetItem(handSlots[selectedHand]);
        }

        // The indices in the container that contains the hands
        private int[] handSlots;
        private Inventory inventory;
    }
}
