using Serilog;
using SS3D.Core.Behaviours;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Inputs
{
    /// <summary>
    /// Contains player's controls in Inputs
    /// </summary>
    public sealed class InputSubSystem : SubSystem
    {
        public Controls Inputs { get; private set; }

        public float MouseSensitivity { get; private set; }

        private Dictionary<InputAction, int> _actionDisables;

        protected override void OnAwake()
        {
            DontDestroyOnLoad(transform.gameObject);
            
            base.OnAwake();

            Setup();
        }

        private void Setup()
        {
            MouseSensitivity = 0.001f;
            Inputs = new Controls();

            _actionDisables = new();     

            foreach (InputAction action in Inputs)
            {
                _actionDisables.Add(action, 1);
            }

            ToggleActionMap(Inputs.Other, true);
        }

        /// <summary>
        /// Substract one set of actions from another
        /// </summary>
        /// <param name="actions"></param>
        /// <param name="exclude"></param>
        /// <returns></returns>
        private InputAction[] Exclude(InputAction[] actions, InputAction[] exclude)
        {
            if (exclude == null)
            {
                return actions;
            }
            else
            {
                return actions.Where(x => !exclude.Contains(x)).ToArray();
            }
        }

        /// <summary>
        /// Set actions to enabled or disabled. If ToggleAction(actions, true) was called less then ToggleAction(actions, false), the actions stay disabled
        /// </summary>
        /// <param name="actions"></param>
        /// <param name="isEnable">True - enable, false - disable</param>
        public void ToggleActions(InputAction[] actions, bool isEnable)
        {
            foreach (InputAction action in actions)
            {
                if (isEnable)
                {
                    _actionDisables[action] -= 1;
                }
                else
                {
                    _actionDisables[action] += 1;
                }

                if (_actionDisables[action] == 0)
                {
                    if (!action.enabled)
                    {
                        action.Enable();
                    }
                }
                else
                {
                    if (action.enabled)
                    {
                        action.Disable();
                    }
                }
            }
        }

        /// <summary>
        /// Set action to enabled or disabled. If ToggleAction(action, true) was called less then ToggleAction(action, false), the action stays disabled.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="isEnable">True - enable, false - disable</param>
        public void ToggleAction(InputAction action, bool isEnable)
        {
            ToggleActions(new []{action}, isEnable);
        }

        /// <summary>
        /// Set all actions to enabled or disabled. If ToggleAllActions(actions, true) was called less then ToggleAllActions(actions, false), the actions stay disabled.
        /// </summary>
        /// <param name="isEnable">True - enable, false - disable</param>
        /// <param name="exclude">Actions, that shouldn't be toggled</param>
        public void ToggleAllActions(bool isEnable, InputAction[] exclude = null)
        {
            ToggleActions(Exclude(Inputs.ToArray(), exclude), isEnable);
        }

        /// <summary>
        /// Set all actions from actionMap to enabled or disabled. If ToggleAction(actions, true) was called less then ToggleAction(actions, false), the actions stay disabled.
        /// </summary>
        /// <param name="actionMap"></param>
        /// <param name="isEnable">True - enable, false - disable</param>
        /// <param name="exclude">Actions, that shouldn't be toggled</param>
        public void ToggleActionMap(InputActionMap actionMap, bool isEnable, InputAction[] exclude = null)
        {
            ToggleActions(Exclude(actionMap.ToArray(), exclude), isEnable);
        }

        /// <summary>
        /// Toggle all actions, that contain bindings with a given key path
        /// </summary>
        /// <param name="keyPath"></param>
        /// <param name="isEnable">True - enable, false - disable</param>
        /// <param name="exclude">Actions, that shouldn't be toggled</param>
        public void ToggleBinding(string keyPath, bool isEnable, InputAction[] exclude = null)
        {
            InputAction[] actions = Inputs.Where(x => x.bindings.Any(y => y.path == keyPath)).ToArray();
            ToggleActions(Exclude(actions, exclude), isEnable);
        }

        /// <summary>
        /// Toggle all actions, that contain bindings with the same key path as inputBinding.
        /// </summary>
        /// <param name="inputBinding"></param>
        /// <param name="isEnable">True - enable, false - disable</param>
        /// <param name="exclude">Actions, that shouldn't be toggled</param>
        public void ToggleBinding(InputBinding inputBinding, bool isEnable, InputAction[] exclude = null)
        {
            ToggleBinding(inputBinding.path, isEnable, exclude);
        }

        /// <summary>
        /// Toggle all actions, that use same key paths as given actions. Doesn't toggle actions in actions parameter.
        /// </summary>
        /// <param name="actions"></param>
        /// <param name="isEnable">True - enable, false - disable</param>
        /// <param name="exclude">Actions, that should be toggled. Already includes actions parameter</param>
        public void ToggleCollisions(InputAction[] actions, bool isEnable, InputAction[] exclude = null)
        {
            List<string> paths = new();
            foreach (InputAction action in actions)
            {
                foreach (InputBinding binding in action.bindings)
                {
                    if (!binding.isComposite)
                    {
                        paths.Add(binding.path);
                    }
                }
            }

            List<InputAction> collisions = new();

            foreach (InputAction action in Inputs)
            {
                if (!actions.Contains(action))
                {
                    foreach (InputBinding binding in action.bindings)
                    {
                        if ((paths.Contains(binding.path)) || ((binding.path == "<Keyboard>/leftShift" 
                                                                || binding.path == "<Keyboard>/rightShift") 
                                                               && paths.Contains("<Keyboard>/shift")))
                        {
                            collisions.Add(action);
                            break;
                        }
                    }
                }
            }

            ToggleActions(Exclude(collisions.ToArray(), exclude), isEnable);
        }

        /// <summary>
        /// Toggle all actions, that use same key paths as in given action. Doesn't toggle given action.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="isEnable">True - enable, false - disable</param>
        /// <param name="exclude">Actions, that should be toggled. Already includes action parameter</param>
        public void ToggleCollisions(InputAction action, bool isEnable, InputAction[] exclude = null)
        {
            InputAction[] actions = new[] { action };
            ToggleCollisions(actions, isEnable, exclude);
        }

        /// <summary>
        /// Toggle all actions, that use same key paths as in given action map. Doesn't toggle actions in given action map.
        /// </summary>
        /// <param name="actionMap"></param>
        /// <param name="isEnable">True - enable, false - disable</param>
        /// <param name="exclude">Actions, that shouldn't be toggled. Already includes actions from actionMap</param>
        public void ToggleCollisions(InputActionMap actionMap, bool isEnable, InputAction[] exclude = null)
        {
            ToggleCollisions(actionMap.ToArray(), isEnable, exclude);
        }
    }
}


