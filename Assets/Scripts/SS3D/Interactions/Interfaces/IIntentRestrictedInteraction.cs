namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Interactions that only appear and execute under a specific player intent.
    /// </summary>
    public interface IIntentRestrictedInteraction
    {
        IntentType AllowedIntent { get; }
    }
}
