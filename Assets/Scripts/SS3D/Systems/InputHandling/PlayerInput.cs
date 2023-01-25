using UnityEngine;

namespace SS3D.Systems.InputHandling
{
    /// <summary>
    /// PlayerInput simply passes through the input query to the Unity input system.
    /// This is the default for all players.
    /// </summary>
    public class PlayerInput : IInputService
    {
        public float GetAxisRaw(string axisName)
        {
            return Input.GetAxisRaw(axisName);
        }

        public bool GetButton(string buttonName)
        {
            return Input.GetButton(buttonName);
        }

        public bool GetButtonDown(string buttonName)
        {
            return Input.GetButtonDown(buttonName);
        }

        public bool GetButtonUp(string buttonName)
        {
            return Input.GetButtonUp(buttonName);
        }
    }
}
