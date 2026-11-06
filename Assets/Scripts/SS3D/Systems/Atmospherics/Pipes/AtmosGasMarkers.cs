using SS3D.Systems.Tile;
using SS3D.Systems.Tile.GasMarkers;
using System;
using System.Collections.Generic;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Puts a map's gas markers into the simulation: line fills and room air once, after a map loads, and
    /// sources every tick. Sources follow the registry, so one placed or cleared mid-round applies at once.
    /// </summary>
    public sealed class AtmosGasMarkers : IDisposable
    {
        private readonly GasMarkerRegistry _markers;
        private readonly TileMap _map;
        private readonly ITileQueryService _query;
        private readonly int _gasCount;
        private readonly List<HeldCell> _held = new();
        private bool _heldDirty = true;

        public AtmosGasMarkers(GasMarkerRegistry markers, TileMap map, ITileQueryService query, int gasCount)
        {
            _markers = markers;
            _map = map;
            _query = query;
            _gasCount = gasCount;
            _markers.Changed += MarkHeldDirty;
        }

        public void Dispose() => _markers.Changed -= MarkHeldDirty;

        /// <summary>Fills the network under every line fill. Call right after the networks are built.</summary>
        public void FillLines(GasPipeNetworkRegistry pipes)
        {
            foreach (GasMarkerRecord marker in _markers.Records)
            {
                if (marker.Kind == GasMarkerKind.LineFill
                    && AtmosPipeConnectivity.TryGetSegment(_map, Tile(marker), marker.Line, out PlacedTileObject segment))
                    pipes.TryFillNetwork(GasPipeSegmentKey.From(segment), marker.GasFractions, marker.PressureKpa);
            }
        }

        /// <summary>Gives each room air marker's room its mix. Call once every chunk has its cells.</summary>
        public void FillRooms(AtmosSimulation simulation)
        {
            var cells = new List<TileCoord>();
            var moles = new float[_gasCount];
            foreach (GasMarkerRecord marker in _markers.Records)
            {
                if (marker.Kind != GasMarkerKind.RoomAir)
                    continue;

                AtmosRoomFlood.Collect(_query, Tile(marker), cells);
                FillCellMoles(marker, moles);
                foreach (TileCoord cell in cells)
                    simulation.HoldCell(cell, moles, AtmosConstants.StandardTemperature);
            }
        }

        public void HoldSources(AtmosSimulation simulation)
        {
            if (_heldDirty)
                RebuildHeld();

            foreach (HeldCell held in _held)
                simulation.HoldCell(held.Coord, held.Moles, AtmosConstants.StandardTemperature);
        }

        private void RebuildHeld()
        {
            _heldDirty = false;
            _held.Clear();
            foreach (GasMarkerRecord marker in _markers.Records)
            {
                if (marker.Kind != GasMarkerKind.Source)
                    continue;

                var moles = new float[_gasCount];
                FillCellMoles(marker, moles);
                _held.Add(new HeldCell(Tile(marker), moles));
            }
        }

        private void FillCellMoles(GasMarkerRecord marker, float[] moles) => AtmosGasMix.FillMoles(marker.GasFractions,
            AtmosPipeThermo.GetMolesForPressure(marker.PressureKpa, AtmosConstants.StandardTemperature, AtmosConstants.CellVolume), moles);

        private TileCoord Tile(GasMarkerRecord marker) => _query.WorldToTile(marker.Position, _map.MapId);

        private void MarkHeldDirty() => _heldDirty = true;

        private readonly struct HeldCell
        {
            public readonly TileCoord Coord;
            public readonly float[] Moles;

            public HeldCell(TileCoord coord, float[] moles)
            {
                Coord = coord;
                Moles = moles;
            }
        }
    }
}
