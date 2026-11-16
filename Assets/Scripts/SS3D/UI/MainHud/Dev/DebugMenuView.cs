using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Debugging;
using SS3D.Systems.Inputs;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud.Dev
{
    /// <summary>
    /// F1 debug window (editor and development builds): one button per registered <see cref="DebugTool"/>, showing
    /// On/Off for toggles and disabled while nothing in the scene answers. Replaces the scattered debug keys; F12
    /// stays the console.
    /// </summary>
    public sealed class DebugMenuView : View
    {
        private static readonly Color PanelBg = new(0.07f, 0.07f, 0.09f, 0.94f);
        private static readonly Color ButtonIdle = new(0.22f, 0.22f, 0.25f, 1f);
        private static readonly Color ButtonOn = new(0.25f, 0.45f, 0.7f, 1f);
        private const long RefreshMilliseconds = 250;

        private static bool s_bootstrapped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_bootstrapped = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (s_bootstrapped || !Debug.isDebugBuild)
            {
                return;
            }

            s_bootstrapped = true;
            GameObject host = new(nameof(DebugMenuView));
            DontDestroyOnLoad(host);
            host.AddComponent<DebugMenuView>();
        }

        private readonly Dictionary<DebugTool, Button> _buttons = new();
        private UIDocument _document;
        private VisualElement _panel;
        private VisualElement _list;
        private InputSubSystem _inputSystem;
        private bool _visible;

        protected override void OnStart()
        {
            base.OnStart();
            if (SubSystems.TryGet(out _inputSystem) && _inputSystem.ToggleDebugMenu != null)
            {
                _inputSystem.ToggleDebugMenu.performed += HandleToggle;
            }
        }

        protected override void OnDestroyed()
        {
            if (_inputSystem != null && _inputSystem.ToggleDebugMenu != null)
            {
                _inputSystem.ToggleDebugMenu.performed -= HandleToggle;
            }

            if (_document != null)
            {
                InputInterface.UnregisterDocument(_document);
            }

            base.OnDestroyed();
        }

        private void HandleToggle(InputAction.CallbackContext context)
        {
            if (_document == null)
            {
                BuildUi();
            }

            _visible = !_visible;
            _panel.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (_visible)
            {
                Refresh();
            }
        }

        private void BuildUi()
        {
            _document = gameObject.AddComponent<UIDocument>();
            _document.panelSettings = Resources.Load<MainHudAssetCatalog>(MainHudAssetPaths.ResourcesCatalogName)?.PanelSettings;
            _document.sortingOrder = 2001f;
            InputInterface.RegisterDocument(_document);

            _panel = new VisualElement { name = "debug-menu" };
            _panel.style.position = UnityEngine.UIElements.Position.Absolute;
            _panel.style.top = 16f;
            _panel.style.right = 16f;
            _panel.style.width = 260f;
            _panel.style.paddingLeft = 12f;
            _panel.style.paddingRight = 12f;
            _panel.style.paddingTop = 12f;
            _panel.style.paddingBottom = 12f;
            _panel.style.backgroundColor = PanelBg;

            Label title = new("Debug (F1)");
            title.style.fontSize = 15;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = Color.white;
            title.style.marginBottom = 8f;
            _panel.Add(title);

            _list = new VisualElement();
            _panel.Add(_list);
            _document.rootVisualElement.Add(_panel);
            _panel.schedule.Execute(() =>
            {
                if (_visible) Refresh();
            }).Every(RefreshMilliseconds);
        }

        /// <summary>Adds buttons for newly registered tools and updates every label and enabled state.</summary>
        private void Refresh()
        {
            foreach (DebugTool tool in DebugTools.All)
            {
                if (!_buttons.TryGetValue(tool, out Button button))
                {
                    DebugTool target = tool;
                    button = new Button(() =>
                    {
                        target.Invoke();
                        Refresh();
                    });
                    button.style.marginTop = 4f;
                    button.style.height = 30f;
                    button.style.color = Color.white;
                    button.style.unityTextAlign = TextAnchor.MiddleLeft;
                    _buttons[tool] = button;
                    _list.Add(button);
                }

                bool on = tool.IsToggle && tool.IsOn();
                button.text = tool.IsToggle ? $"{tool.Name}: {(on ? "On" : "Off")}" : tool.Name;
                button.style.backgroundColor = on ? ButtonOn : ButtonIdle;
                button.SetEnabled(tool.Available);
                button.tooltip = tool.Available ? null : "Nothing in this scene answers this tool yet.";
            }
        }
    }
}
