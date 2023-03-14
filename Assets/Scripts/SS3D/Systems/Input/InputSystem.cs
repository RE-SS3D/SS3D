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
        base.OnAwake();
        
        Inputs = new Controls();
        _actionDisables = new();
        foreach (InputAction action in Inputs)
        {
            _actionDisables.Add(action, 1);
        }
        ToggleActionMap(Inputs.Other, true);
    }

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

    public void ToggleActionMap(InputActionMap actionMap, bool isEnable)
    {
        foreach (InputAction action in actionMap)
        {
            ToggleAction(action, isEnable);
        }
    }

    public void ToggleAllActions(bool isEnable)
    {
        foreach (InputAction action in Inputs)
        {
            ToggleAction(action, isEnable);
        }
    }

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

    public void ToggleBinding(InputBinding inputBinding, bool isEnable)
    {
        ToggleBinding(inputBinding.path, isEnable);
    }

    public void ToggleCollisions(InputAction[] actions, bool isEnable)
    {
        List<string> dontToggle = new();
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
            dontToggle.Add(action.actionMap.name + '/' + action.name);
        }
        
        InputAction[] collisions = _actionDisables.Where(pair => pair.Key.bindings.
                Any(binding => paths.Contains(binding.path) && !dontToggle.Contains(pair.Key.actionMap.name + '/' + binding.action)))
            .Select(x => x.Key).ToArray();
        for (int i = 0; i < collisions.Length; i++)
        {
            InputAction collision = collisions[i];
            ToggleAction(collision, isEnable);
        }
    }

    public void ToggleCollisions(InputAction action, bool isEnable)
    {
        InputAction[] actions = new[] { action };
        ToggleCollisions(actions, isEnable);
    }

    public void ToggleCollisions(InputActionMap map, bool isEnable)
    {
        ToggleCollisions(map.ToArray(), isEnable);
    }
}


