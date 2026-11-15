using System;
using System.Collections.Generic;
using SS3D.Systems.Tile.TileMapCreator;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// The client half of one domain's view (Documents/architecture/systems/map-editor-systems-views.md, Drawing, Code split): how the
    /// overlay lays out that domain's slots, draws its badge codes and words its hover readout. The overlay and the
    /// hover stay domain-free; each domain keeps the meaning of its Slot, Kind and State codes next to its source.
    /// </summary>
    public interface ISystemsViewStyle
    {
        /// <summary>Where a run on <paramref name="slot"/> sits within its tile (x, z from the tile centre), so lines sharing a tile sit side by side.</summary>
        Vector2 SlotOffset(byte slot);

        /// <summary>
        /// Adds a badge's marking around <paramref name="centre"/> (mesh space, at overlay height), each network side in
        /// <paramref name="colorOf"/>'s colour for its view id (<see cref="SystemsViewStructure.ColorOf"/>).
        /// </summary>
        void DrawBadge(SystemsViewMeshBuilder mesh, Vector3 centre, SystemsViewBadge badge, Func<int, Color> colorOf);

        /// <summary>The hover readout for one tile: its badges and the networks of its runs; empty when nothing is there.</summary>
        string Describe(SystemsViewStructure structure, Vector2Int tile);

        /// <summary>
        /// What Select traces from the object on <paramref name="layer"/> at <paramref name="tile"/> (creative-mode §4
        /// Tracing, Q11): its network, or each network of a device between several, with the side's name ("Inlet"), in
        /// panel order. Adds nothing for an object that isn't part of the view.
        /// </summary>
        void TraceTargets(SystemsViewStructure structure, Vector2Int tile, TileLayer layer, List<SystemsViewTraceSide> into);
    }

    /// <summary>What one domain gives the shared systems view code on the client: its style, legend lines and own layer.</summary>
    public sealed class SystemsViewDomain
    {
        public SystemsViewKind View;
        public ISystemsViewStyle Style;
        public IReadOnlyList<SystemsViewLegendEntry> Legend;
        /// <summary>
        /// The layer the view draws over (Gas Pipes for Atmos). The view dims it with the x-ray layers, so its own paint
        /// doesn't compete with network colours, but it never gets harder to pick: <c>MapEditorViewModel.IsLayerPickable</c>.
        /// </summary>
        public TileLayerCategory? OwnLayer;
        /// <summary>The stripes of a hatched room (<see cref="SystemsViewRoom.Hatched"/>), for a domain that sends rooms.</summary>
        public Color RoomHatch;
        /// <summary>
        /// The problem kinds this domain's analyzer marks (Phase 4), by the code in a marker's slot: the hover names a
        /// marker with them, in the words the legend and Map Check use.
        /// </summary>
        public IReadOnlyList<SystemsViewProblemKind> Problems;
    }

    /// <summary>
    /// Domains register here the way their server sources register on the publisher, so a new view adds files rather
    /// than editing shared switches. Each domain registers itself on load (<c>AtmosSystemsViewDomain</c>,
    /// <c>PowerSystemsViewDomain</c>).
    /// </summary>
    public static class SystemsViewDomains
    {
        private static readonly Dictionary<SystemsViewKind, SystemsViewDomain> Domains = new();

        public static void Register(SystemsViewDomain domain)
        {
            if (domain != null && domain.View != SystemsViewKind.Off)
                Domains[domain.View] = domain;
        }

        /// <summary>The domain of a view; null for Off and for a view no domain has registered yet.</summary>
        public static SystemsViewDomain For(SystemsViewKind view) =>
            Domains.TryGetValue(view, out SystemsViewDomain domain) ? domain : null;
    }

    public static class SystemsViewStyles
    {
        /// <summary>The style of a view's domain; null when none is registered.</summary>
        public static ISystemsViewStyle For(SystemsViewKind view) => SystemsViewDomains.For(view)?.Style;
    }
}
