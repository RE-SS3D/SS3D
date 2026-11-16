using System.Collections.Generic;

namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// A source extension (on the held item) that the hover outline asks about the hovered target,
    /// so tool-side interactions such as welding a wall light the target like target-side ones (D11 of
    /// 2026-11_construction-mvp1). Add only interactions that are structurally relevant to
    /// <paramref name="interactionEvent"/>'s target; the probe runs <c>CanInteract</c> itself.
    /// </summary>
    /// <remarks>Runs from the outline LateUpdate throttle: do not allocate per call beyond the
    /// interaction instances, and do not keep the event.</remarks>
    public interface IOutlineSourceExtension
    {
        void CollectOutlineInteractions(InteractionEvent interactionEvent, List<IInteraction> into);
    }
}
