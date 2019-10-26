using UnityEngine;

/**
 * An item describes what is held in a container.
 */
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

    public ItemType     itemType;
    public Sprite       sprite;
    public GameObject   prefab;

    public void Despawn()
    {
        gameObject.SetActive(false);
    }
    public void Spawn(Vector3 location)
    {
        transform.position = location;
        gameObject.SetActive(true);
    }
}
