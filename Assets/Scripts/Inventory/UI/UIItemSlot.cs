using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/**
 * Controls a single slot that displays a single item
 */
[ExecuteInEditMode]
public class UIItemSlot : MonoBehaviour
{
    public interface SlotInteractor
    {
        // When the given item slot is tapped
        void OnPress(UIItemSlot slot);

        /// <summary>
        /// When the item is dragged to a container. Can either be dragged just to that container, or to a slot in that container.
        /// 
        /// Note: The UIItemSlot does not assume a drag results in the item moving, and will function correctly whether or not
        /// the item moves
        /// </summary>
        /// <param name="from">The UISlot being dragged</param>
        /// <param name="toContainer">The container being dragged to</param>
        /// <param name="toSlot">The slot being dragged to within the container. May be null.</param>
        void DragTo(UIItemSlot from, UIAbstractContainer toContainer, UIItemSlot toSlot = null);
        /// <summary>
        /// When the item is dragged to the world.
        /// 
        /// Note: The UIItemSlot does not assume a drag results in the item moving, and will function correctly whether or not
        /// the item moves
        /// </summary>
        /// <param name="slot">The UISlot being dragged</param>
        /// <param name="position">The raycast indicating where the drag ended</param>
        void DragTo(UIItemSlot slot, Vector2 screenPosition);

        // When the item is being dragged, and it hovers over the given slot
        void StartHover(UIItemSlot hovering, UIAbstractContainer overContainer, UIItemSlot over = null);
        // When the item is being dragged, and it stops hovering over the given slot
        void StopHover(UIItemSlot hovering, UIAbstractContainer overContainer, UIItemSlot over = null);
    }

    // Unity Inspector
    // The place in which the item sprite goes
    [SerializeField]
    private Image itemContainer;
    [SerializeField]
    private Button button;
    // The default image for when there is no sprite
    [SerializeField]
    private Sprite emptySprite;

    [SerializeField]
    private Color highlightedColor;
    [SerializeField]
    private Color selectedColor;
    [SerializeField]
    private Color defaultColor;
    [SerializeField]
    private Color disabledColor;

    // Runtime Variables
    [System.NonSerialized]
    public SlotInteractor slotInteractor;

    public Item Item {
        get => item;
        set {
            item = value;
            itemContainer.sprite = item == null ? emptySprite : item.sprite;
        }
    }
    public bool Selected {
        get => selected;
        set {
            selected = value;
            CalculateColors();
        }
    }
    public bool Highlighted {
        get => highlighted;
        set {
            highlighted = value;
            CalculateColors();
        }
    }
    public bool Transparent {
        get => transparent;
        set {
            transparent = value;
            CalculateColors();
        }
    }

    public void Press()
    {
        slotInteractor.OnPress(this);
    }
    public GameObject CreateDraggableSprite(Vector2 position, Quaternion quaternion, Transform parent)
    {
        return Instantiate(itemContainer.gameObject, position, quaternion, transform);
    }


    private void Awake()
    {
        itemContainer.sprite = emptySprite;
        buttonColors = button.colors;
        CalculateColors();
    }
    private void Start() => CalculateColors();
    private void OnValidate()
    {
        if (itemContainer && emptySprite)
            itemContainer.sprite = emptySprite;

        if(!button)
            button = GetComponent<Button>();
        buttonColors = button.colors;
    }
    private void OnDisable() => CalculateColors();
    private void OnEnable() => CalculateColors();

    private void CalculateColors()
    {
        if (!enabled)
            buttonColors.normalColor = disabledColor;
        else if (highlighted)
            buttonColors.normalColor = highlightedColor;
        else if (selected)
            buttonColors.normalColor = selectedColor;
        else
            buttonColors.normalColor = defaultColor;

        buttonColors.highlightedColor = buttonColors.normalColor;
        buttonColors.pressedColor = buttonColors.normalColor;

        button.colors = buttonColors;

        var itemColor = itemContainer.color;
        if (transparent)
            itemColor.a = 0.75f;
        else
            itemColor.a = 1.0f;
        itemContainer.color = itemColor;
    }

    private Item item = null;
    private bool selected = false;
    private bool highlighted = false;
    private bool transparent = false;

    private ColorBlock buttonColors;
}
