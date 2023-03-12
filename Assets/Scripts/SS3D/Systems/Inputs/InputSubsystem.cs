using SS3D.Core.Behaviours;

namespace SS3D.Systems.Inputs
{
    /// <summary>
    /// Contains player's controls in Inputs
    /// </summary>
    public sealed class InputSubsystem : Subsystem
    {
        public Controls Inputs { get; private set; }

        protected override void OnEnabled()
        {
            base.OnEnabled();
        
            Inputs = new();
            Inputs.Other.Enable();
        }
    }
}