using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A component that can be attached to a prefab to reference an Item.
 * This is not part of Item so that Item may be completely detached from any Game Object.
 */
public class ItemReference : MonoBehaviour
{
    public Item item;
}
