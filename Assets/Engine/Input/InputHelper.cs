using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Engine.Input {
    public class InputHelper : MonoBehaviour
    {

        public static InputActions inp { get; } = new InputActions();

        static InputHelper()
        {
            inp.Enable();
        }

        public static Vector3 GetPointerWorldPos()
        {
            return Camera.current.ScreenToWorldPoint(inp.Pointer.Position.ReadValue<Vector2>());
        }
    }
}
