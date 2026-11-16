namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// A producer whose output varies (switched off, the sun's angle) but has a fixed rating: what it gives at most. The
    /// Power view's "needs more than it gives" check (Documents/architecture/systems/map-editor-systems-views.md, Problems)
    /// compares a circuit's rooms with this, not with the output of the moment, so its marker doesn't come and go.
    /// </summary>
    public interface IRatedPowerProducer : IPowerProducer
    {
        /// <summary>Most kW it produces when running flat out (a solar panel in full sun, aimed at it).</summary>
        float RatedPowerKw { get; }
    }
}
