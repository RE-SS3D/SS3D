using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using Coimbra.Services.Events;
using Coimbra.Services.PlayerLoopEvents;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Inputs;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using Actor = SS3D.Core.Behaviours.Actor;
using InputSubSystem = SS3D.Systems.Inputs.InputSubSystem;

namespace SS3D.Systems.IngameConsoleSystem
{
    public class ConsolePanelView : Actor
    {
        [SerializeField] private RectTransform _consolePanel;
        /// <summary>
        /// Object, that contains command responses
        /// </summary>
        [SerializeField] private GameObject _contentContainer;
        /// <summary>
        /// Opening/closing speed
        /// </summary>
        [SerializeField] private float _movingSpeed = 2500f;
        [SerializeField] private TMP_InputField _inputField;
        // Used for opening/closing
        private bool _isSliding;
        private bool _isShowed;
        private Vector2 _targetPointMax;
        private Vector2 _targetPointMin;
        /// <summary>
        /// Text field with command responses in _contentContainer
        /// </summary>
        private TextMeshProUGUI _textField;

        [SerializeField] private CommandsController _commandsController;

        // Used for choosing command via arrows
        [SerializeField] private List<string> _allPrevCommands = new() {""};
        private int _chosenPrevCommand;
        private Controls _controls;
        private Controls.ConsoleActions _consoleControls;
        private InputSubSystem _inputSystem;

        protected override void OnStart()
        {
            base.OnStart();
            _textField = _contentContainer.GetComponent<TextMeshProUGUI>();
            _inputSystem = SubSystems.Get<InputSubSystem>();
            _controls = _inputSystem.Inputs;
            _consoleControls = _controls.Console;
            _inputSystem.ToggleAction(_consoleControls.Open, true);

            AddHandle(UpdateEvent.AddListener(HandleUpdate));
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();

            InputSubSystem inputSystem = SubSystems.Get<InputSubSystem>();

            if (inputSystem && inputSystem.Inputs != null)
            {
                Controls.ConsoleActions consoleInputs = inputSystem.Inputs.Console;

                consoleInputs.Close.performed += HandleClose;
                consoleInputs.Open.performed += HandleOpen;
                consoleInputs.SwitchCommand.performed += HandleSwitchCommand;
                consoleInputs.Submit.performed += HandleSubmit;
            }
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();

            InputSubSystem inputSystem = SubSystems.Get<InputSubSystem>();

            if (inputSystem && inputSystem.Inputs != null)
            {
                Controls.ConsoleActions consoleInputs = inputSystem.Inputs.Console;

                consoleInputs.Close.performed -= HandleClose;
                consoleInputs.Open.performed -= HandleOpen;
                consoleInputs.SwitchCommand.performed -= HandleSwitchCommand;
                consoleInputs.Submit.performed -= HandleSubmit;
            }
        }

        private void HandleUpdate(ref EventContext context, in UpdateEvent updateEvent)
        {
            if (_isSliding)
            {
                Slide();
            }
        }

        /// <summary>
        /// Move the console offscreen and disable all console controls, except of Open action
        /// </summary>
        private void HandleClose(InputAction.CallbackContext context)
        {
            _isSliding = true;
            _targetPointMin = Vector2.zero;
            _targetPointMax = _targetPointMin + new Vector2(0, _consolePanel.rect.height);
            _inputField.DeactivateInputField();
            _inputSystem.ToggleAllActions(true, ((InputActionMap)_consoleControls).ToArray());
            _inputSystem.ToggleAction(_consoleControls.Open, true);
            _inputSystem.ToggleActionMap(_consoleControls, false, new []{_consoleControls.Open});
        }
        /// <summary>
        /// Move console to screen, enable all controls, disable Open action
        /// </summary>
        private void HandleOpen(InputAction.CallbackContext context)
        {
            _isSliding = true;
            _targetPointMin = new Vector2(0, -_consolePanel.rect.height);
            _targetPointMax = _targetPointMin + new Vector2(0, _consolePanel.rect.height);
            _inputField.ActivateInputField();
            _inputSystem.ToggleAllActions(false, ((InputActionMap)_consoleControls).ToArray());
            _inputSystem.ToggleActionMap(_consoleControls, true, new []{_consoleControls.Open});
            _inputSystem.ToggleAction(_consoleControls.Open, false);
        }
        /// <summary>
        /// Put previously used commands in input field
        /// </summary>
        private void HandleSwitchCommand(InputAction.CallbackContext context)
        {
            _chosenPrevCommand =
                Math.Clamp(_chosenPrevCommand + (int)context.ReadValue<float>(), 0, _allPrevCommands.Count - 1);
            _inputField.text = _allPrevCommands[_chosenPrevCommand];
        }
        /// <summary>
        /// Process and remove command in input field
        /// </summary>
        private void HandleSubmit(InputAction.CallbackContext context)
        {
            ProcessCommand(_inputField.text);
            _inputField.text = "";
            _inputField.ActivateInputField();
        }
        
        private void Slide()
        {
            _consolePanel.offsetMax = Vector2.MoveTowards(_consolePanel.offsetMax, _targetPointMax, _movingSpeed * Time.deltaTime);
            _consolePanel.offsetMin = Vector2.MoveTowards(_consolePanel.offsetMin, _targetPointMin, _movingSpeed * Time.deltaTime);
            if (_consolePanel.offsetMin == _targetPointMin)
            {
                _isSliding = false;
                _isShowed = !_isShowed;
            }
        }
        /// <summary>
        /// Handle command taking from input field and showing a response 
        /// </summary>
        [Client]
        public void ProcessCommand(string command)
        {
            AddText("> <color=#742F27>" + command + "</color>");
            _allPrevCommands.RemoveLast();
            _allPrevCommands.Add(command);
            _allPrevCommands.Add("");
            _chosenPrevCommand = _allPrevCommands.Count;
            _commandsController.ClientProcessCommand(command);
        }

        [Client]
        public void AddText(string text)
        {
            _textField.text += "\n" + text;
        }
    }
}
