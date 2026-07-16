namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Interactions that only appear and execute under a specific player intent.
    /// </summary>
    /// <remarks>
    /// Implement on combat or medical interactions that should be hidden unless the player toggles Help/Harm.
    /// Discovery filters on the client; the server re-validates using the synced intent on <c>InteractionController</c>.
    /// </remarks>
    public interface IIntentRestrictedInteraction
    {
        IntentType AllowedIntent { get; }
    }
}
