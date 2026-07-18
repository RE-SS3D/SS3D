using System;
using System.Collections.Generic;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.StoragePanel
{
    /// <summary>
    /// One on-demand storage panel: header (name + slot count), weight readout, and a slot grid —
    /// per Documents/design/inventory-storage.md §6 and the imported "Backpack Storage Panel" /
    /// "Looting Scene" Claude Design mockups. StoragePanelHost owns one of these per currently-open
    /// container; several can be visible side by side.
    /// <para>
    /// Width follows the container's column count (1×1, 2×2, 3×3, …). The header is draggable via
    /// UITK pointer capture (same pattern as <c>MachineWindow</c>); the host's
    /// <c>InputInterface.RegisterDocument</c> keeps those pointer hits from leaking into world clicks.
    /// </para>
    /// </summary>
    public sealed class StoragePanelView : VisualElement, IContainerPanel
    {
        public const float SlotSizePx = 72f;
        public const float SlotGapPx = 8f;
        public const float BodyPaddingPx = 16f;
        public const float MinPanelWidthPx = 160f;

        /// <summary>Fired when the player clicks this panel's close button (needs a server close request).</summary>
        public event Action CloseRequested;

        /// <summary>Fired when a drag gesture starts on one of this panel's slots. Position is panel-space.</summary>
        public event Action<StoragePanelView, StorageSlot, Vector2> SlotDragStarted;

        /// <summary>Fired as the pointer moves during a drag that started on this panel. Position is panel-space.</summary>
        public event Action<Vector2> SlotDragMoved;

        /// <summary>
        /// Fired when a drag gesture ends. Because pointer capture keeps events on the source slot,
        /// the source slot is passed alongside the release position — the actual drop target still
        /// needs to be resolved by hit-testing that position (see StoragePanelHost).
        /// </summary>
        public event Action<StoragePanelView, StorageSlot, Vector2> SlotDragEnded;

        /// <summary>Fired when a slot bound to a nested container item is clicked (Documents/design/inventory-storage.md §7).</summary>
        public event Action<StoragePanelView, StorageSlot> SlotNestedOpenRequested;

        private readonly VisualElement _header;
        private readonly VisualElement _closeButton;
        private readonly Label _titleLabel;
        private readonly Label _slotCountLabel;
        private readonly Label _weightLabel;
        private readonly VisualElement _weightFill;
        private readonly VisualElement _slotGrid;
        private readonly List<StorageSlot> _slots = new();

        private bool _isDragging;
        private Vector2 _dragStartPointer;
        private Vector2 _dragStartPosition;

        public AttachedContainer Container { get; private set; }

        public StoragePanelView(string originBreadcrumb)
        {
            AddToClassList("storage-panel");

            VisualElement clip = new();
            clip.AddToClassList("storage-panel__clip");

            _header = new VisualElement();
            _header.AddToClassList("storage-panel__header");

            _titleLabel = new Label();
            _titleLabel.AddToClassList("storage-panel__title");

            _slotCountLabel = new Label();
            _slotCountLabel.AddToClassList("storage-panel__slot-count");

            Button closeButton = new(() => CloseRequested?.Invoke()) { text = "×" };
            closeButton.AddToClassList("storage-panel__close");
            closeButton.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            _closeButton = closeButton;

            _header.Add(_titleLabel);
            _header.Add(_slotCountLabel);
            _header.Add(_closeButton);

            VisualElement body = new();
            body.AddToClassList("storage-panel__body");

            if (!string.IsNullOrEmpty(originBreadcrumb))
            {
                Label breadcrumb = new(originBreadcrumb);
                breadcrumb.AddToClassList("storage-panel__breadcrumb");
                body.Add(breadcrumb);
            }

            VisualElement weightRow = new();
            weightRow.AddToClassList("storage-panel__weight-row");
            Label weightCaption = new("WEIGHT");
            weightCaption.AddToClassList("storage-panel__weight-caption");
            _weightLabel = new Label();
            _weightLabel.AddToClassList("storage-panel__weight-value");
            weightRow.Add(weightCaption);
            weightRow.Add(_weightLabel);

            VisualElement weightTrack = new();
            weightTrack.AddToClassList("storage-panel__weight-track");
            VisualElement weightTrackClip = new();
            weightTrackClip.AddToClassList("storage-panel__weight-track-clip");
            _weightFill = new VisualElement();
            _weightFill.AddToClassList("storage-panel__weight-fill");
            weightTrackClip.Add(_weightFill);
            weightTrack.Add(weightTrackClip);

            _slotGrid = new VisualElement();
            _slotGrid.AddToClassList("storage-panel__grid");

            body.Add(weightRow);
            body.Add(weightTrack);
            body.Add(_slotGrid);

            clip.Add(_header);
            clip.Add(body);
            Add(clip);

            RegisterHeaderDragHandlers();
        }

        public void Bind(AttachedContainer container)
        {
            Container = container;
            _titleLabel.text = container.ContainerName;

            Vector2Int size = new(Mathf.Max(0, container.Size.x), Mathf.Max(0, container.Size.y));
            int slotCount = size.x * size.y;
            _slotCountLabel.text = $"{slotCount} SLOTS";

            RebuildGrid(size);
            ApplyPanelWidth(Mathf.Max(1, size.x));
            RefreshContents();

            container.OnContentsChanged += HandleContentsChanged;
        }

        public void Unbind()
        {
            if (Container != null)
            {
                Container.OnContentsChanged -= HandleContentsChanged;
            }
        }

        /// <summary>Finds the slot (if any) whose world bound contains the given panel-space position.</summary>
        public StorageSlot HitTestSlot(Vector2 panelPosition)
        {
            foreach (StorageSlot slot in _slots)
            {
                if (slot.worldBound.Contains(panelPosition))
                {
                    return slot;
                }
            }

            return null;
        }

        private void RegisterHeaderDragHandlers()
        {
            _header.RegisterCallback<PointerDownEvent>(OnHeaderPointerDown);
            _header.RegisterCallback<PointerMoveEvent>(OnHeaderPointerMove);
            _header.RegisterCallback<PointerUpEvent>(OnHeaderPointerUp);
            _header.RegisterCallback<PointerCaptureOutEvent>(_ => EndHeaderDrag());
        }

        private void OnHeaderPointerDown(PointerDownEvent evt)
        {
            if (IsOverCloseButton(evt.target))
            {
                return;
            }

            ConvertToPixelPosition();
            BringToFront();
            _isDragging = true;
            _dragStartPointer = evt.position;
            _dragStartPosition = new Vector2(resolvedStyle.left, resolvedStyle.top);
            _header.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnHeaderPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging)
            {
                return;
            }

            Vector2 delta = (Vector2)evt.position - _dragStartPointer;
            Vector2 newPosition = _dragStartPosition + delta;
            ClampToParent(ref newPosition);
            style.left = newPosition.x;
            style.top = newPosition.y;
            evt.StopPropagation();
        }

        private void OnHeaderPointerUp(PointerUpEvent evt)
        {
            EndHeaderDrag();
            evt.StopPropagation();
        }

        private void EndHeaderDrag()
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;
            if (_header.HasPointerCapture(PointerId.mousePointerId))
            {
                _header.ReleasePointer(PointerId.mousePointerId);
            }
        }

        private bool IsOverCloseButton(IEventHandler target)
        {
            if (target is not VisualElement element)
            {
                return false;
            }

            return element == _closeButton || _closeButton.Contains(element);
        }

        private void ConvertToPixelPosition()
        {
            VisualElement parentElement = parent;
            if (parentElement == null)
            {
                return;
            }

            Rect parentBounds = parentElement.worldBound;
            Rect selfBounds = worldBound;

            style.translate = new Translate(0, 0);
            style.left = selfBounds.x - parentBounds.x;
            style.top = selfBounds.y - parentBounds.y;
        }

        private void ClampToParent(ref Vector2 position)
        {
            VisualElement parentElement = parent;
            if (parentElement == null)
            {
                return;
            }

            float panelWidth = resolvedStyle.width;
            float panelHeight = resolvedStyle.height;
            float parentWidth = parentElement.resolvedStyle.width;
            float parentHeight = parentElement.resolvedStyle.height;

            if (float.IsNaN(panelWidth) || panelWidth <= 0f)
            {
                panelWidth = MinPanelWidthPx;
            }

            if (float.IsNaN(panelHeight) || panelHeight <= 0f)
            {
                panelHeight = 200f;
            }

            position.x = Mathf.Clamp(position.x, 0f, Mathf.Max(0f, parentWidth - panelWidth));
            position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, parentHeight - panelHeight));
        }

        private void HandleContentsChanged(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type)
        {
            RefreshContents();
        }

        private void RebuildGrid(Vector2Int size)
        {
            _slotGrid.Clear();
            _slots.Clear();

            for (int y = 0; y < size.y; y++)
            {
                VisualElement row = new();
                row.AddToClassList("storage-panel__grid-row");
                if (y < size.y - 1)
                {
                    row.AddToClassList("storage-panel__grid-row--spaced");
                }

                for (int x = 0; x < size.x; x++)
                {
                    StorageSlot slot = new();
                    if (x < size.x - 1)
                    {
                        slot.AddToClassList("storage-slot--gap-right");
                    }

                    slot.DragStarted += (s, pos) => SlotDragStarted?.Invoke(this, s, pos);
                    slot.DragMoved += (_, pos) => SlotDragMoved?.Invoke(pos);
                    slot.DragEnded += (s, pos) => SlotDragEnded?.Invoke(this, s, pos);
                    slot.NestedOpenRequested += s => SlotNestedOpenRequested?.Invoke(this, s);
                    _slots.Add(slot);
                    row.Add(slot);
                }

                _slotGrid.Add(row);
            }
        }

        private void ApplyPanelWidth(int columns)
        {
            float gridWidth = (columns * SlotSizePx) + (Mathf.Max(0, columns - 1) * SlotGapPx);
            float panelWidth = Mathf.Max(MinPanelWidthPx, gridWidth + (BodyPaddingPx * 2f));
            style.width = panelWidth;
        }

        private void RefreshContents()
        {
            if (Container == null)
            {
                return;
            }

            for (int y = 0; y < Container.Size.y; y++)
            {
                for (int x = 0; x < Container.Size.x; x++)
                {
                    int index = (y * Container.Size.x) + x;
                    if (index >= _slots.Count)
                    {
                        continue;
                    }

                    Vector2Int position = new(x, y);
                    Item item = Container.ItemAt(position);
                    _slots[index].Position = position;
                    _slots[index].Bind(item);
                }
            }

            RefreshWeightReadout();
        }

        private void RefreshWeightReadout()
        {
            float weight = Container.Weight;
            float capacity = Mathf.Max(0.01f, Container.MaxWeight);

            _weightLabel.text = $"{weight:0.0} / {capacity:0.#} KG";

            float pct = Mathf.Clamp01(weight / capacity);
            _weightFill.style.width = new Length(pct * 100f, LengthUnit.Percent);

            _weightFill.RemoveFromClassList("storage-panel__weight-fill--warning");
            _weightFill.RemoveFromClassList("storage-panel__weight-fill--danger");

            if (pct >= 0.9f)
            {
                _weightFill.AddToClassList("storage-panel__weight-fill--danger");
            }
            else if (pct >= 0.7f)
            {
                _weightFill.AddToClassList("storage-panel__weight-fill--warning");
            }
        }

        /// <summary>
        /// Local visual teardown only, per IContainerPanel — called when the container's own open
        /// state changes server-side. StoragePanelHost.RequestClose is the player-initiated path.
        /// </summary>
        void IContainerPanel.Close()
        {
            EndHeaderDrag();
            Unbind();
            RemoveFromHierarchy();
        }
    }
}
