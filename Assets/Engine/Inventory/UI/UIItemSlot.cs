using SS3D.Engine.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Engine.Interactions.UI
{

    /**
     * Controls a single slot that displays a single item
     */
    [ExecuteInEditMode]
    public class UIItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerClickHandler
    {
        public interface SlotInteractor
        {
            // When the given item slot is tapped
            void OnPress(UIItemSlot slot, PointerEventData.InputButton button);
            void OnDragStart(UIItemSlot from, PointerEventData eventData);

            // When the item is being dragged, and it hovers over the given slot
            void StartHover(UIItemSlot hovering, UIAbstractContainer overContainer, UIItemSlot over = null);
            // When the item is being dragged, and it stops hovering over the given slot
            void StopHover(UIItemSlot hovering, UIAbstractContainer overContainer, UIItemSlot over = null);
        }

        // Unity Inspector
        // The place in which the item sprite goes
        [SerializeField] private Image itemContainer = null;
        [SerializeField] private Button button = null;
        // The default image for when there is no sprite
        [SerializeField] private Sprite emptySprite = null;
        [SerializeField] private Slider itemSupplySlider = null;

        [SerializeField] private Color highlightedColor = Color.red;
        [SerializeField] private Color selectedColor = Color.green;
        [SerializeField] private Color defaultColor = Color.blue;
        [SerializeField] private Color disabledColor = Color.black;
        [SerializeField] private Color itemSupplyFullColor = Color.green;
        [SerializeField] private Color itemSupplyHalfColor = Color.yellow;
        [SerializeField] private Color itemSupplyEmptyColor = Color.red;

        // Runtime Variables
        [System.NonSerialized]
        public SlotInteractor slotInteractor;

        public Item Item
        {
            get => item;
            set
            {
                item = value;
                itemContainer.sprite = item == null ? emptySprite : item.sprite;
                CalculateColors();
                if (item != null) {
                    //Activate item supply bar display, if appropriate.
                    IItemWithSupply itemWithSupply = item.GetComponent<IItemWithSupply>();
                    if (itemWithSupply != null)
                    {
                        itemSupplySlider.gameObject.SetActive(true);
                        SetItemSupplyDisplay(itemWithSupply.GetRemainingSupplyPercentage());
                        return;
                    }
                    
                    // we need to remove this from here like wtf this is a base code not a specific part
                    IChargeable powerCell = item.GetComponent<IChargeable>();
                    if (powerCell == null && itemWithSupply == null) return;

                    itemSupplySlider.gameObject.SetActive(true);
                    SetItemSupplyDisplay(powerCell.GetPowerPercentage());
                }
                else {
                    itemSupplySlider.gameObject.SetActive(false);
                }
            }
        }
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;
                CalculateColors();
            }
        }
        public bool Highlighted
        {
            get => highlighted;
            set
            {
                highlighted = value;
                CalculateColors();
            }
        }
        public bool Transparent
        {
            get => transparent;
            set
            {
                transparent = value;
                CalculateColors();
            }
        }
        
        public GameObject CreateDraggableSprite(Vector2 position, Quaternion quaternion, Transform parent)
        {
            var itemObject = Instantiate(itemContainer.gameObject, position, quaternion, transform);
            var image = itemObject.GetComponent<Image>();
            var transparentColor = image.color;
            transparentColor.a = 0.75f;
            image.color = transparentColor;
            return itemObject;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            slotInteractor.OnDragStart(this, eventData);
        }
        // Required to do OnBeginDrag :/
        public void OnDrag(PointerEventData eventData) { }

        public void SetItemSupplyDisplay(float value)
        {
            itemSupplySlider.value = value;
            ColorBlock colors = itemSupplySlider.colors;
            if (value > 0.66f) colors.disabledColor = itemSupplyFullColor;
            else if (value > 0.33f) {
                colors.disabledColor = itemSupplyHalfColor;
            }
            else {
                colors.disabledColor = itemSupplyEmptyColor;
            }

            itemSupplySlider.colors = colors;
        }

        private void Awake()
        {
            if (emptySprite)
                itemContainer.sprite = emptySprite;
            buttonColors = button.colors;
            CalculateColors();
        }
        private void Start() => CalculateColors();
        private void OnValidate()
        {
            if (itemContainer && itemContainer.sprite == null && emptySprite)
                itemContainer.sprite = emptySprite;

            if (!button)
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
            if (itemContainer.sprite == null)
                itemColor.a = 0.0f;
            else if (transparent)
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
        public void OnPointerClick(PointerEventData eventData)
        {
            slotInteractor.OnPress(this, eventData.button);
        }
    }
}