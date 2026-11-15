using System.Collections.Generic;
using SS3D.Core;
using SS3D.Systems.Electricity;
using SS3D.Systems.Atmospherics.SystemsView;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.MapEditor.SystemsView;

namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Offers the map editor's Atmos view its source (Documents/architecture/2026-11_map-editor-systems-views.md Q10).
    /// The editor hub is spawned on another object with no fixed start order, so the server retries from each atmos tick
    /// until it finds one, and offers again if that hub is replaced.
    /// </summary>
    public sealed partial class AtmosSubSystem
    {
        private AtmosSystemsViewSource _systemsViewSource;
        private readonly SystemsViewRegistration _systemsViewRegistration = new();

        /// <summary>Server: the Atmos view's structure source; null on a pure client.</summary>
        public AtmosSystemsViewSource SystemsViewSource => _systemsViewSource;

        private void EnsureSystemsViewRegistered()
        {
            if (!IsServer)
                return;

            _systemsViewSource ??= new AtmosSystemsViewSource(
                () => SubSystems.TryGet(out TileSubSystem tiles) ? tiles.CurrentMap : null,
                () => _pipeRegistry,
                _portRegistry,
                () => SimulationPaused,
                gas => _gasRegistry != null && _gasRegistry.TryGetDefinition(new GasId((ushort)gas), out GasDefinition definition)
                    ? definition.Symbol : null,
                // A device's room and its power come from electricity's room → APC links (Phase 4), not read here.
                () => SubSystems.TryGet(out ElectricitySubSystem electricity) ? electricity.SystemsViewRooms : null,
                SystemsViewLineFills,
                () => IsReady);
            _systemsViewRegistration.Ensure(_systemsViewSource);
        }

        private void UnregisterSystemsView() => _systemsViewRegistration.Release(_systemsViewSource);

        private readonly List<AtmosLineFill> _systemsViewFills = new();

        /// <summary>The map's line fill markers in a stable order (west to east), for the Atmos view's analysis.</summary>
        private IReadOnlyList<AtmosLineFill> SystemsViewLineFills()
        {
            _systemsViewFills.Clear();
            if (!SubSystems.TryGet(out TileSubSystem tiles) || tiles.CurrentMap == null || tiles.GasMarkers == null)
                return _systemsViewFills;

            foreach (GasMarkerRecord marker in tiles.GasMarkers.Records)
            {
                if (marker.Kind == GasMarkerKind.LineFill)
                    _systemsViewFills.Add(new AtmosLineFill(tiles.QueryService.WorldToTile(marker.Position, tiles.CurrentMap.MapId),
                        marker.Line, marker.Position));
            }

            _systemsViewFills.Sort((a, b) => a.Coord.Grid != b.Coord.Grid ? SystemsViewProblems.CompareTiles(a.Coord.Grid, b.Coord.Grid)
                : a.Line.CompareTo(b.Line));
            return _systemsViewFills;
        }
    }
}
