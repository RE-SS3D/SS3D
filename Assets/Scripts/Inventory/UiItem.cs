using UnityEngine;
using UnityEngine.UI;

public class UiItem : MonoBehaviour
{
    public Item Item;
    public Image Image;

    public void Initialize(Item item)
    {
        name = $"UI Item - {item.name}";
        Item = item;
        Image.sprite = item.Sprite;
    }
}