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
    /// Documents/architecture/2026-07_inventory-storage-redesign.md "Scope decisions" on why this is a
    /// new UITK implementation rather than the world-space InteractionTier.Combine grammar).
    /// </summary>
    public class StorageSlot : InventorySlot
    {
        /// <summary>Fired when a drag gesture starts on a bound (non-empty) slot. Position is panel-space.</summary>
        public event Action<StorageSlot, Vector2> DragStarted;

        /// <summary>Fired as the pointer moves while this slot holds the drag capture. Position is panel-space.</summary>
        public event Action<StorageSlot, Vector2> DragMoved;

        /// <summary>Fired when the drag gesture ends (pointer released). Position is panel-space (release point).</summary>
        public event Action<StorageSlot, Vector2> DragEnded;

        /// <summary>
        /// Fired on a plain click of a slot bound to an item that is itself a container (e.g. a
        /// lockbox sitting in an open locker) — opens it as a nested panel
        /// (Documents/design/inventory-storage.md §7).
        /// </summary>
        public event Action<StorageSlot> NestedOpenRequested;

        private readonly Label _stackLabel;

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
            RegisterCallback<ClickEvent>(OnClicked);
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
            if (BoundItem == null)
            {
                return;
            }

            this.CapturePointer(evt.pointerId);
            DragStarted?.Invoke(this, evt.position);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            DragMoved?.Invoke(this, evt.position);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!this.HasPointerCapture(evt.pointerId))
            {
                return;
            }

            this.ReleasePointer(evt.pointerId);
            DragEnded?.Invoke(this, evt.position);
        }

        private void OnClicked(ClickEvent evt)
        {
            if (BoundItem != null && BoundItem.GetComponent<AttachedContainer>() != null)
            {
                NestedOpenRequested?.Invoke(this);
            }
        }
    }
}
