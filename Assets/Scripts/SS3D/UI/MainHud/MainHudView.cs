using System;
using SS3D.Interactions;
using SS3D.UI.MainHud.Components;
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
        public event Action IntentToggleRequested;

        /// <summary>
        /// Fired when a hand well is clicked. Argument is true for the left (first) hand slot.
        /// </summary>
        public event Action<bool> HandSelectedRequested;

        /// <summary>Fired when a gear-strip slot (belt/ID/PDA/back) is clicked.</summary>
        public event Action<HandsGearStrip.GearSlot> GearSlotClicked;

        private readonly StyleSheet[] _styleSheets;
        private readonly MainHudIconSet _icons;

        private VisualElement _root;
        private AlertIconStack _alertStack;
        private EquipmentGrid _equipmentGrid;
        private HandsGearStrip _handsGearStrip;
        private IntentModule _intentModule;

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
            SetVisible(false);
        }

        public void Detach()
        {
            _root?.RemoveFromHierarchy();
            _root = null;
        }

        public void SetVisible(bool visible)
        {
            if (_root == null)
            {
                return;
            }

            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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
            _equipmentGrid.SetIcon(slot, itemIcon);
        }

        public void SetGearIcon(HandsGearStrip.GearSlot slot, UnityEngine.Sprite itemIcon)
        {
            _handsGearStrip.SetGearIcon(slot, itemIcon);
        }

        public void SetHandIcons(UnityEngine.Sprite leftItemIcon, UnityEngine.Sprite rightItemIcon)
        {
            _handsGearStrip.SetHandIcons(leftItemIcon, rightItemIcon);
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

        private void BuildTree()
        {
            _alertStack = new AlertIconStack();
            VisualElement alertZone = BuildZone("main-hud__zone--alerts", _alertStack);

            _equipmentGrid = new EquipmentGrid(_icons);
            VisualElement equipmentZone = BuildZone("main-hud__zone--equipment", _equipmentGrid);

            _handsGearStrip = new HandsGearStrip(_icons);
            _handsGearStrip.HandClickRequested += leftIsActive => HandSelectedRequested?.Invoke(leftIsActive);
            _handsGearStrip.GearSlotClicked += slot => GearSlotClicked?.Invoke(slot);
            VisualElement handsGearZone = BuildZone("main-hud__zone--hands-gear", _handsGearStrip);

            _intentModule = new IntentModule();
            _intentModule.ToggleRequested += () => IntentToggleRequested?.Invoke();
            VisualElement intentZone = BuildZone("main-hud__zone--intent", _intentModule);

            _root.Add(alertZone);
            _root.Add(equipmentZone);
            _root.Add(handsGearZone);
            _root.Add(intentZone);
        }

        private static VisualElement BuildZone(string className, VisualElement content)
        {
            VisualElement zone = new();
            zone.AddToClassList("main-hud__zone");
            zone.AddToClassList(className);
            zone.pickingMode = PickingMode.Ignore;
            content.pickingMode = PickingMode.Position;
            zone.Add(content);
            return zone;
        }
    }
}
