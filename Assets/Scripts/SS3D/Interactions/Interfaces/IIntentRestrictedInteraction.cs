namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Interactions that only appear and execute under a specific player intent.
    /// </summary>
    /// <remarks>
    /// Interactions that do <b>not</b> implement this interface are Help-default (Drop, Open,
    /// machine UI, etc.). Harm and other exclusive modes must opt in here (e.g. melee Hit).
    /// Help-only medical verbs also implement this with <see cref="IntentType.Help"/>.
    /// Discovery filters on the client; the server re-validates using the synced intent on
    /// <c>InteractionController</c>.
    /// </remarks>
    public interface IIntentRestrictedInteraction
    {
        IntentType AllowedIntent { get; }
    }
}
