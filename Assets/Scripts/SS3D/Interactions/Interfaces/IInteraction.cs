using UnityEngine;

namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Represents an interaction that can be performed.
    /// </summary>
    /// <remarks>
    /// <para><see cref="GetName"/> is display-only and may change with target or toggle state.</para>
    /// <para><see cref="GetGenericName"/> is the stable wire identifier used by RPC matching. Never derive it from animator state, open/close labels, or other runtime-only values.</para>
    /// <para><see cref="Priority"/> bands: 100+ combat, 50–99 machine UI, 10–49 inventory and world devices, 0–9 passive/fallback. Higher values win primary-click selection.</para>
    /// <para><see cref="Start"/> runs server-side. Mutations that must replicate should go through networked components (for example <c>NetworkedOpenable.SetOpenState</c>), not local-only animator writes.</para>
    /// <para>Tag intent-specific interactions with <see cref="IIntentRestrictedInteraction"/> so discovery and server validation stay aligned.</para>
    /// </remarks>
    public interface IInteraction
    {
        /// <summary>
        /// Gets the name when interacted with a source
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <returns>The display name of the interaction</returns>
        string GetName(InteractionEvent interactionEvent);

        /// <summary>
        /// Get the stable wire identifier for this interaction. Must not depend on runtime toggle or animation state.
        /// Display text belongs in <see cref="GetName"/>.
        /// </summary>
        string GetGenericName();

        /// <summary>
        /// Gets the interaction icon
        /// </summary>
        Sprite GetIcon(InteractionEvent interactionEvent);

        /// <summary>
        /// Relative ordering for primary-click selection and radial menus. Higher runs first.
        /// </summary>
        int Priority => 0;

        /// <summary>
        /// Checks if this interaction can be executed
        /// </summary>
        /// <param name="interactionEvent">The interaction source</param>
        /// <returns>If the interaction can be executed</returns>
        bool CanInteract(InteractionEvent interactionEvent);

        /// <summary>
        /// Starts the interaction (server-side)
        /// </summary>
        /// <param name="interactionEvent">The source used in the interaction</param>
        /// <param name="reference"></param>
        /// <returns>If the interaction should continue running</returns>
        bool Start(InteractionEvent interactionEvent, InteractionReference reference);
    }
}