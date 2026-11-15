namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// A consumer whose draw varies (idle or in use) but has a fixed most. The Power view's "needs more than it gives" check
    /// (Documents/architecture/2026-11_map-editor-systems-views.md Phase 4) counts this, not the draw of the moment, so its
    /// marker doesn't come and go as machines are used.
    /// </summary>
    public interface IRatedPowerConsumer : IPowerConsumer
    {
        /// <summary>Most kW it draws: in use, or idle if that is more.</summary>
        float RatedPowerKw { get; }
    }
}
