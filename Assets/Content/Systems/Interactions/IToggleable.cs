namespace SS3D.Content.Systems.Interactions
{
    public interface IToggleable
    {
        bool GetState();
        void Toggle();
    }
}