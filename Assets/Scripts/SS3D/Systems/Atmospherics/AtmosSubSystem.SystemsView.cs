using System.Collections.Generic;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Rendering.URP;
using SS3D.Systems.Atmospherics.Visualization;
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
    /// until it finds one, and offers again if that hub is replaced. Also the Atmos half of Phase 5: the admin pause and
    /// step (<see cref="ISystemsViewSimulation"/>) and the Room air resync of every chunk to one client.
    /// </summary>
    public sealed partial class AtmosSubSystem : ISystemsViewSimulation
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

        private readonly AtmosChunkResync _chunkResync = new();

        /// <summary>
        /// Server: Room air went on for <paramref name="conn"/>'s editor (Q2). A pure client is sent every chunk once,
        /// through the patch path the broadcast uses, by <see cref="SendChunkResync"/>; the broadcast keeps it current after
        /// that. The host's own client reads the server's atlas and needs nothing.
        /// </summary>
        public void BeginChunkResync(NetworkConnection conn)
        {
            if (!IsServer || conn == null || conn.IsLocalClient || _simulation == null || _chunkResync.IsStarted(conn.ClientId))
                return;

            _chunkResync.Begin(conn.ClientId, AllChunkKeys());
        }

        private List<UnityEngine.Vector2Int> AllChunkKeys()
        {
            List<UnityEngine.Vector2Int> chunks = new(_simulation.Chunks.Count);
            foreach (TileChunkRef chunk in _simulation.Chunks)
                chunks.Add(chunk.ChunkKey);
            return chunks;
        }

        /// <summary>Server: Room air went off, or its editor closed or left.</summary>
        public void EndChunkResync(int clientId) => _chunkResync.End(clientId);

        /// <summary>
        /// Server: sends the next few resync chunks, at most the broadcast's per-tick cap per call. The map editor hub
        /// calls it on its poll, which runs while the simulation is paused too.
        /// </summary>
        public void SendChunkResync()
        {
            if (!IsServer || _simulation == null || _patchBuilder == null)
                return;

            // After a station template restore, the clients that had Room air on get the new station.
            if (_chunkResync.HasRestarts && IsReady)
            {
                foreach (int clientId in _chunkResync.TakeRestarts())
                    _chunkResync.Begin(clientId, AllChunkKeys());
            }

            if (!_chunkResync.HasWork)
                return;

            _chunkResync.Drain(MaxPatchesPerTick, (clientId, chunkKey) =>
            {
                if (ServerManager.Clients.TryGetValue(clientId, out NetworkConnection conn) && conn.IsActive
                    && _simulation.TryGetChunkIndex(chunkKey, out int chunkIndex))
                    TargetApplyChunkPatch(conn, _patchBuilder.Build(_simulation, chunkIndex, chunkKey, advanceFire: false));
            });
        }

        /// <summary>
        /// This client's Room air is on or off (Phase 5): the one way in for it. On a host the snapshot then covers the whole
        /// station rather than the player's area of interest (<c>AtmosRenderContext.FullAtlasRequested</c>), and is published
        /// again at once, since a paused simulation publishes nothing. Off (or the editor going away) narrows it again, so a
        /// leaked request never costs the next session full-station uploads. A pure client's atlas comes from patches.
        /// </summary>
        public static void SetRoomAirRequested(bool requested)
        {
            AtmosRenderContext.SetFullAtlasRequested(requested);
            if (requested && SubSystems.TryGet(out AtmosSubSystem atmos) && atmos.IsServer && atmos._simulation != null)
                atmos._visualizationBridge?.PublishSnapshot();
        }

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
