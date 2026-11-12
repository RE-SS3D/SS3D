using System;
using System.Collections.Generic;
using SS3D.Systems.Examine;
using SS3D.Systems.Inputs;
using SS3D.UI.Examine.Components;
using SS3D.UI.MachineInterface.Components;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Examine
{
    /// <summary>
    /// Persistent character-examine window — a paperdoll grid inside a <see cref="MachineWindow"/>.
    /// Unlike every other examine surface, it stays open until the × closes it (no auto-close on
    /// mouse-leave) — a deliberate departure from examine.md §4's hold-to-peek/no-click-lock rule,
    /// scoped to character examine only (see examine.md fork-deviations).
    /// <para>
    /// Click-to-take: click a filled slot (when <see cref="TakeAllowed"/>) raises
    /// <see cref="TakeHoldStarted"/>; the owning overlay starts a server delayed interaction.
    /// Progress is drawn via <see cref="BeginTakeProgress"/> after the server accepts. Cancel via
    /// the Cancel key, leaving range, or closing the window — not by releasing the mouse.
    /// </para>
    /// </summary>
    public sealed class CharacterExamineWindowView : IUiSurface
    {
        private const float SlotSize = 64f;
        private const float OpenOffsetX = 24f;
        private const float OpenOffsetY = 24f;

        public event Action CloseRequested;
        public event Action<CharacterExamineSlot> TakeHoldStarted;
        public event Action TakeHoldCancelled;

        private readonly StyleSheet _examineStyle;
        private readonly StyleSheet _inventorySlotStyle;
        private readonly StyleSheet _diegeticTokensStyle;
        private readonly StyleSheet _machineWindowStyle;
        private readonly CharacterExamineIconSet _icons;

        private VisualElement _root;
        private MachineWindow _window;
        private CharacterPaperdollGrid _grid;
        private Label _holdHintLabel;

        private CharacterExamineSlot? _pendingTakeSlot;
        private CharacterExamineSlot? _progressSlot;
        private float _progressDelay;
        private float _progressElapsed;

        public bool IsOpen { get; private set; }

        /// <summary>
        /// When false, pointer holds on slots do not start a take (living conscious examine-only).
        /// </summary>
        public bool TakeAllowed { get; set; }

        public CharacterExamineWindowView(
            StyleSheet examineStyle,
            StyleSheet inventorySlotStyle,
            StyleSheet diegeticTokensStyle,
            StyleSheet machineWindowStyle,
            CharacterExamineIconSet icons)
        {
            _examineStyle = examineStyle;
            _inventorySlotStyle = inventorySlotStyle;
            _diegeticTokensStyle = diegeticTokensStyle;
            _machineWindowStyle = machineWindowStyle;
            _icons = icons;
        }

        public void Attach(VisualElement layerRoot)
        {
            _root = new VisualElement { name = "character-examine-window-surface" };
            // Fill the overlay. flexGrow siblings stack and only cover a slice of the panel;
            // ScreenToPanel left/top are full-panel coords — relative to a bottom-third root
            // that pushed worldBound.y past the visible screen (wy~1166 with top~530).
            _root.style.position = Position.Absolute;
            _root.style.left = 0;
            _root.style.top = 0;
            _root.style.right = 0;
            _root.style.bottom = 0;
            _root.pickingMode = PickingMode.Ignore;

            if (_examineStyle != null)
            {
                _root.styleSheets.Add(_examineStyle);
            }

            if (_inventorySlotStyle != null)
            {
                _root.styleSheets.Add(_inventorySlotStyle);
            }

            if (_diegeticTokensStyle != null)
            {
                _root.styleSheets.Add(_diegeticTokensStyle);
            }

            layerRoot.Add(_root);

            BuildTree();
            SetVisible(false);
        }

        public void Detach()
        {
            _window = null;
            _grid = null;
            _holdHintLabel = null;
            _root?.RemoveFromHierarchy();
            _root = null;
        }

        /// <param name="screenPosition">
        /// Bottom-left screen pixels (mouse). Window opens near the cursor via
        /// <see cref="InputInterface.ScreenToPanel"/> — same panel-space rule as hover examine.
        /// </param>
        public void Show(
            string title,
            IReadOnlyList<CharacterExamineSlotContent> slots,
            bool takeAllowed,
            Vector2 screenPosition)
        {
            if (_window == null)
            {
                return;
            }

            TakeAllowed = takeAllowed;
            ApplySlots(title, slots);
            CancelLocalTakeGesture();
            ClearTakeProgress();
            IsOpen = true;
            SetVisible(true);
            PositionNearCursor(screenPosition);
            UpdateHoldHintIdle();
        }

        private void PositionNearCursor(Vector2 screenPositionBottomLeft)
        {
            if (_window == null)
            {
                return;
            }

            Vector2 panelPos = screenPositionBottomLeft;
            IPanel panel = _window.panel;
            if (panel != null)
            {
                panelPos = InputInterface.ScreenToPanel(panel, screenPositionBottomLeft);
            }

            // Pixel left/top (clear any percent/translate defaults) so drag ConvertToPixelPosition works.
            _window.style.translate = new Translate(0, 0);
            _window.style.left = panelPos.x + OpenOffsetX;
            _window.style.top = panelPos.y + OpenOffsetY;
            _window.style.right = StyleKeyword.Auto;
            _window.style.bottom = StyleKeyword.Auto;
        }

        public void RefreshSlots(string title, IReadOnlyList<CharacterExamineSlotContent> slots)
        {
            if (_window == null || !IsOpen)
            {
                return;
            }

            ApplySlots(title, slots);
        }

        public void Hide()
        {
            IsOpen = false;
            CancelLocalTakeGesture();
            ClearTakeProgress();
            SetVisible(false);
        }

        /// <summary>Starts the slot spinner after the server accepts the take windup.</summary>
        public void BeginTakeProgress(CharacterExamineSlot slot, float delaySeconds)
        {
            ClearTakeProgressVisualOnly();
            _pendingTakeSlot = null;
            _progressSlot = slot;
            _progressDelay = Mathf.Max(0.01f, delaySeconds);
            _progressElapsed = 0f;
            InventorySlot inventorySlot = _grid.GetSlot(slot);
            inventorySlot.AddToClassList("inventory-slot--drop-target");
            inventorySlot.SetTakeProgress(0.01f);
            _holdHintLabel.text = $"Taking: {inventorySlot.SlotLabel}";
            _holdHintLabel.style.display = DisplayStyle.Flex;
        }

        public void ClearTakeProgress()
        {
            ClearTakeProgressVisualOnly();
            _progressSlot = null;
            _progressDelay = 0f;
            _progressElapsed = 0f;
            UpdateHoldHintIdle();
        }

        /// <summary>Advances the confirmed take spinner. Driven by the owning subsystem's Update.</summary>
        public void Tick(float deltaTime)
        {
            if (_progressSlot == null)
            {
                return;
            }

            _progressElapsed += deltaTime;
            float progress = Mathf.Clamp01(_progressElapsed / _progressDelay);
            _grid.GetSlot(_progressSlot.Value).SetTakeProgress(Mathf.Max(0.01f, progress));
        }

        private void ApplySlots(string title, IReadOnlyList<CharacterExamineSlotContent> slots)
        {
            _window.Title = title;
            foreach (CharacterExamineSlotContent content in slots)
            {
                _grid.SetSlot(content);
            }
        }

        private void BuildTree()
        {
            _window = new MachineWindow();
            _window.CloseClicked += () => CloseRequested?.Invoke();
            _window.AddToClassList("character-examine-window");
            if (_machineWindowStyle != null)
            {
                _window.styleSheets.Add(_machineWindowStyle);
            }

            _grid = new CharacterPaperdollGrid(SlotSize, _icons);
            _window.Content.Add(_grid);

            foreach (CharacterExamineSlot slot in Enum.GetValues(typeof(CharacterExamineSlot)))
            {
                WireTakeClick(slot, _grid.GetSlot(slot));
            }

            _holdHintLabel = new Label();
            _holdHintLabel.AddToClassList("character-examine-window__hint");
            _holdHintLabel.AddToClassList("font-arcade");
            _holdHintLabel.style.display = DisplayStyle.None;
            _window.Content.Add(_holdHintLabel);

            _root.Add(_window);
        }

        private void WireTakeClick(CharacterExamineSlot slot, InventorySlot inventorySlot)
        {
            inventorySlot.RegisterCallback<ClickEvent>(_ => BeginTakeClick(slot, inventorySlot));
        }

        private void BeginTakeClick(CharacterExamineSlot slot, InventorySlot inventorySlot)
        {
            if (!TakeAllowed || (inventorySlot.ItemIcon == null && !inventorySlot.Unknown))
            {
                return;
            }

            // Click the in-progress slot again to cancel; otherwise replace any active windup.
            if (_progressSlot == slot || _pendingTakeSlot == slot)
            {
                CancelTakeGesture();
                return;
            }

            if (_progressSlot != null || _pendingTakeSlot != null)
            {
                CancelTakeGesture();
            }

            _pendingTakeSlot = slot;
            inventorySlot.AddToClassList("inventory-slot--drop-target");
            _holdHintLabel.text = $"Taking: {inventorySlot.SlotLabel}";
            _holdHintLabel.style.display = DisplayStyle.Flex;
            TakeHoldStarted?.Invoke(slot);
        }

        /// <summary>Cancels a pending/local take gesture and notifies the overlay to abort the server windup.</summary>
        public void CancelTakeGesture()
        {
            if (_pendingTakeSlot == null && _progressSlot == null)
            {
                return;
            }

            CancelLocalTakeGesture();
            ClearTakeProgressVisualOnly();
            _progressSlot = null;
            TakeHoldCancelled?.Invoke();
            UpdateHoldHintIdle();
        }

        private void CancelLocalTakeGesture()
        {
            if (_pendingTakeSlot != null)
            {
                _grid.GetSlot(_pendingTakeSlot.Value).RemoveFromClassList("inventory-slot--drop-target");
            }

            _pendingTakeSlot = null;
        }

        private void ClearTakeProgressVisualOnly()
        {
            if (_progressSlot != null)
            {
                InventorySlot slot = _grid.GetSlot(_progressSlot.Value);
                slot.ClearTakeProgress();
                slot.RemoveFromClassList("inventory-slot--drop-target");
            }
        }

        private void UpdateHoldHintIdle()
        {
            if (_holdHintLabel == null)
            {
                return;
            }

            if (TakeAllowed && IsOpen && _progressSlot == null && _pendingTakeSlot == null)
            {
                _holdHintLabel.text = "Click a slot to take";
                _holdHintLabel.style.display = DisplayStyle.Flex;
                return;
            }

            if (_progressSlot == null && _pendingTakeSlot == null)
            {
                _holdHintLabel.style.display = DisplayStyle.None;
            }
        }

        private void SetVisible(bool visible)
        {
            if (_window == null)
            {
                return;
            }

            _window.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
