using SS3D.Core.Debugging;
using System;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using SS3D.UI.MainHud.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud.Dev
{
    /// <summary>
    /// Dev-only panel to force every Alert Icon Stack hazard through None/Warning/Critical, without needing
    /// the hunger/thirst/pressure/radiation/pulling/restrained/low-oxygen/dying trackers that don't exist yet
    /// (see <see cref="MainHudSubSystem.SetDebugAlertOverride"/>). Toggled from the F1 debug
    /// window (<see cref="DebugToolNames.AlertStack"/>).
    /// <para>
    /// Anchored top-left so it does not cover the live alert stack (top-right). Reuses the Main HUD
    /// <see cref="PanelSettings"/> theme so labels actually render (a blank runtime PanelSettings has no font).
    /// </para>
    /// </summary>
    public sealed class AlertStackDebugMenuView : View
    {
        private static readonly AlertHazard[] Hazards = (AlertHazard[])Enum.GetValues(typeof(AlertHazard));

        private static readonly Color PanelBg = new(0.07f, 0.07f, 0.09f, 0.94f);
        private static readonly Color RowBg = new(0.14f, 0.14f, 0.16f, 1f);
        private static readonly Color ButtonIdle = new(0.22f, 0.22f, 0.25f, 1f);
        private static readonly Color ButtonOffActive = new(0.35f, 0.35f, 0.38f, 1f);
        private static readonly Color ButtonWarnActive = new(0.75f, 0.55f, 0.15f, 1f);
        private static readonly Color ButtonCritActive = new(0.75f, 0.2f, 0.2f, 1f);
        private static readonly Color ClearBg = new(0.45f, 0.15f, 0.15f, 1f);

        private static bool s_bootstrapped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (s_bootstrapped)
            {
                return;
            }

            s_bootstrapped = true;

            GameObject host = new(nameof(AlertStackDebugMenuView));
            DontDestroyOnLoad(host);
            host.AddComponent<AlertStackDebugMenuView>();
        }

        private UIDocument _document;
        private VisualElement _panel;
        private AlertStackState _state;
        private bool _visible;
        private bool _toggleBound;

        protected override void OnStart()
        {
            TryBindToggle();
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            TryBindToggle();
        }

        protected override void OnDisabled()
        {
            UnbindToggle();
            base.OnDisabled();
        }

        protected override void OnDestroyed()
        {
            UnbindToggle();

            if (_document != null)
            {
                InputInterface.UnregisterDocument(_document);
            }

            base.OnDestroyed();
        }

        private void TryBindToggle()
        {
            if (_toggleBound)
            {
                return;
            }

            DebugTool tool = DebugTools.Get(DebugToolNames.AlertStack);
            tool.Invoked += HandleToggle;
            tool.IsOn = () => _visible;
            _toggleBound = true;
        }

        private void UnbindToggle()
        {
            if (!_toggleBound)
            {
                return;
            }

            DebugTools.Get(DebugToolNames.AlertStack).Invoked -= HandleToggle;
            _toggleBound = false;
        }

        private void HandleToggle()
        {
            if (_document == null)
            {
                BuildUi();
                _visible = false;
                _panel.style.display = DisplayStyle.None;
            }

            _visible = !_visible;
            if (_visible)
            {
                _state = SubSystems.Get<MainHudSubSystem>()?.DebugAlertOverride ?? default;
                RefreshAllButtons();
            }

            _panel.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void BuildUi()
        {
            _state = SubSystems.Get<MainHudSubSystem>()?.DebugAlertOverride ?? default;

            _document = gameObject.AddComponent<UIDocument>();
            _document.panelSettings = ResolvePanelSettings();
            _document.sortingOrder = 2000f;

            _panel = new VisualElement();
            // Top-left: alert icons live in main-hud__zone--alerts (top-right).
            _panel.style.position = UnityEngine.UIElements.Position.Absolute;
            _panel.style.top = 16f;
            _panel.style.left = 16f;
            _panel.style.width = 360f;
            _panel.style.maxHeight = Length.Percent(90);
            _panel.style.paddingLeft = 12f;
            _panel.style.paddingRight = 12f;
            _panel.style.paddingTop = 12f;
            _panel.style.paddingBottom = 12f;
            _panel.style.backgroundColor = PanelBg;
            _panel.style.borderTopLeftRadius = 6f;
            _panel.style.borderTopRightRadius = 6f;
            _panel.style.borderBottomLeftRadius = 6f;
            _panel.style.borderBottomRightRadius = 6f;
            _panel.style.flexDirection = FlexDirection.Column;

            _panel.Add(BuildLabel("Alert Stack Debug", 15, FontStyle.Bold));
            _panel.Add(BuildHint("Icons render top-right. Pick a severity per hazard."));

            ScrollView list = new(ScrollViewMode.Vertical);
            list.style.flexGrow = 1f;
            list.style.marginTop = 8f;
            foreach (AlertHazard hazard in Hazards)
            {
                list.Add(BuildRow(hazard));
            }

            _panel.Add(list);

            Button clear = new(() =>
            {
                _state = default;
                SubSystems.Get<MainHudSubSystem>()?.ClearDebugAlertOverride();
                RefreshAllButtons();
            })
            {
                text = "Clear all",
            };
            StyleButton(clear, ClearBg);
            clear.style.marginTop = 10f;
            clear.style.height = 28f;
            _panel.Add(clear);

            _document.rootVisualElement.Add(_panel);
            InputInterface.RegisterDocument(_document);
        }

        private static PanelSettings ResolvePanelSettings()
        {
            // Blank runtime PanelSettings has no theme/font — labels render as empty boxes.
            MainHudAssetCatalog catalog =
                Resources.Load<MainHudAssetCatalog>(MainHudAssetPaths.ResourcesCatalogName);
            if (catalog?.PanelSettings != null)
            {
                return catalog.PanelSettings;
            }

            PanelSettings fallback = ScriptableObject.CreateInstance<PanelSettings>();
            fallback.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            fallback.referenceResolution = new Vector2Int(1200, 800);
            return fallback;
        }

        private VisualElement BuildRow(AlertHazard hazard)
        {
            VisualElement row = new() { name = $"row-{hazard}" };
            row.style.flexDirection = FlexDirection.Column;
            row.style.marginTop = 6f;
            row.style.paddingLeft = 8f;
            row.style.paddingRight = 8f;
            row.style.paddingTop = 6f;
            row.style.paddingBottom = 6f;
            row.style.backgroundColor = RowBg;
            row.style.borderTopLeftRadius = 4f;
            row.style.borderTopRightRadius = 4f;
            row.style.borderBottomLeftRadius = 4f;
            row.style.borderBottomRightRadius = 4f;

            row.Add(BuildLabel(HazardLabel(hazard), 12, FontStyle.Bold));

            VisualElement buttons = new();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.marginTop = 4f;
            buttons.style.justifyContent = Justify.SpaceBetween;

            buttons.Add(BuildSeverityButton(hazard, AlertSeverity.None, "Off"));
            // Dying / cardiac arrest have no warning tier — off or critical only.
            if (hazard != AlertHazard.Dying && hazard != AlertHazard.CardiacArrest)
            {
                buttons.Add(BuildSeverityButton(hazard, AlertSeverity.Warning, "Warning"));
            }

            buttons.Add(BuildSeverityButton(hazard, AlertSeverity.Critical, "Critical"));
            row.Add(buttons);
            return row;
        }

        private Button BuildSeverityButton(AlertHazard hazard, AlertSeverity severity, string label)
        {
            Button button = new(() =>
            {
                SetSeverity(hazard, severity);
                SubSystems.Get<MainHudSubSystem>()?.SetDebugAlertOverride(_state);
                RefreshRowButtons(hazard);
            })
            {
                text = label,
                name = ButtonName(hazard, severity),
            };

            button.style.flexGrow = 1f;
            button.style.marginRight = 4f;
            button.style.height = 24f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.fontSize = 11;
            ApplySeverityButtonStyle(button, hazard, severity);
            return button;
        }

        private void RefreshAllButtons()
        {
            foreach (AlertHazard hazard in Hazards)
            {
                RefreshRowButtons(hazard);
            }
        }

        private void RefreshRowButtons(AlertHazard hazard)
        {
            RefreshOneButton(hazard, AlertSeverity.None);
            if (hazard != AlertHazard.Dying && hazard != AlertHazard.CardiacArrest)
            {
                RefreshOneButton(hazard, AlertSeverity.Warning);
            }

            RefreshOneButton(hazard, AlertSeverity.Critical);
        }

        private void RefreshOneButton(AlertHazard hazard, AlertSeverity severity)
        {
            Button button = _panel.Q<Button>(ButtonName(hazard, severity));
            if (button != null)
            {
                ApplySeverityButtonStyle(button, hazard, severity);
            }
        }

        private void ApplySeverityButtonStyle(Button button, AlertHazard hazard, AlertSeverity severity)
        {
            bool active = _state[hazard] == severity;
            Color bg = severity switch
            {
                AlertSeverity.Warning => active ? ButtonWarnActive : ButtonIdle,
                AlertSeverity.Critical => active ? ButtonCritActive : ButtonIdle,
                _ => active ? ButtonOffActive : ButtonIdle,
            };
            StyleButton(button, bg);
        }

        private static void StyleButton(Button button, Color background)
        {
            button.style.backgroundColor = background;
            button.style.color = Color.white;
            button.style.borderTopWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderLeftWidth = 0;
            button.style.borderTopLeftRadius = 3f;
            button.style.borderTopRightRadius = 3f;
            button.style.borderBottomLeftRadius = 3f;
            button.style.borderBottomRightRadius = 3f;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        private void SetSeverity(AlertHazard hazard, AlertSeverity severity)
        {
            switch (hazard)
            {
                case AlertHazard.Fire: _state.Fire = severity; break;
                case AlertHazard.Hot: _state.Hot = severity; break;
                case AlertHazard.Cold: _state.Cold = severity; break;
                case AlertHazard.LowPressure: _state.LowPressure = severity; break;
                case AlertHazard.HighPressure: _state.HighPressure = severity; break;
                case AlertHazard.Radiation: _state.Radiation = severity; break;
                case AlertHazard.Hunger: _state.Hunger = severity; break;
                case AlertHazard.Thirst: _state.Thirst = severity; break;
                case AlertHazard.Pulling: _state.Pulling = severity; break;
                case AlertHazard.Restrained: _state.Restrained = severity; break;
                case AlertHazard.LowOxygen: _state.LowOxygen = severity; break;
                case AlertHazard.Dying: _state.Dying = severity; break;
                case AlertHazard.Bleeding: _state.Bleeding = severity; break;
                case AlertHazard.CardiacArrest: _state.CardiacArrest = severity; break;
                case AlertHazard.SealBreach: _state.SealBreach = severity; break;
            }
        }

        private static string ButtonName(AlertHazard hazard, AlertSeverity severity) =>
            $"btn-{hazard}-{severity}";

        private static string HazardLabel(AlertHazard hazard) => hazard switch
        {
            AlertHazard.LowPressure => "Low pressure",
            AlertHazard.HighPressure => "High pressure",
            AlertHazard.LowOxygen => "Low oxygen",
            AlertHazard.Dying => "Dying / critical",
            AlertHazard.CardiacArrest => "Cardiac arrest",
            AlertHazard.SealBreach => "Seal breach",
            _ => hazard.ToString(),
        };

        private static Label BuildLabel(string text, int fontSize, FontStyle fontStyle)
        {
            Label label = new(text);
            label.style.color = Color.white;
            label.style.fontSize = fontSize;
            label.style.unityFontStyleAndWeight = fontStyle;
            return label;
        }

        private static Label BuildHint(string text)
        {
            Label label = BuildLabel(text, 11, FontStyle.Normal);
            label.style.color = new Color(0.7f, 0.7f, 0.72f);
            label.style.marginTop = 2f;
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }
    }
}
