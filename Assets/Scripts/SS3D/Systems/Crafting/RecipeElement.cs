using SS3D.Data.Enums;
using UnityEngine;

/// <summary>
/// A recipe element is simply describing an item and a number of it.
/// </summary>
[System.Serializable]
public class RecipeElement
{
    /// <summary>
    /// Number of items.
    /// </summary>
    [SerializeField]
    private int _count;

    /// <summary>
    /// Id of the item.
    /// </summary>
    [SerializeField]
    private ItemId _itemId;

    public RecipeElement()
    {

    }

    public RecipeElement(int count, ItemId id)
    {
        _count = count;
        _itemId= id;
    }
}
