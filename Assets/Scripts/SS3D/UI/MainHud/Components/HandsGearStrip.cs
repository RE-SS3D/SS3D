using System;
using SS3D.UI.MachineInterface.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// Bottom-center gear strip (belt/ID/pocket/back) + the two hand slots, active hand marked with the
    /// accent-rust border wrapper from the mockup.
    /// </summary>
    public class HandsGearStrip : VisualElement
    {
        public enum GearSlot
        {
            Belt,
            Id,
            Pocket,
            Back,
        }

        public enum HandSlot
        {
            Left,
            Right,
        }

        /// <summary>
        /// Fired when a hand well is clicked. Argument is true for the left (first) hand slot.
        /// </summary>
        public event Action<bool> HandClickRequested;

        /// <summary>
        /// Fired when a gear-strip slot (belt/ID/pocket/back) is clicked — opens that slot's storage
        /// panel (Documents/design/inventory-storage.md §6/§11).
        /// </summary>
        public event Action<GearSlot> GearSlotClicked;

        public event Action<GearSlot, Vector2> GearDragStarted;
        public event Action<GearSlot, Vector2> GearDragMoved;
        public event Action<GearSlot, Vector2> GearDragEnded;

        public event Action<HandSlot, Vector2> HandDragStarted;
        public event Action<HandSlot, Vector2> HandDragMoved;
        public event Action<HandSlot, Vector2> HandDragEnded;

        private readonly InventorySlot _belt;
        private readonly InventorySlot _id;
        private readonly InventorySlot _pocket;
        private readonly InventorySlot _back;
        private readonly InventorySlot _handLeft;
        private readonly InventorySlot _handRight;

        private GearSlot? _dragGear;
        private HandSlot? _dragHand;
        private bool _dragMoved;

        public HandsGearStrip(MainHudIconSet icons)
        {
            AddToClassList("hands-gear-strip");

            _belt = CreateGearSlot(GearSlot.Belt, "Belt", icons.Belt, 64);
            _id = CreateGearSlot(GearSlot.Id, "ID", icons.Id, 64);
            _pocket = CreateGearSlot(GearSlot.Pocket, "Pocket", icons.Pocket, 64);
            _back = CreateGearSlot(GearSlot.Back, "Back", icons.Back, 64);

            VisualElement gear = new();
            gear.AddToClassList("hands-gear-strip__gear");
            gear.Add(_belt);
            gear.Add(_id);
            gear.Add(_pocket);
            gear.Add(_back);

            VisualElement divider = new();
            divider.AddToClassList("hands-gear-strip__divider");

            _handLeft = CreateHandSlot(HandSlot.Left, "Left hand", icons.HandLeft, 96);
            _handRight = CreateHandSlot(HandSlot.Right, "Right hand", icons.HandRight, 96);

            VisualElement hands = new();
            hands.AddToClassList("hands-gear-strip__hands");
            hands.Add(_handLeft);
            hands.Add(_handRight);

            Add(gear);
            Add(divider);
            Add(hands);

            SetActiveHand(leftIsActive: true);
        }

        public void SetGearIcon(GearSlot slot, Sprite itemIcon)
        {
            SetGearContents(slot, itemIcon, itemName: null);
        }

        /// <summary>
        /// Updates a gear well's icon and label. Empty <paramref name="itemName"/> restores Belt/ID/Pocket/Back.
        /// </summary>
        public void SetGearContents(GearSlot slot, Sprite itemIcon, string itemName)
        {
            InventorySlot inventorySlot = GetGearSlot(slot);
            if (inventorySlot == null)
            {
                return;
            }

            inventorySlot.ItemIcon = itemIcon;
            inventorySlot.SlotLabel = string.IsNullOrEmpty(itemName) ? DefaultGearLabel(slot) : itemName;
        }

        /// <summary>Panel-space bounds of a gear slot, used to anchor its storage panel near the click.</summary>
        public Rect GetGearSlotWorldBound(GearSlot slot) => GetGearSlot(slot).worldBound;

        public InventorySlot GetGearInventorySlot(GearSlot slot) => GetGearSlot(slot);

        public InventorySlot GetHandInventorySlot(HandSlot slot) => slot == HandSlot.Left ? _handLeft : _handRight;

        public void SetHandIcons(Sprite leftItemIcon, Sprite rightItemIcon)
        {
            SetHandContents(HandSlot.Left, leftItemIcon, itemName: null);
            SetHandContents(HandSlot.Right, rightItemIcon, itemName: null);
        }

        public void SetHandContents(HandSlot slot, Sprite itemIcon, string itemName)
        {
            InventorySlot inventorySlot = GetHandInventorySlot(slot);
            inventorySlot.ItemIcon = itemIcon;
            inventorySlot.SlotLabel = string.IsNullOrEmpty(itemName)
                ? (slot == HandSlot.Left ? "Left hand" : "Right hand")
                : itemName;
        }

        public void SetActiveHand(bool leftIsActive)
        {
            _handLeft.EnableInClassList("inventory-slot--active-hand", leftIsActive);
            _handRight.EnableInClassList("inventory-slot--active-hand", !leftIsActive);
        }

        private InventorySlot GetGearSlot(GearSlot slot) => slot switch
        {
            GearSlot.Belt => _belt,
            GearSlot.Id => _id,
            GearSlot.Pocket => _pocket,
            GearSlot.Back => _back,
            _ => null,
        };

        private static string DefaultGearLabel(GearSlot slot) => slot switch
        {
            GearSlot.Belt => "Belt",
            GearSlot.Id => "ID",
            GearSlot.Pocket => "Pocket",
            GearSlot.Back => "Back",
            _ => string.Empty,
        };

        private InventorySlot CreateGearSlot(GearSlot slot, string label, Sprite emptyIcon, float size)
        {
            InventorySlot inventorySlot = CreateSlot(label, emptyIcon, size);
            inventorySlot.RegisterCallback<ClickEvent>(_ =>
            {
                if (_dragMoved)
                {
                    return;
                }

                GearSlotClicked?.Invoke(slot);
            });
            WireDrag(
                inventorySlot,
                () => inventorySlot.ItemIcon != null,
                () =>
                {
                    _dragGear = slot;
                    _dragHand = null;
                },
                pos => GearDragStarted?.Invoke(slot, pos),
                pos => GearDragMoved?.Invoke(slot, pos),
                pos => GearDragEnded?.Invoke(slot, pos));
            return inventorySlot;
        }

        private InventorySlot CreateHandSlot(HandSlot slot, string label, Sprite emptyIcon, float size)
        {
            InventorySlot inventorySlot = CreateSlot(label, emptyIcon, size);
            bool left = slot == HandSlot.Left;
            inventorySlot.RegisterCallback<ClickEvent>(_ =>
            {
                if (_dragMoved)
                {
                    return;
                }

                HandClickRequested?.Invoke(left);
            });
            WireDrag(
                inventorySlot,
                () => inventorySlot.ItemIcon != null,
                () =>
                {
                    _dragHand = slot;
                    _dragGear = null;
                },
                pos => HandDragStarted?.Invoke(slot, pos),
                pos => HandDragMoved?.Invoke(slot, pos),
                pos => HandDragEnded?.Invoke(slot, pos));
            return inventorySlot;
        }

        private void WireDrag(
            InventorySlot inventorySlot,
            Func<bool> canDrag,
            Action onBegin,
            Action<Vector2> started,
            Action<Vector2> moved,
            Action<Vector2> ended)
        {
            Vector2 dragOrigin = default;

            // Always clear the click-suppression flag on press — including empty wells. Otherwise a
            // prior drag leaves _dragMoved true and empty-hand clicks (which never re-enter the
            // canDrag path) permanently fail to select a hand.
            inventorySlot.RegisterCallback<PointerDownEvent>(evt =>
            {
                _dragMoved = false;
                dragOrigin = (Vector2)evt.position;

                if (!canDrag())
                {
                    return;
                }

                onBegin();
                inventorySlot.CapturePointer(evt.pointerId);
                started((Vector2)evt.position);
                evt.StopPropagation();
            });
            inventorySlot.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!inventorySlot.HasPointerCapture(evt.pointerId))
                {
                    return;
                }

                Vector2 delta = (Vector2)evt.position - dragOrigin;
                if (!_dragMoved && delta.sqrMagnitude >= DragThresholdSq)
                {
                    _dragMoved = true;
                }

                if (_dragMoved)
                {
                    moved((Vector2)evt.position);
                }
            });
            inventorySlot.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!inventorySlot.HasPointerCapture(evt.pointerId))
                {
                    return;
                }

                inventorySlot.ReleasePointer(evt.pointerId);
                ended((Vector2)evt.position);
                _dragGear = null;
                _dragHand = null;
            });
            inventorySlot.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                if (_dragGear == null && _dragHand == null)
                {
                    return;
                }

                _dragGear = null;
                _dragHand = null;
            });
        }

        private const float DragThresholdSq = 25f;

        private static InventorySlot CreateSlot(string label, Sprite emptyIcon, float size)
        {
            return new InventorySlot { Unknown = emptyIcon == null, EmptyIcon = emptyIcon, Size = size, SlotLabel = label };
        }
    }
}
