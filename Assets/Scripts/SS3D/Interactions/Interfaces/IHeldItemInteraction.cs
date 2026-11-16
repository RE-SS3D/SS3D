namespace SS3D.Interactions.Interfaces
{
    /// <summary>
    /// A target-side interaction that acts with the held item (stacking held sheets onto a matching stack).
    /// While a tool or stack is held, the hover outline lights only these and the tool's own interactions
    /// green (D15 of 2026-11_construction-mvp1); other target interactions show yellow.
    /// </summary>
    public interface IHeldItemInteraction
    {
    }
}
