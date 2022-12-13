using System;
using System.Collections.Generic;
using Coimbra;
using SS3D.Core.Behaviours;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.IngameConsoleSystem
{
    public class ConsolePanelView : SpessBehaviour
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
        private bool _isSliding = false;
        private bool _isShowed = false;
        private Vector2 _targetPointMax;
        private Vector2 _targetPointMin;
        /// <summary>
        /// Text field with command responses in _contentContainer
        /// </summary>
        private TextMeshProUGUI _textField;
        private CommandsController _commandsController;
        // Used for choosing command via arrows
        [SerializeField] private List<string> _allPrevCommands = new() {""};
        private int _chosenPrevCommand = 0;

        protected override void OnStart()
        {
            base.OnStart();
            _textField = _contentContainer.GetComponent<TextMeshProUGUI>();
            //_inputField.resetOnDeActivation = true;
            _commandsController = new CommandsController();
        }

        protected override void HandleUpdate(in float deltaTime)
        {
            base.HandleUpdate(in deltaTime);
            
            ProcessInput();
            if (_isSliding)
            {
                Slide();
            }
        }
    
        private void ProcessInput()
        {
            if (Input.GetKeyUp(KeyCode.F12))
            {
                _isSliding = true;
                if (_isShowed)
                {
                    _targetPointMin = Vector2.zero;
                    _inputField.ReleaseSelection();
                    _inputField.DeactivateInputField();
                }
                else
                {
                    _targetPointMin = new Vector2(0, -_consolePanel.rect.height);
                    _inputField.Select();
                    _inputField.ActivateInputField();
                }
                _targetPointMax = _targetPointMin + new Vector2(0, _consolePanel.rect.height);
            }

            if ((GetArrowVertical() != 0) && _isShowed)
            {
                _chosenPrevCommand = Math.Clamp(_chosenPrevCommand - GetArrowVertical(), 0, _allPrevCommands.Count - 1);
                _inputField.text = _allPrevCommands[_chosenPrevCommand];
            }
        }

        private int GetArrowVertical()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                return 1;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                return -1;
            }
            return 0;
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
        public void ProcessCommand()
        {
            if (_isSliding)
                return;
            string command = _inputField.text;
            AddText("> <color=#742F27>" + command + "</color>");
            _allPrevCommands.RemoveLast();
            _allPrevCommands.Add(command);
            _allPrevCommands.Add("");
            _chosenPrevCommand = _allPrevCommands.Count;
            _inputField.text = "";
            string answer = _commandsController.ProcessCommand(command);
            AddText(answer);
        }
        private void AddText(string text)
        {
            _textField.text += "\n" + text;
        }
    }
}