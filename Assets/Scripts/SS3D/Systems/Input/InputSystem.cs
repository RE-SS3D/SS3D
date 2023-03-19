using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

/// <summary>
/// Contains player's controls in Inputs
/// </summary>
public class InputSystem : SS3D.Core.Behaviours.System
{
    public Controls Inputs { get; private set; }
    private Dictionary<InputAction, int> _actionDisables;

    protected override void OnAwake()
    {
        DontDestroyOnLoad(this);
        base.OnAwake();
        
        Inputs = new Controls();
        _actionDisables = new();
        foreach (InputAction action in Inputs)
        {
            _actionDisables.Add(action, 1);
        }
        ToggleActionMap(Inputs.Other, true);
    }
    /// <summary>
    /// Set action to enabled or disabled. If ToggleAction(action, false) was called more or equal times then ToggleAction(action, true), the action stays disabled.
    /// </summary>
    /// <param name="isEnable">True - enable, false - disable</param>

    public void ToggleAction(InputAction action, bool isEnable)
    {
        if (isEnable)
        {
            if (_actionDisables[action] > 0)
            {
                _actionDisables[action] -= 1;
            }
        }
        else
        {
            _actionDisables[action] += 1;
        }

        if (_actionDisables[action] == 0)
        {
            action.Enable();
        }
        else
        {
            action.Disable();
        }
    }
    /// <summary>
    /// Perform ToggleAction for every action in ActionMap
    /// </summary>
    /// <param name="isEnable">True - enable, false - disable</param>
    public void ToggleActionMap(InputActionMap actionMap, bool isEnable)
    {
        foreach (InputAction action in actionMap)
        {
            ToggleAction(action, isEnable);
        }
    }

    /// <param name="isEnable">True - enable, false - disable</param>

    public void ToggleAllActions(bool isEnable)
    {
        foreach (InputAction action in Inputs)
        {
            ToggleAction(action, isEnable);
        }
    }
    /// <summary>
    /// Toggle all actions, that contain bindings with given key path
    /// </summary>
    /// <param name="isEnable">True - enable, false - disable</param>

    public void ToggleBinding(string keyPath, bool isEnable)
    {
        InputAction[] inputActions = _actionDisables.Where(pair => pair.Key.bindings.
            Any(binding => binding.path == keyPath)).Select(x => x.Key).ToArray();
        for (int i = 0; i < inputActions.Length; i++)
        {
            InputAction action = inputActions[i];
            ToggleAction(action, isEnable);
        }
    }
    /// <summary>
    /// Toggle all actions, that contain bindings with the same key path.
    /// </summary>
    /// <param name="isEnable">True - enable, false - disable</param>

    public void ToggleBinding(InputBinding inputBinding, bool isEnable)
    {
        ToggleBinding(inputBinding.path, isEnable);
    }
    /// <summary>
    /// Toggle all actions, that use same key paths as in given actions. Doesn't toggle actions in actions parameter.
    /// </summary>
    /// <param name="isEnable">True - enable, false - disable</param>

    public void ToggleCollisions(InputAction[] actions, bool isEnable)
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

        KeyValuePair<InputAction, int>[] _disablesArray = _actionDisables.ToArray();
        for (int i = 0; i < _disablesArray.Length; i++)
        {
            KeyValuePair<InputAction, int> pair = _disablesArray[i];
            InputAction action = pair.Key;
            if (!actions.Contains(action))
            {
                foreach (InputBinding binding in action.bindings)
                {
                    if (paths.Contains(binding.path))
                    {
                        ToggleAction(action, isEnable);
                        break;
                    }
                    if ((binding.path == "<Keyboard>/leftShift" || binding.path == "<Keyboard>/rightShift") &&
                             paths.Contains("<Keyboard>/shift"))
                    {
                        ToggleAction(action, isEnable);
                        break;
                    }
                }
            }
        }
    }
    /// <summary>
    /// Toggle all actions, that use same key paths as in given action. Doesn't toggle given action.
    /// </summary>
    /// <param name="isEnable">True - enable, false - disable</param>
    public void ToggleCollisions(InputAction action, bool isEnable)
    {
        InputAction[] actions = new[] { action };
        ToggleCollisions(actions, isEnable);
    }
    /// <summary>
    /// Toggle all actions, that use same key paths as in given action map. Doesn't toggle actions in given action map.
    /// </summary>
    /// <param name="isEnable">True - enable, false - disable</param>
    public void ToggleCollisions(InputActionMap map, bool isEnable)
    {
        ToggleCollisions(map.ToArray(), isEnable);
    }
}


