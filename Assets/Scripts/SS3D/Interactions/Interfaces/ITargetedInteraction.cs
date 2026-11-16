namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Interactions that resolve on a follow-up world target after arming from the radial menu.
    /// </summary>
    public interface ITargetedInteraction : IInteractionTierProvider
    {
        /// <summary>
        /// Checks whether the armed interaction can resolve on the hovered target.
        /// </summary>
        bool CanTarget(InteractionEvent originEvent, InteractionEvent targetEvent);
    }
}
