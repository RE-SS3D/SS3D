using SS3D.Core;
using UnityEngine;

public class InputSystem : SS3D.Core.Behaviours.System
{
    public Controls Inputs { get; private set; }
    protected override void OnAwake()
    {
        base.OnAwake();
        
        Debug.Log(1);
        Inputs = new Controls();
        Inputs.Other.Enable();
    }
}
