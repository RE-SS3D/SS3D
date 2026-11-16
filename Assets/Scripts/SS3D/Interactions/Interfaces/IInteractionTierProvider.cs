namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Optional interface for interactions that resolve differently from the radial menu.
    /// Interactions without this interface default to <see cref="InteractionTier.Instant"/>.
    /// </summary>
    public interface IInteractionTierProvider
    {
        InteractionTier GetTier(InteractionEvent interactionEvent);
    }
}
