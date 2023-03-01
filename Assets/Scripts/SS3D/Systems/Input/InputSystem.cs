/// <summary>
/// Contains player's controls in Inputs
/// </summary>
public class InputSystem : SS3D.Core.Behaviours.System
{
    public Controls Inputs { get; private set; }
    protected override void OnAwake()
    {
        base.OnAwake();
        
        Inputs = new Controls();
        Inputs.Other.Enable();
    }
}
