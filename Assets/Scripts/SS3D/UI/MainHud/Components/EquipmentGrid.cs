using System;
using SS3D.UI.MachineInterface.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// Bottom-left worn-equipment doll: head/eyes/face-cover/ears on top, gloves/shirt/feet below.
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
            GloveLeft,
            Shirt,
            GloveRight,
            Feet,
        }

        public event Action<Slot> SlotClicked;
        public event Action<Slot, Vector2> SlotDragStarted;
        public event Action<Slot, Vector2> SlotDragMoved;
        public event Action<Slot, Vector2> SlotDragEnded;

        private readonly InventorySlot _head;
        private readonly InventorySlot _eyes;
        private readonly InventorySlot _face;
        private readonly InventorySlot _ears;
        private readonly InventorySlot _gloveLeft;
        private readonly InventorySlot _shirt;
        private readonly InventorySlot _gloveRight;
        private readonly InventorySlot _feet;

        private Slot? _dragSlot;
        private bool _dragMoved;

        public EquipmentGrid(MainHudIconSet icons)
        {
            AddToClassList("equipment-grid");

            _head = CreateSlot(Slot.Head, "Head", icons.Head);
            _eyes = CreateSlot(Slot.Eyes, "Eyes", icons.Eyes);
            _face = CreateSlot(Slot.Face, "Face Cover", icons.Face);
            _ears = CreateSlot(Slot.Ears, "Ears", icons.Ears);
            _gloveLeft = CreateSlot(Slot.GloveLeft, "Left Glove", icons.HandLeft);
            _shirt = CreateSlot(Slot.Shirt, "Shirt", icons.Shirt);
            _gloveRight = CreateSlot(Slot.GloveRight, "Right Glove", icons.HandRight);
            _feet = CreateSlot(Slot.Feet, "Feet", icons.Feet);

            Add(BuildRow(BuildSpacer(), _head, BuildSpacer()));
            Add(BuildRow(_eyes, _face, _ears));
            Add(BuildRow(_gloveLeft, _shirt, _gloveRight));
            Add(BuildRow(BuildSpacer(), _feet, BuildSpacer()));
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
            Slot.GloveLeft => _gloveLeft,
            Slot.Shirt => _shirt,
            Slot.GloveRight => _gloveRight,
            Slot.Feet => _feet,
            _ => null,
        };

        private static string DefaultLabel(Slot slot) => slot switch
        {
            Slot.Head => "Head",
            Slot.Eyes => "Eyes",
            Slot.Face => "Face Cover",
            Slot.Ears => "Ears",
            Slot.GloveLeft => "Left Glove",
            Slot.Shirt => "Shirt",
            Slot.GloveRight => "Right Glove",
            Slot.Feet => "Feet",
            _ => string.Empty,
        };

        private InventorySlot CreateSlot(Slot slot, string label, Sprite emptyIcon)
        {
            InventorySlot inventorySlot = new()
            {
                Unknown = emptyIcon == null,
                EmptyIcon = emptyIcon,
                Size = 72,
                SlotLabel = label,
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
                if (_dragSlot == slot)
                {
                    _dragSlot = null;
                }
            });
            return inventorySlot;
        }

        private Vector2 _dragOrigin;
        private const float DragThresholdSq = 25f;

        private void OnPointerDown(Slot slot, InventorySlot inventorySlot, PointerDownEvent evt)
        {
            // Clear before the empty-slot early-out — same sticky-_dragMoved bug as HandsGearStrip.
            _dragMoved = false;
            _dragOrigin = (Vector2)evt.position;

            if (inventorySlot.ItemIcon == null)
            {
                return;
            }

            _dragSlot = slot;
            inventorySlot.CapturePointer(evt.pointerId);
            SlotDragStarted?.Invoke(slot, (Vector2)evt.position);
            evt.StopPropagation();
        }

        private void OnPointerMove(Slot slot, InventorySlot inventorySlot, PointerMoveEvent evt)
        {
            if (_dragSlot != slot || !inventorySlot.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            Vector2 delta = (Vector2)evt.position - _dragOrigin;
            if (!_dragMoved && delta.sqrMagnitude >= DragThresholdSq)
            {
                _dragMoved = true;
            }

            if (_dragMoved)
            {
                SlotDragMoved?.Invoke(slot, (Vector2)evt.position);
            }
        }

        private void OnPointerUp(Slot slot, InventorySlot inventorySlot, PointerUpEvent evt)
        {
            if (_dragSlot != slot || !inventorySlot.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            inventorySlot.ReleasePointer(evt.pointerId);
            SlotDragEnded?.Invoke(slot, (Vector2)evt.position);
            _dragSlot = null;
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
            return spacer;
        }
    }
}
