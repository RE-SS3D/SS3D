using System;
using DG.Tweening;
using SS3D.Interactions;
using SS3D.UI.MainHud.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud
{
    /// <summary>
    /// Plain C# view for the always-on main HUD overlay - mirrors the Attach/Detach shape of
    /// <c>RadialInteractionMenuView</c>/<c>ArmedInteractionOverlayView</c>. Builds its tree in code rather than
    /// from an authored UXML template, same as <c>ArmedInteractionOverlayView</c> does.
    /// </summary>
    public sealed class MainHudView
    {
        private const float AnimDuration = 0.22f;
        private const float ShowScaleFrom = 0.92f;
        private const float ShowTranslateYFrom = 20f;

        public event Action IntentToggleRequested;

        /// <summary>
        /// Fired when a hand well is clicked. Argument is true for the left (first) hand slot.
        /// </summary>
        public event Action<bool> HandSelectedRequested;

        /// <summary>Fired when a gear-strip slot (belt/ID/pocket/back) is clicked.</summary>
        public event Action<HandsGearStrip.GearSlot> GearSlotClicked;

        /// <summary>Fired when an equipment-doll slot is clicked (equip/unequip vs active hand).</summary>
        public event Action<EquipmentGrid.Slot> EquipmentSlotClicked;

        public event Action<EquipmentGrid.Slot, Vector2> EquipmentDragStarted;
        public event Action<EquipmentGrid.Slot, Vector2> EquipmentDragMoved;
        public event Action<EquipmentGrid.Slot, Vector2> EquipmentDragEnded;

        public event Action<HandsGearStrip.GearSlot, Vector2> GearDragStarted;
        public event Action<HandsGearStrip.GearSlot, Vector2> GearDragMoved;
        public event Action<HandsGearStrip.GearSlot, Vector2> GearDragEnded;

        public event Action<HandsGearStrip.HandSlot, Vector2> HandDragStarted;
        public event Action<HandsGearStrip.HandSlot, Vector2> HandDragMoved;
        public event Action<HandsGearStrip.HandSlot, Vector2> HandDragEnded;

        private readonly StyleSheet[] _styleSheets;
        private readonly MainHudIconSet _icons;

        private VisualElement _root;
        private AlertIconStack _alertStack;
        private EquipmentGrid _equipmentGrid;
        private HandsGearStrip _handsGearStrip;
        private IntentModule _intentModule;
        private Sequence _visibilitySequence;
        private bool _visible;
        private float _scale = 1f;
        private float _translateY;

        public MainHudView(StyleSheet[] styleSheets, MainHudIconSet icons)
        {
            _styleSheets = styleSheets;
            _icons = icons;
        }

        public void Attach(VisualElement overlayRoot)
        {
            _root = new VisualElement();
            _root.AddToClassList("main-hud");
            _root.pickingMode = PickingMode.Ignore;

            foreach (StyleSheet styleSheet in _styleSheets)
            {
                if (styleSheet != null)
                {
                    _root.styleSheets.Add(styleSheet);
                }
            }

            BuildTree();
            overlayRoot.Add(_root);
            SetVisibleImmediate(false);
        }

        public void Detach()
        {
            KillVisibilitySequence();
            _root?.RemoveFromHierarchy();
            _root = null;
        }

        public void SetVisible(bool visible)
        {
            if (_root == null || _visible == visible)
            {
                return;
            }

            _visible = visible;
            if (visible)
            {
                PlayShow();
            }
            else
            {
                PlayHide();
            }
        }

        public void SetAlertState(AlertStackState state)
        {
            _alertStack.SetState(state);
        }

        public void SetIntent(IntentType intent)
        {
            _intentModule.SetIntent(intent);
        }

        public void SetEquipmentIcon(EquipmentGrid.Slot slot, UnityEngine.Sprite itemIcon)
        {
            SetEquipmentContents(slot, itemIcon, itemName: null);
        }

        public void SetEquipmentContents(EquipmentGrid.Slot slot, UnityEngine.Sprite itemIcon, string itemName)
        {
            _equipmentGrid.SetContents(slot, itemIcon, itemName);
        }

        public void SetGearIcon(HandsGearStrip.GearSlot slot, UnityEngine.Sprite itemIcon)
        {
            SetGearContents(slot, itemIcon, itemName: null);
        }

        public void SetGearContents(HandsGearStrip.GearSlot slot, UnityEngine.Sprite itemIcon, string itemName)
        {
            _handsGearStrip.SetGearContents(slot, itemIcon, itemName);
        }

        public void SetHandIcons(UnityEngine.Sprite leftItemIcon, UnityEngine.Sprite rightItemIcon)
        {
            _handsGearStrip.SetHandIcons(leftItemIcon, rightItemIcon);
        }

        public void SetHandContents(
            HandsGearStrip.HandSlot slot,
            UnityEngine.Sprite itemIcon,
            string itemName)
        {
            _handsGearStrip.SetHandContents(slot, itemIcon, itemName);
        }

        public void SetActiveHand(bool leftIsActive)
        {
            _handsGearStrip.SetActiveHand(leftIsActive);
        }

        /// <summary>Panel-space bounds of a gear slot, used to anchor its storage panel near the click.</summary>
        public UnityEngine.Rect GetGearSlotWorldBound(HandsGearStrip.GearSlot slot)
        {
            return _handsGearStrip.GetGearSlotWorldBound(slot);
        }

        public EquipmentGrid Equipment => _equipmentGrid;

        public HandsGearStrip HandsGear => _handsGearStrip;

        private void SetVisibleImmediate(bool visible)
        {
            KillVisibilitySequence();
            _visible = visible;
            if (_root == null)
            {
                return;
            }

            _scale = 1f;
            _translateY = 0f;
            _root.style.opacity = visible ? 1f : 0f;
            _root.style.scale = new Scale(Vector3.one);
            _root.style.translate = new Translate(0f, 0f);
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void PlayShow()
        {
            KillVisibilitySequence();

            _scale = ShowScaleFrom;
            _translateY = ShowTranslateYFrom;
            _root.style.display = DisplayStyle.Flex;
            _root.style.opacity = 0f;
            _root.style.scale = new Scale(new Vector3(_scale, _scale, 1f));
            _root.style.translate = new Translate(0f, _translateY);

            _visibilitySequence = DOTween.Sequence();
            _visibilitySequence.Append(DOTween.To(
                    () => _root.style.opacity.value,
                    value => _root.style.opacity = value,
                    1f,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
            _visibilitySequence.Join(DOTween.To(
                    () => _scale,
                    value =>
                    {
                        _scale = value;
                        _root.style.scale = new Scale(new Vector3(value, value, 1f));
                    },
                    1f,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
            _visibilitySequence.Join(DOTween.To(
                    () => _translateY,
                    value =>
                    {
                        _translateY = value;
                        _root.style.translate = new Translate(0f, value);
                    },
                    0f,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
        }

        private void PlayHide()
        {
            KillVisibilitySequence();

            _root.style.display = DisplayStyle.Flex;
            _visibilitySequence = DOTween.Sequence();
            _visibilitySequence.Append(DOTween.To(
                    () => _root.style.opacity.value,
                    value => _root.style.opacity = value,
                    0f,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
            _visibilitySequence.Join(DOTween.To(
                    () => _scale,
                    value =>
                    {
                        _scale = value;
                        _root.style.scale = new Scale(new Vector3(value, value, 1f));
                    },
                    ShowScaleFrom,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
            _visibilitySequence.Join(DOTween.To(
                    () => _translateY,
                    value =>
                    {
                        _translateY = value;
                        _root.style.translate = new Translate(0f, value);
                    },
                    ShowTranslateYFrom,
                    AnimDuration)
                .SetEase(Ease.OutCirc));
            _visibilitySequence.OnComplete(() =>
            {
                if (_root != null && !_visible)
                {
                    _root.style.display = DisplayStyle.None;
                }
            });
        }

        private void KillVisibilitySequence()
        {
            _visibilitySequence?.Kill();
            _visibilitySequence = null;
        }

        private void BuildTree()
        {
            _alertStack = new AlertIconStack();
            VisualElement alertZone = BuildZone("main-hud__zone--alerts", _alertStack);

            _equipmentGrid = new EquipmentGrid(_icons);
            _equipmentGrid.SlotClicked += slot => EquipmentSlotClicked?.Invoke(slot);
            _equipmentGrid.SlotDragStarted += (slot, pos) => EquipmentDragStarted?.Invoke(slot, pos);
            _equipmentGrid.SlotDragMoved += (slot, pos) => EquipmentDragMoved?.Invoke(slot, pos);
            _equipmentGrid.SlotDragEnded += (slot, pos) => EquipmentDragEnded?.Invoke(slot, pos);
            VisualElement equipmentZone = BuildZone("main-hud__zone--equipment", _equipmentGrid);

            _handsGearStrip = new HandsGearStrip(_icons);
            _handsGearStrip.HandClickRequested += leftIsActive => HandSelectedRequested?.Invoke(leftIsActive);
            _handsGearStrip.GearSlotClicked += slot => GearSlotClicked?.Invoke(slot);
            _handsGearStrip.GearDragStarted += (slot, pos) => GearDragStarted?.Invoke(slot, pos);
            _handsGearStrip.GearDragMoved += (slot, pos) => GearDragMoved?.Invoke(slot, pos);
            _handsGearStrip.GearDragEnded += (slot, pos) => GearDragEnded?.Invoke(slot, pos);
            _handsGearStrip.HandDragStarted += (slot, pos) => HandDragStarted?.Invoke(slot, pos);
            _handsGearStrip.HandDragMoved += (slot, pos) => HandDragMoved?.Invoke(slot, pos);
            _handsGearStrip.HandDragEnded += (slot, pos) => HandDragEnded?.Invoke(slot, pos);
            VisualElement handsGearZone = BuildZone("main-hud__zone--hands-gear", _handsGearStrip);

            _intentModule = new IntentModule();
            _intentModule.ToggleRequested += () => IntentToggleRequested?.Invoke();
            VisualElement intentZone = BuildZone("main-hud__zone--intent", _intentModule);

            _root.Add(alertZone);
            _root.Add(equipmentZone);
            _root.Add(handsGearZone);
            _root.Add(intentZone);
        }

        private static VisualElement BuildZone(string className, VisualElement child)
        {
            VisualElement zone = new();
            zone.AddToClassList("main-hud__zone");
            zone.AddToClassList(className);
            zone.pickingMode = PickingMode.Ignore;
            zone.Add(child);
            return zone;
        }
    }
}
