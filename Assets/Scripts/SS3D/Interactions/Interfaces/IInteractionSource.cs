using System.Collections.Generic;

namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Represents a source for an interaction.
	/// A screw driver can be a source of interaction for the opening maintenance panels, building and stabbing interactions.
    /// </summary>
    public interface IInteractionSource : INetworkObjectProvider, IGameObjectProvider
    {
        IInteractionSource Source { get; set; }

        /// <summary>
        /// Allows the source to manipulate existing interactions and add new ones.
        /// Discover may pre-filter candidates; <see cref="InteractionPipeline.FilterAndSort"/> is the
        /// sole full viability gate. Source-only verbs use <see cref="InteractionEntry.SourceOnly"/>.
        /// </summary>
        /// <param name="targets">The interaction targets of this interaction</param>
        /// <param name="entries">The already present interactions</param>
        /// <param name="context">Discover event (source + optional resolved point/normal)</param>
        void CreateSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> entries, InteractionEvent context);
        /// <summary>
        /// Checks if this source can interact with a certain target
        /// </summary>
        /// <param name="target">The interaction target to check against</param>
        /// <returns>If the interaction target is supported</returns>
        bool CanInteractWithTarget(IInteractionTarget target);
        /// <summary>
        /// Checks if this source can execute a given interaction
        /// </summary>
        /// <param name="interaction">The interaction to check</param>
        /// <returns>If this interaction source can execute the interaction</returns>
        bool CanExecuteInteraction(IInteraction interaction);

        /// <summary>
        /// Checks if this source can continue an in-progress interaction.
        /// </summary>
        bool CanContinueInteraction();

        /// <summary>
        /// Executes the interaction (server-side)
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <param name="interaction">The desired interaction</param>
        /// <returns>A reference to the interaction</returns>
        InteractionReference Interact(InteractionEvent interactionEvent, IInteraction interaction);

        /// <summary>
        /// Executes the interaction (client-side)
        /// </summary>
        /// <param name="interactionEvent">The interaction event</param>
        /// <param name="interaction">The desired interaction</param>
        /// <param name="reference">The reference to the interaction (generated on server)</param>
        void ClientInteract(InteractionEvent interactionEvent, IInteraction interaction, InteractionReference reference);
        /// <summary>
        /// Cancels an interaction
        /// </summary>
        /// <param name="reference">The reference to the interaction</param>
        void CancelInteraction(InteractionReference reference);

        /// <summary>
        /// Cancels an interaction and tells the owning client why (progress ring reason).
        /// </summary>
        void CancelInteraction(InteractionReference reference, DelayedCancelReason reason);

        InteractionInstance GetInstanceFromReference(InteractionReference reference);

        /// <summary>
        /// Returns whether an interaction with the given reference is still active on this source.
        /// </summary>
        bool HasInteraction(InteractionReference reference);
    }
}