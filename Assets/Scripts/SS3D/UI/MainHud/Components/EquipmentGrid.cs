using System;
using SS3D.UI.MachineInterface.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// Bottom-left worn-equipment doll, collapsed behind a small toggle by default (main-hud.md §4 keeps only the
    /// hands and gear strip permanent). Laid out like the character-examine paperdoll (<c>CharacterPaperdollGrid</c>)
    /// so your own gear and another player's read the same: head on top, eyes/face/ears, suit/shirt/gloves, then
    /// feet with suit storage beside them while a suit is worn. Gloves and feet are one slot each, covering both the
    /// left and right containers.
    /// Click transfers with the active hand; drag participates in storage-panel cross-surface DnD.
    /// </summary>
    public class EquipmentGrid : VisualElement
    {
        public enum Slot
        {
            Head,
            Eyes,
            Face,
            Ears,
            Gloves,
            Shirt,
            Feet,
            Suit,
            SuitStorage,
        }

        /// <summary>Same well size as the examine window's paperdoll.</summary>
        public const float SlotSize = 64f;

        public event Action<Slot> SlotClicked;
        public event Action<Slot, Vector2> SlotDragStarted;
        public event Action<Slot, Vector2> SlotDragMoved;
        public event Action<Slot, Vector2> SlotDragEnded;

        /// <summary>The doll was shown or hidden from its toggle.</summary>
        public event Action<bool> ExpandedChanged;

        private readonly VisualElement _panel;
        private readonly VisualElement _toggle;
        private readonly Label _toggleLabel;

        private readonly InventorySlot _head;
        private readonly InventorySlot _eyes;
        private readonly InventorySlot _face;
        private readonly InventorySlot _ears;
        private readonly InventorySlot _gloves;
        private readonly InventorySlot _shirt;
        private readonly InventorySlot _feet;
        private readonly InventorySlot _suit;
        private readonly InventorySlot _suitStorage;

        private Slot? _pressSlot;
        private bool _dragMoved;
        private Vector2 _dragOrigin;
        private bool _expanded = true;

        public EquipmentGrid(MainHudIconSet icons)
        {
            AddToClassList("equipment-grid");
            pickingMode = PickingMode.Ignore;

            Sprite suitIcon = icons.Suit != null ? icons.Suit : icons.Shirt;
            _head = CreateSlot(Slot.Head, icons.Head);
            _eyes = CreateSlot(Slot.Eyes, icons.Eyes);
            _face = CreateSlot(Slot.Face, icons.Face);
            _ears = CreateSlot(Slot.Ears, icons.Ears);
            _suit = CreateSlot(Slot.Suit, suitIcon);
            _shirt = CreateSlot(Slot.Shirt, icons.Shirt);
            _gloves = CreateSlot(Slot.Gloves, icons.Gloves);
            _feet = CreateSlot(Slot.Feet, icons.Feet);
            _suitStorage = CreateSlot(Slot.SuitStorage, icons.SuitStorage != null ? icons.SuitStorage : suitIcon);
            SetSuitStorageVisible(false);

            _panel = new VisualElement();
            _panel.AddToClassList("equipment-grid__panel");
            _panel.Add(BuildRow(BuildSpacer(), _head, BuildSpacer()));
            _panel.Add(BuildRow(_eyes, _face, _ears));
            _panel.Add(BuildRow(_suit, _shirt, _gloves));
            _panel.Add(BuildRow(BuildSpacer(), _feet, _suitStorage));

            _toggle = new VisualElement();
            _toggle.AddToClassList("equipment-grid__toggle");
            VisualElement toggleGlyph = new();
            toggleGlyph.AddToClassList("equipment-grid__toggle-glyph");
            toggleGlyph.pickingMode = PickingMode.Ignore;
            if (icons.EquipmentToggle != null)
            {
                toggleGlyph.style.backgroundImage = new StyleBackground(icons.EquipmentToggle);
            }

            _toggleLabel = new Label();
            _toggleLabel.AddToClassList("equipment-grid__toggle-label");
            _toggleLabel.AddToClassList("font-body");
            _toggleLabel.pickingMode = PickingMode.Ignore;
            // Host wider than the 64 px well, centred, so the chip fits its text (see EquipmentGrid.uss).
            VisualElement toggleLabelHost = new();
            toggleLabelHost.AddToClassList("equipment-grid__toggle-label-host");
            toggleLabelHost.pickingMode = PickingMode.Ignore;
            toggleLabelHost.Add(_toggleLabel);
            _toggle.Add(toggleGlyph);
            _toggle.Add(toggleLabelHost);
            _toggle.RegisterCallback<ClickEvent>(_ =>
            {
                SetExpanded(!_expanded);
                ExpandedChanged?.Invoke(_expanded);
            });

            Add(_panel);
            Add(_toggle);
            SetExpanded(false);
        }

        public bool Expanded => _expanded;

        /// <summary>Shows or hides the doll; only the toggle stays on screen while collapsed.</summary>
        public void SetExpanded(bool expanded)
        {
            _expanded = expanded;
            _panel.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            _toggle.EnableInClassList("equipment-grid__toggle--open", expanded);
            _toggleLabel.text = expanded ? "Hide equipment" : "Show equipment";
        }

        /// <summary>Suit storage exists only while a suit is worn; the slot keeps its place in the grid.</summary>
        public void SetSuitStorageVisible(bool visible)
        {
            _suitStorage.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }

        public void SetIcon(Slot slot, Sprite itemIcon)
        {
            SetContents(slot, itemIcon, itemName: null);
        }

        /// <summary>
        /// Updates the well icon and label. When <paramref name="itemName"/> is set, the label shows
        /// the item name; when empty/null, it restores the slot's default name (Head, Eyes, …).
        /// </summary>
        public void SetContents(Slot slot, Sprite itemIcon, string itemName)
        {
            InventorySlot inventorySlot = GetSlot(slot);
            if (inventorySlot == null)
            {
                return;
            }

            inventorySlot.ItemIcon = itemIcon;
            inventorySlot.SlotLabel = string.IsNullOrEmpty(itemName) ? DefaultLabel(slot) : itemName;
        }

        public InventorySlot GetInventorySlot(Slot slot) => GetSlot(slot);

        public Rect GetSlotWorldBound(Slot slot) => GetSlot(slot).worldBound;

        private InventorySlot GetSlot(Slot slot) => slot switch
        {
            Slot.Head => _head,
            Slot.Eyes => _eyes,
            Slot.Face => _face,
            Slot.Ears => _ears,
            Slot.Gloves => _gloves,
            Slot.Shirt => _shirt,
            Slot.Feet => _feet,
            Slot.Suit => _suit,
            Slot.SuitStorage => _suitStorage,
            _ => null,
        };

        /// <summary>Same names as the examine paperdoll.</summary>
        private static string DefaultLabel(Slot slot) => slot switch
        {
            Slot.Head => "Head",
            Slot.Eyes => "Eyes",
            Slot.Face => "Face Cover",
            Slot.Ears => "Ears",
            Slot.Gloves => "Gloves",
            Slot.Shirt => "Shirt",
            Slot.Feet => "Feet",
            Slot.Suit => "Suit",
            Slot.SuitStorage => "Suit Storage",
            _ => string.Empty,
        };

        private InventorySlot CreateSlot(Slot slot, Sprite emptyIcon)
        {
            InventorySlot inventorySlot = new()
            {
                Unknown = false,
                EmptyIcon = emptyIcon,
                Size = SlotSize,
                SlotLabel = DefaultLabel(slot),
            };

            inventorySlot.RegisterCallback<ClickEvent>(_ =>
            {
                if (_dragMoved)
                {
                    return;
                }

                SlotClicked?.Invoke(slot);
            });
            inventorySlot.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(slot, inventorySlot, evt));
            inventorySlot.RegisterCallback<PointerMoveEvent>(evt => OnPointerMove(slot, inventorySlot, evt));
            inventorySlot.RegisterCallback<PointerUpEvent>(evt => OnPointerUp(slot, inventorySlot, evt));
            inventorySlot.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                if (_pressSlot == slot)
                {
                    _pressSlot = null;
                }
            });
            return inventorySlot;
        }

        private void OnPointerDown(Slot slot, InventorySlot inventorySlot, PointerDownEvent evt)
        {
            // Left-button only, and another button pressed mid-drag must not restart it (see HandsGearStrip.WireDrag).
            if (evt.button != (int)MouseButton.LeftMouse)
            {
                if (inventorySlot.HasPointerCapture(evt.pointerId))
                {
                    evt.StopPropagation();
                }

                return;
            }

            // Clear before the empty-slot early-out — same sticky-_dragMoved bug as HandsGearStrip.
            _dragMoved = false;
            _dragOrigin = (Vector2)evt.position;

            if (inventorySlot.ItemIcon == null)
            {
                return;
            }

            // The drag itself starts once the pointer moves (DragGesture); a release before that is a click.
            _pressSlot = slot;
            inventorySlot.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(Slot slot, InventorySlot inventorySlot, PointerMoveEvent evt)
        {
            if (_pressSlot != slot || !inventorySlot.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            if (!_dragMoved)
            {
                if (!DragGesture.PastThreshold(_dragOrigin, (Vector2)evt.position))
                {
                    return;
                }

                _dragMoved = true;
                SlotDragStarted?.Invoke(slot, _dragOrigin);
            }

            SlotDragMoved?.Invoke(slot, (Vector2)evt.position);
        }

        private void OnPointerUp(Slot slot, InventorySlot inventorySlot, PointerUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse || _pressSlot != slot || !inventorySlot.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            inventorySlot.ReleasePointer(evt.pointerId);
            if (_dragMoved)
            {
                SlotDragEnded?.Invoke(slot, (Vector2)evt.position);
            }

            _pressSlot = null;
        }

        private static VisualElement BuildRow(params VisualElement[] children)
        {
            VisualElement row = new();
            row.AddToClassList("equipment-grid__row");
            foreach (VisualElement child in children)
            {
                row.Add(child);
            }

            return row;
        }

        private static VisualElement BuildSpacer()
        {
            VisualElement spacer = new();
            spacer.AddToClassList("equipment-grid__spacer");
            spacer.style.width = SlotSize;
            return spacer;
        }
    }
}
