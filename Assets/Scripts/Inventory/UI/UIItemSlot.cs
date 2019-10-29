using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Controls a single slot that displays a single item
 */
[ExecuteInEditMode]
public class UIItemSlot : MonoBehaviour
{
    [SerializeField]
    private UnityEngine.UI.Image itemContainer;
    [SerializeField]
    private UnityEngine.UI.Image border;
    [SerializeField]
    private Color activeColor = Color.white;
    [SerializeField]
    private Color inactiveColor = Color.grey;
    [SerializeField]
    private Sprite emptySprite;
    // TODO: On press

    public void SetSprite(Sprite sprite)
    {
        if(!active && sprite != null)
            SetActive(true);

        if(sprite != null)
            itemContainer.sprite = sprite;
        else
            itemContainer.sprite = emptySprite;
    }
    public void SetActive(bool active)
    {
        this.active = active;

        // TODO: Correct logic?
        if(!active)
            SetSprite(null);
    }
    public void SetHighlight(bool value)
    {
        border.color = value ? activeColor : inactiveColor;
        // itemContainer.color = value ? activeColor : inactiveColor;
    }

    private void Awake()
    {
        itemContainer.sprite = emptySprite;
        itemContainer.color = active ? activeColor : inactiveColor;
        border.color = active ? activeColor : inactiveColor;
    }
    private void OnValidate()
    {
        if (itemContainer && emptySprite)
            itemContainer.sprite = emptySprite;
    }

    private bool active = false;
}
