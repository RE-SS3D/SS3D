using UnityEngine;

/// <summary>
/// Contains player's controls in Inputs
/// </summary>
public class InputSubsystem : SS3D.Core.Behaviours.Subsystem
{
    public Controls Inputs { get; private set; }
    
    protected override void OnAwake()
    {
        base.OnAwake();
        
        Inputs = new Controls();
        Inputs.Other.Enable();
    }
}
