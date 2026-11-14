namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// One domain's server-side half of a systems view (Documents/architecture/2026-11_map-editor-systems-views.md Q10):
    /// the atmos source under <c>Atmospherics/…/SystemsView/</c>, the power source under <c>Electricity/…/SystemsView/</c>.
    /// Registered on the map editor's server half, which polls it only while some editor client shows this view.
    /// </summary>
    public interface ISystemsViewSource
    {
        SystemsViewKind View { get; }

        /// <summary>
        /// Changes whenever <see cref="BuildStructure"/> may return something different: a topology version bump, a
        /// circuit rebuild, a device state or problem change. The server rebuilds only when it moves, then sends each
        /// client only what differs from what that client was sent, so a spurious bump costs a build, not traffic.
        /// </summary>
        int StructureVersion { get; }

        /// <summary>Server: a fresh structure of the whole station, read from current state (never port caches).</summary>
        SystemsViewStructure BuildStructure();
    }
}
