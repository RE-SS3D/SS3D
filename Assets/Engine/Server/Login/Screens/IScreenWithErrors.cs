namespace SS3D.Engine.Server.Login.Screens
{
    /// <summary>
    /// Interface to guarantee existence of error display and clearing from the various login screens.
    /// </summary>
    public interface IScreenWithErrors
    {
        void DisplayErrorMessage(string error);
        void ClearErrors();
    }
}