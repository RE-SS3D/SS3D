using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Inputs
{
    /// <summary>
    /// Declares the exact set of action maps and individual actions a context enables. When a context
    /// is active its definition is the whole world of enabled input for that moment; suppressions then
    /// subtract from it. Membership is by reference, so map/action instances must come from the same
    /// asset the arbiter drives.
    /// </summary>
    public sealed class InputContextDefinition
    {
        private readonly HashSet<InputActionMap> _maps;
        private readonly HashSet<InputAction> _actions;

        public InputContextDefinition(IEnumerable<InputActionMap> maps, IEnumerable<InputAction> actions)
        {
            _maps = new HashSet<InputActionMap>(maps);
            _actions = new HashSet<InputAction>(actions);
        }

        public bool Includes(InputAction action)
        {
            return _maps.Contains(action.actionMap) || _actions.Contains(action);
        }
    }
}
