using System.Collections.Generic;

namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// Allows the creation of additional interactions for an existing source.
    /// </summary>
    /// <remarks>
    /// Discover contract (TECH_DEBT 1.3 / interaction Discover contract):
    /// <list type="bullet">
    /// <item>
    /// <b>Target-bound:</b> Add with <c>Target != null</c> only when the interaction is structurally
    /// relevant to that target (type/shape and/or <c>CanInteract</c>). Never unconditional Add for every hover.
    /// </item>
    /// <item>
    /// <b>Source-only:</b> Use <see cref="InteractionEntry.SourceOnly"/> once per Discover (e.g. Drop).
    /// Not “doable to this hover.”
    /// </item>
    /// <item>
    /// <b>Viability:</b> <see cref="InteractionPipeline.FilterAndSort"/> is the sole full gate for menus/RPC.
    /// Discover may pre-filter; it must not imply already menu-ready without FilterAndSort.
    /// </item>
    /// <item>
    /// Prefer building per-target checks with <see cref="InteractionEvent.WithTarget"/> so
    /// <see cref="InteractionEvent.HasPoint"/> from <paramref name="context"/> is preserved.
    /// </item>
    /// </list>
    /// </remarks>
    public interface IInteractionSourceExtension
    {
        /// <summary>
        /// Allows the extension to manipulate existing interactions and add new ones.
        /// </summary>
        /// <param name="targets">The interaction targets of this interaction</param>
        /// <param name="interactions">The already present interactions</param>
        /// <param name="context">Discover event (source + optional resolved point/normal)</param>
        void GetSourceInteractions(IInteractionTarget[] targets, List<InteractionEntry> interactions, InteractionEvent context);
    }
}
