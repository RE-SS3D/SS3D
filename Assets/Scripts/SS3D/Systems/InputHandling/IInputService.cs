namespace SS3D.Systems.InputHandling
{
    /// <summary>
    /// IInputService is an interface to allow dependency injection for player input actions.
    /// This will enable easier testing of input-related functionality, as well as allowing 
    /// it to be overridden if required.
    /// </summary>
    public interface IInputService
    {
        public bool GetButtonUp(string buttonName);
        public bool GetButtonDown(string buttonName);
        public bool GetButton(string buttonName);
        public float GetAxisRaw(string axisName);

    }
}