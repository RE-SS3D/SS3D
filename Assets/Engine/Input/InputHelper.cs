using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Input {
    public class InputHelper : MonoBehaviour
    {
        public static Inputs inputs { get; } = new Inputs();

        public static Vector3 GetPointerWorldPos()
        {
            return Camera.current.ScreenToWorldPoint(inputs.pointer.position.ReadValue<Vector2>());
        }
    }
}
