using UnityEngine;

/**
 * An item describes what is held in a container.
 */
public class Item
{
    // The slots in which the item can go.
    public string[] compatibleSlots;

    // Attributes for the item.
    public Sprite sprite;
    public GameObject prefab;
}
