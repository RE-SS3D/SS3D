using System;
using SS3D.Interactions;
using SS3D.UI.MachineInterface.Components;
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

        private readonly StyleSheet[] _styleSheets;
        private readonly MainHudIconSet _icons;

        private VisualElement _root;
        private AlertIconStack _alertStack;
        private EquipmentGrid _equipmentGrid;
        private HandsGearStrip _handsGearStrip;
        private IntentModule _intentModule;
        private MachineWindow _examineWindow;
        private SelfExamineWindowContent _examineContent;

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
        }

        public void Detach()
        {
            _root?.RemoveFromHierarchy();
            _root = null;
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

        public void SetExamineOpen(bool isOpen)
        {
            _examineWindow.style.display = isOpen ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetExamineData(System.Collections.Generic.IReadOnlyList<LimbReadout> limbs, System.Collections.Generic.IReadOnlyList<OrganReadout> organs)
        {
            _examineContent.SetLimbs(limbs);
            _examineContent.SetOrgans(organs);
        }

        private void BuildTree()
        {
            _alertStack = new AlertIconStack();
            VisualElement alertZone = BuildZone("main-hud__zone--alerts", _alertStack);

            _equipmentGrid = new EquipmentGrid(_icons);
            VisualElement equipmentZone = BuildZone("main-hud__zone--equipment", _equipmentGrid);

            _handsGearStrip = new HandsGearStrip(_icons);
            VisualElement handsGearZone = BuildZone("main-hud__zone--hands-gear", _handsGearStrip);

            _intentModule = new IntentModule();
            _intentModule.ToggleRequested += () => IntentToggleRequested?.Invoke();
            VisualElement intentZone = BuildZone("main-hud__zone--intent", _intentModule);

            _examineContent = new SelfExamineWindowContent();
            _examineWindow = new MachineWindow { Title = "Self-Examine" };
            _examineWindow.Content.Add(_examineContent);
            _examineWindow.style.display = DisplayStyle.None;
            VisualElement examineZone = BuildZone("main-hud__zone--examine", _examineWindow);

            _root.Add(alertZone);
            _root.Add(equipmentZone);
            _root.Add(handsGearZone);
            _root.Add(intentZone);
            _root.Add(examineZone);
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
