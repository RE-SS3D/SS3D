using UnityEngine;
using Mirror;

/**
 * An item describes what is held in a container.
 */
[DisallowMultipleComponent]
public class Item : MonoBehaviour
{
    // Distinguishes what can go in what slot
    public enum ItemType
    {
        Other,
        Hat,
        Glasses,
        Mask,
        Earpiece,
        Shirt,
        OverShirt,
        Gloves,
        Shoes
    }
    public enum ItemPosition
    {
        InInventory,
        InWorld,
        OnObject
    }

    public ItemType     itemType;
    public Sprite       sprite;
    public GameObject   prefab;
}
