using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// One domain's server-side half of a systems view (Documents/architecture/systems/map-editor-systems-views.md, Code split):
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

        /// <summary>
        /// Server: the live numbers of the network <paramref name="viewId"/> names in the last
        /// <see cref="BuildStructure"/>, read now (Q2's live stream, polled about twice a second only while a client
        /// traces it). Rows are worded and formatted for the panel; devices are everything attached to that network.
        /// False when the id names no network any more (it went in an edit), which drops it from the trace.
        /// </summary>
        bool ReadLive(int viewId, out SystemsViewLiveNetwork live);

        /// <summary>Server: the simulation behind this view is paused, so traced numbers hold still on purpose.</summary>
        bool SimulationPaused { get; }

        /// <summary>
        /// Server: the problems the domain's pure analyzer finds in the current state (Q5, Phase 4), with tile positions.
        /// <see cref="BuildStructure"/> puts the same issues in as markers and Map Check reports them as warnings, so the
        /// two agree by construction. Cached until something the analyzer reads changes (topology, a device coming,
        /// going or moving, a room's power), never on a plain state flip, and such a change moves
        /// <see cref="StructureVersion"/> too.
        /// </summary>
        IReadOnlyList<SystemsViewIssue> Analyze();

        /// <summary>
        /// Server: the state <see cref="Analyze"/> reads has caught up with the map. False for a moment after a template load
        /// (the power graph rebuilds a tick later, atmos re-initialises), when <see cref="Analyze"/> reports nothing rather
        /// than bogus problems; Map Check sends its report again once every source is current.
        /// </summary>
        bool AnalysisCurrent { get; }
    }
}
