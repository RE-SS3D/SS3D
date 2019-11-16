using UnityEngine;
using UnityEngine.EventSystems;

/**
 * The hand system is an interaction system which connects to the inventory.
 * There is a hands container, that the interaction system connects to, that defines what items the player is currently holding.
 */
[RequireComponent(typeof(Inventory))]
public class Hands : Interaction, Tool
{
    public delegate void OnHandChange(int selectedHand);

    // A container which manages the hands. Should be sibling component.
    [SerializeField]
    private Container handContainer;

    [System.NonSerialized]
    public int selectedHand = 0;
    public event OnHandChange onHandChange;

    /**
     * The default hand interaction when no object is present.
     * Note: This could be moved into a different class if this one gets too cluttered
     */
    public void Interact(RaycastHit hit, bool secondary)
    {
        // All actions just use primary mouse.
        if(secondary)
            return;

        // To pick up an item, we need to be clicking a free item object with an empty hand
        if(GetItemInHand() == null)
        {
            Item item = hit.collider.gameObject.GetComponent<Item>();
            if(item && item.transform.parent == null)
                inventory.CmdAddItem(hit.collider.gameObject, handContainer.gameObject, handSlots[selectedHand]);
        }
        else
        {
            // Drop the item currently being held.
            inventory.CmdPlaceItem(handContainer.gameObject, handSlots[selectedHand], hit.point + new Vector3(0f, 0.2f), new Quaternion());
        }
        // TODO: Default hand interactions with non-items
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

        handContainer.OnChange += c => UpdateTool();
        if (handContainer.GetItems().Count > 0)
        {
            inventory.selectedSlot = new Inventory.SlotReference(handContainer, handSlots[selectedHand]);
            UpdateTool();
        }
    }

    protected override void Update()
    {
        base.Update();

        if (!isLocalPlayer)
            return;

        // Hand-related buttons
        if (Input.GetButtonDown("Swap Active"))
        {
            selectedHand = 1 - selectedHand;
            inventory.selectedSlot = new Inventory.SlotReference(handContainer, handSlots[selectedHand]);
            onHandChange?.Invoke(selectedHand);
            
            UpdateTool();
        }
        if (Input.GetButtonDown("Drop Item") && GetItemInHand())
        {
            var transform = GetItemInHand().transform;
            inventory.CmdPlaceItem(handContainer.gameObject, handSlots[selectedHand], transform.position, transform.rotation);
        }
    }

    // Update the current tool being used, should occur whenever hand or item changes
    private void UpdateTool()
    {
        selectedTool = GetItemInHand() is Tool ? GetItemInHand() as Tool : this;
    }

    private Item GetItemInHand()
    {
        return handContainer.GetItem(handSlots[selectedHand]);
    }

    // The indices in the container that contains the hands
    private int[] handSlots;
    private Inventory inventory;
}
