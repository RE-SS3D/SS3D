using SS3D.Core;
using SS3D.Systems.Inputs;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Entities
{
    /// <summary>
    /// Ghost-screen Respawn button with its cooldown (<c>sandbox-server.md</c> §4). Shown on the
    /// UiShell HUD layer only after the server offered a respawn to this client; the server decides.
    /// Added at runtime by <see cref="EntitySubSystem"/> on clients.
    /// </summary>
    public sealed class RespawnPrompt : MonoBehaviour
    {
        private EntitySubSystem _entities;
        private VisualElement _panel;
        private Label _label;
        private Button _button;
        private float _availableAt = -1f;
        private bool _requested;

        public bool IsShown => _panel != null && _panel.style.display == DisplayStyle.Flex;

        public bool CanRespawn => IsShown && _button.enabledSelf;

        private void OnEnable()
        {
            _entities = GetComponent<EntitySubSystem>();
            _entities.LocalRespawnOffered += HandleOffered;
            _entities.LocalRespawnWithdrawn += Hide;
        }

        private void OnDisable()
        {
            _entities.LocalRespawnOffered -= HandleOffered;
            _entities.LocalRespawnWithdrawn -= Hide;
            _panel?.RemoveFromHierarchy();
            _panel = null;
        }

        /// <summary>Sends the respawn request, as the button does.</summary>
        public void Respawn()
        {
            if (_requested)
                return;

            _requested = true;
            _button.SetEnabled(false);
            _entities.CmdRespawn();
        }

        private void HandleOffered(int secondsFromNow)
        {
            _availableAt = Time.unscaledTime + secondsFromNow;
            _requested = false;
            if (EnsurePanel())
                _panel.style.display = DisplayStyle.Flex;
            Refresh();
        }

        private void Hide()
        {
            _availableAt = -1f;
            _requested = false;
            if (_panel != null)
                _panel.style.display = DisplayStyle.None;
        }

        private void Update()
        {
            if (_availableAt < 0f)
                return;

            // The round ended or this client lost its ghost (lobby, reconnect): the offer is stale.
            if (!_entities.IsPlayerSpawned(_entities.LocalConnection))
            {
                Hide();
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            if (_panel == null)
                return;

            int left = Mathf.Max(0, Mathf.CeilToInt(_availableAt - Time.unscaledTime));
            _label.text = left > 0 ? $"You died. Respawn in {left / 60}:{left % 60:00}" : "You died.";
            _button.SetEnabled(left == 0 && !_requested);
        }

        private bool EnsurePanel()
        {
            if (_panel != null)
                return true;

            if (!SubSystems.TryGet(out UiShellSubSystem shell) || !shell.TryGetLayer(UiLayer.Hud, out VisualElement layer))
                return false;

            InputInterface.RegisterDocument(shell.Document);
            _panel = new VisualElement { name = "respawn-prompt", pickingMode = PickingMode.Position };
            _panel.style.position = Position.Absolute;
            _panel.style.bottom = 120;
            _panel.style.left = new Length(50, LengthUnit.Percent);
            _panel.style.translate = new Translate(new Length(-50, LengthUnit.Percent), 0);
            _panel.style.flexDirection = FlexDirection.Row;
            _panel.style.alignItems = Align.Center;
            _panel.style.paddingLeft = _panel.style.paddingRight = 14;
            _panel.style.paddingTop = _panel.style.paddingBottom = 8;
            _panel.style.backgroundColor = new Color(0.05f, 0.07f, 0.1f, 0.85f);
            _panel.style.borderTopLeftRadius = _panel.style.borderTopRightRadius = 4;
            _panel.style.borderBottomLeftRadius = _panel.style.borderBottomRightRadius = 4;

            _label = new Label { name = "respawn-prompt-label", pickingMode = PickingMode.Ignore };
            _label.style.color = new Color(0.9f, 0.92f, 0.95f);
            _label.style.fontSize = 16;
            _label.style.marginRight = 12;

            _button = new Button(Respawn) { name = "respawn-prompt-button", text = "Respawn" };
            _button.style.fontSize = 16;

            _panel.Add(_label);
            _panel.Add(_button);
            _panel.style.display = DisplayStyle.None;
            layer.Add(_panel);
            return true;
        }
    }
}
