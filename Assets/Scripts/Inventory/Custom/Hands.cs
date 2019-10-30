using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * The hand system is an interaction system which connects to the inventory.
 * There is a hands container, that the interaction system connects to, that defines what items the player is currently holding.
 */
[RequireComponent(typeof(Inventory))]
[RequireComponent(typeof(HandContainer))]
public class Hands : Interaction, Tool
{
    public delegate void OnHandChange(int selectedHand);

    [System.NonSerialized]
    public int selectedHand = 0;
    public event OnHandChange onHandChange;

    /**
     * The default hand interaction when no object is present.
     * Note: This could be moved into a different class if this one gets too cluttered
     */
    public void Interact(GameObject interactedObject, bool secondary = false)
    {
        Item item = interactedObject.GetComponent<Item>();

        if(item && item.transform.parent == null)
            inventory.CmdAddItem(interactedObject, holding.gameObject, selectedHand);

        // TODO: Default hand interactions with non-items
    }

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
        holding = GetComponent<HandContainer>();
    }
    public override void OnStartClient()
    {
        holding.onChange += (a, b, c) => UpdateTool();

        if (holding.GetItems().Count > 0)
            UpdateTool();
    }

    protected override void Update()
    {
        base.Update();

        if (!isLocalPlayer)
            return;

        // TODO: Ensure both hands are usable
        if (Input.GetButtonDown("Swap Active"))
        {
            selectedHand = 1 - selectedHand;
            onHandChange?.Invoke(selectedHand);
            UpdateTool();
        }
        if (Input.GetButtonDown("Drop Item") && holding.GetItem(selectedHand))
        {
            inventory.CmdPlaceItem(transform.position, holding.gameObject, selectedHand);
        }
    }

    private void UpdateTool()
    {
        selectedTool = holding.GetItem(selectedHand) is Tool ? holding.GetItem(selectedHand) as Tool : this;
    }

    private HandContainer holding;
    private Inventory inventory;
    private int _selectedHand;
}
