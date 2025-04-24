using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Networking;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Launcher
{
    public class LauncherView : View
    {
        [SerializeField]
        private UIDocument _document;

        private VisualElement _root;

        private RadioButtonGroup _networkModeSelectionGroup;

        private TextField _usernameTextField;
        private TextField _ipAddressTextField;
        private TextField _portTextField;

        private Button _startGameButton;

        protected override void OnAwake()
        {
            base.OnAwake();

            CreateUI();
            RegisterUIEvents();
        }

        private void RegisterUIEvents()
        {
            _startGameButton.clickable.clicked += HandleStartGameButtonPressed;
        }

        private void CreateUI()
        {
            _root = _document.rootVisualElement;

            _networkModeSelectionGroup = _root.Q<RadioButtonGroup>("network-mode-radio-button-group");

            _networkModeSelectionGroup.choices = new[]
            {
                NetworkType.DedicatedServer.ToString(),
                NetworkType.Client.ToString(),
                NetworkType.Host.ToString(),
            };

            _usernameTextField = _root.Q<TextField>("username-text-field");
            _ipAddressTextField = _root.Q<TextField>("ip-text-field");
            _portTextField = _root.Q<TextField>("port-text-field");

            _startGameButton = _root.Q<Button>("start-game-button");
        }

        private void HandleStartGameButtonPressed()
        {
            LaunchGame();
        }

        private void LaunchGame()
        {
            NetworkType networkType = (NetworkType)_networkModeSelectionGroup.value;

            string ckey = _usernameTextField.value;
            string ip = _ipAddressTextField.value;
            string port = _portTextField.value;

            SubSystems.Get<LauncherSubSystem>().LaunchGame(networkType, ckey, ip, port);
        }
    }
}