using SS3D.Interactions;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Examine tunables. Exact distances are balancing, not design (examine.md §11).
    /// </summary>
    public static class ExamineConfig
    {
        /// <summary>
        /// "Within a couple of tiles" (examine.md §2): inside it, <see cref="ExamineBand.Close"/> lines,
        /// the flavour description and the detailed image show. Measured from the selected hand's
        /// interaction origin to the target's closest collider point.
        /// </summary>
        public static readonly RangeLimit CloseRange = new(2.5f, 2f);
    }
}
