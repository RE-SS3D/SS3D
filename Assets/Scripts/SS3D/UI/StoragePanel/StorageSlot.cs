using System;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.UI.MachineInterface.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.StoragePanel
{
    /// <summary>
    /// Drop-target/reject visual state for a slot mid-drag, per
    /// Documents/design/inventory-storage.md §4/§6 (size-class rejection, valid-drop highlight).
    /// </summary>
    public enum SlotDropState
    {
        None = 0,
        Valid = 1,
        Invalid = 2,
    }

    /// <summary>
    /// One grid cell in a <see cref="StoragePanelView"/>. Extends the shared <see cref="InventorySlot"/>
    /// well/icon/label chrome with a stack-count badge and the pointer-capture drag source/target events
    /// a storage panel needs (screen-space slot-to-slot dragging — see
    /// Documents/architecture/systems/inventory.md § Overview on why this is a
    /// new UITK implementation rather than the world-space InteractionTier.Combine grammar).
    /// A press becomes a drag once the pointer moves <see cref="DragThresholdPx"/>; a release before that is a
    /// click (Documents/design/inventory-storage.md §6).
    /// </summary>
    public class StorageSlot : InventorySlot
    {
        /// <summary>How far a press has to move before it becomes a drag rather than a click.</summary>
        public const float DragThresholdPx = 6f;

        /// <summary>Fired when a drag gesture starts on a bound (non-empty) slot. Position is panel-space.</summary>
        public event Action<StorageSlot, Vector2> DragStarted;

        /// <summary>Fired as the pointer moves while this slot holds the drag capture. Position is panel-space.</summary>
        public event Action<StorageSlot, Vector2> DragMoved;

        /// <summary>Fired when the drag gesture ends (pointer released). Position is panel-space (release point).</summary>
        public event Action<StorageSlot, Vector2> DragEnded;

        /// <summary>
        /// Fired on a left click (press and release without dragging), bound or empty. The int is the click count:
        /// 2 for the second click of a double-click.
        /// </summary>
        public event Action<StorageSlot, int> Clicked;

        private readonly Label _stackLabel;

        private int _pressPointerId = -1;
        private Vector2 _pressPosition;
        private int _pressClickCount;
        private bool _dragging;

        public Vector2Int Position { get; set; }

        public Item BoundItem { get; private set; }

        public StorageSlot()
        {
            AddToClassList("storage-slot");

            _stackLabel = new Label();
            _stackLabel.AddToClassList("storage-slot__stack-count");
            _stackLabel.pickingMode = PickingMode.Ignore;
            Add(_stackLabel);

            Size = 72f;
            Unknown = false;

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCaptureOutEvent>(_ => ResetPress());
        }

        /// <summary>Binds this slot to a stored item, or clears it to the empty placeholder state.</summary>
        public void Bind(Item item)
        {
            BoundItem = item;
            ItemIcon = item != null ? item.ItemSprite : null;

            bool showStack = item != null && item.IsStackable && item.StackCount > 1;
            _stackLabel.text = showStack ? $"×{item.StackCount}" : string.Empty;
            _stackLabel.style.display = showStack ? DisplayStyle.Flex : DisplayStyle.None;

            EnableInClassList("storage-slot--empty", item == null);
        }

        public void SetDropState(SlotDropState state)
        {
            EnableInClassList("inventory-slot--valid-drop", state == SlotDropState.Valid);
            EnableInClassList("inventory-slot--invalid-drop", state == SlotDropState.Invalid);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            _pressPointerId = evt.pointerId;
            _pressPosition = evt.position;
            _pressClickCount = evt.clickCount;
            _dragging = false;
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pressPointerId || !this.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            if (!_dragging)
            {
                if (BoundItem == null || ((Vector2)evt.position - _pressPosition).magnitude < DragThresholdPx)
                {
                    return;
                }

                _dragging = true;
                DragStarted?.Invoke(this, _pressPosition);
            }

            DragMoved?.Invoke(this, evt.position);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pressPointerId || !this.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            bool dragging = _dragging;
            int clickCount = _pressClickCount;
            ResetPress();
            this.ReleasePointer(evt.pointerId);

            if (dragging)
            {
                DragEnded?.Invoke(this, evt.position);
                return;
            }

            if (worldBound.Contains(evt.position))
            {
                Clicked?.Invoke(this, clickCount);
            }
        }

        private void ResetPress()
        {
            _pressPointerId = -1;
            _dragging = false;
        }
    }
}
