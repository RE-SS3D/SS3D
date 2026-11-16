namespace SS3D.Interactions.Interfaces
{
    public interface IIntentProvider
    {
        IntentType CurrentIntent { get; }

        void RequestToggleIntent();
    }
}
