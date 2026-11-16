using System;
using System.Collections.Generic;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.MapEditor.UI;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Client half of the systems views (Documents/architecture/systems/map-editor-systems-views.md, Streams, Code split): which view
    /// it asked the server for, the structure the server has sent for it, the overlay drawing that structure, and the
    /// trace (Phase 2): which networks a Select followed, the live numbers the server sends for them, and the label.
    /// The RPCs live on the hub, in <c>MapEditorSystemsView.cs</c>; it sends what <see cref="TakeTraceRequest"/> hands it.
    /// A view that sends rooms (Power, Phase 3) also tints them: the synced room membership drawn by a
    /// <see cref="MapEditorAreaOverlay"/> in each room's circuit colour, hatched for a room with no APC.
    /// </summary>
    public sealed class MapEditorSystemsViewClient : IDisposable
    {
        /// <summary>Under the systems overlay's runs (4000), above the world.</summary>
        public const int RoomTintQueue = 3999;

        private readonly MapEditorSystemsOverlay _overlay = new();
        private readonly MapEditorAreaOverlay _roomTint;

        public MapEditorSystemsViewClient() => _roomTint = new MapEditorAreaOverlay(RoomTint, RoomTintQueue, "MapEditorSystemsRoomTint");

        /// <summary>The view last asked of the server.</summary>
        public SystemsViewKind Requested { get; private set; }

        /// <summary>What the server has sent for <see cref="Requested"/>; null until its reset arrives.</summary>
        public SystemsViewStructure Structure { get; private set; }

        /// <summary>
        /// Every frame while the editor is open, with the view now on. Returns true when that differs from
        /// <see cref="Requested"/>, so the server must be asked for it; the old view's structure is dropped.
        /// <paramref name="roomsOverlayShown"/>: the Scripts › Rooms overlay is up, whose room colours painting needs, so
        /// the room tint stays off under it.
        /// </summary>
        public bool Update(SystemsViewKind active, bool roomsOverlayShown = false)
        {
            bool changed = active != Requested;
            if (changed)
            {
                Requested = active;
                Structure = null;
                _overlay.SetStructure(null);
                _tintApplied.Clear();
                _roomTint.Invalidate();
                _hoverRevision++;
                // The server starts the new view (or none) with no trace, so nothing needs asking.
                EndTrace(tellServer: false);
            }

            _overlay.SetVisible(active != SystemsViewKind.Off);
            _roomTint.SetVisible(ShowsRoomTint && !roomsOverlayShown);
            return changed;
        }

        /// <summary>The structure has rooms to tint (the Power view; Atmos sends none).</summary>
        public bool ShowsRoomTint => Requested != SystemsViewKind.Off && Structure != null && Structure.Rooms.Count > 0;

        /// <summary>The room tint, for tests and the visual pilot.</summary>
        public MapEditorAreaOverlay RoomTintOverlay => _roomTint;

        /// <summary>
        /// A room's tint: its APC's circuit colour (grey for an APC on no cable), or hatched when it has no APC. While a
        /// trace is on, rooms neither traced nor on a traced circuit dim like the runs.
        /// </summary>
        public MapEditorAreaOverlay.Tint RoomTint(ushort areaId)
        {
            if (Structure == null || !Structure.Rooms.TryGetValue(areaId, out SystemsViewRoom room))
                return MapEditorAreaOverlay.Tint.None;

            bool hatched = room.IsHatched;
            Color color = hatched ? HatchColor : SystemsViewPalette.ColorFor(room.CircuitViewId);
            if (_focusIds.Count > 0 && !_focusIds.Contains(room.ViewId) && (room.CircuitViewId <= 0 || !_focusIds.Contains(room.CircuitViewId)))
                color.a *= MapEditorSystemsOverlay.UntracedAlpha;
            return new MapEditorAreaOverlay.Tint(color, hatched);
        }

        /// <summary>The stripes of a hatched room, as the view's domain gives them.</summary>
        private Color HatchColor => SystemsViewDomains.For(Requested)?.RoomHatch ?? SystemsViewPalette.None;

        /// <summary>
        /// The colour that names a network in the panel and the trace label: its palette colour, or for a room its APC's
        /// circuit colour (the hatch colour when it has none), so a room reads in the colour it is tinted.
        /// </summary>
        public Color ColorOf(int viewId)
        {
            if (Structure != null && Structure.Rooms.Count > 0 && Structure.TryGetRoomByViewId(viewId, out SystemsViewRoom room))
                return room.IsHatched ? new Color(HatchColor.r, HatchColor.g, HatchColor.b, 1f) : SystemsViewPalette.ColorFor(room.CircuitViewId);

            return Structure?.ColorOf(viewId) ?? SystemsViewPalette.ColorFor(viewId);
        }

        /// <summary>
        /// A delta from the server. One for a view switched away from may still be in flight, so it is dropped, as is
        /// anything before the new view's reset.
        /// </summary>
        public bool Receive(SystemsViewDelta delta)
        {
            if (delta.View != Requested || delta.View == SystemsViewKind.Off)
                return false;

            if (delta.Reset)
                Structure = new SystemsViewStructure(delta.View);
            else if (Structure == null)
                return false;

            Structure.Apply(delta);
            _overlay.Apply(Structure, delta);
            if (delta.Reset)
            {
                _tintApplied.Clear();
                _roomTint.Invalidate();
            }

            if (delta.Reset || delta.RoomsSet?.Length > 0 || delta.RoomsRemoved?.Length > 0)
                RefreshRoomTint();
            _hoverRevision++;
            if (IsTracing)
                FollowStructure();
            return true;
        }

        private int _hoverRevision;
        private int _describedRevision = -1;
        private Vector2Int _describedTile;
        private string _described = string.Empty;

        /// <summary>
        /// What the view shows on <paramref name="tile"/> in words ("Supply 3", "Pump: on · Surface 2 → Surface 5"), or
        /// empty; worded by the view's domain (<see cref="ISystemsViewStyle.Describe"/>). Cached per tile and delta.
        /// </summary>
        public string HoverText(Vector2Int tile)
        {
            if (Structure == null || Requested == SystemsViewKind.Off)
                return string.Empty;

            if (_describedRevision == _hoverRevision && _describedTile == tile)
                return _described;

            _describedRevision = _hoverRevision;
            _describedTile = tile;
            _described = SystemsViewStyles.For(Structure.View)?.Describe(Structure, tile) ?? string.Empty;
            string problems = ProblemText(Structure, tile);
            if (problems.Length > 0)
                _described = _described.Length > 0 ? problems + "\n" + _described : problems;
            return _described;
        }

        /// <summary>
        /// The problems marked on <paramref name="tile"/>, one row each in the legend's words ("Open pipe end: the pipe
        /// stops here, joined to nothing."), first in the hover so a marker reads before what it sits on.
        /// </summary>
        public static string ProblemText(SystemsViewStructure structure, Vector2Int tile)
        {
            if (structure == null || structure.Markers.Count == 0)
                return string.Empty;

            IReadOnlyList<SystemsViewProblemKind> kinds = SystemsViewDomains.For(structure.View)?.Problems;
            System.Text.StringBuilder text = null;
            for (int slot = 0; slot <= byte.MaxValue; slot++)
            {
                if (!structure.Markers.ContainsKey(new SystemsViewKey(tile, (byte)slot))
                    || !SystemsViewProblems.TryGetKind(kinds, SystemsViewProblems.CodeOf((byte)slot), out SystemsViewProblemKind kind))
                    continue;

                text ??= new System.Text.StringBuilder();
                if (text.Length > 0 && text.ToString().Contains(kind.Sentence))
                    continue;

                SystemsViewText.NewRow(text).Append(kind.Sentence);
            }

            return text?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// The cursor badge with a view on: what is being placed, then what the view shows under the cursor, so a pipe's
        /// line and network show on Lower › Gas Pipes too, where the view turns itself on while you place.
        /// </summary>
        public static string CursorLabel(string placing, string hover) =>
            string.IsNullOrEmpty(hover) ? placing ?? string.Empty
            : string.IsNullOrEmpty(placing) ? hover
            : placing + "\n" + hover;

        /// <summary>The overlay, for tests and the visual pilot.</summary>
        public MapEditorSystemsOverlay Overlay => _overlay;

        private readonly MapEditorRoomAirOverlay _roomAir = new();

        /// <summary>Room air as last asked of the server: wanted, and the Atmos view on (Phase 5).</summary>
        public bool RoomAirRequested { get; private set; }

        /// <summary>The Room air overlay, for tests and the visual pilot.</summary>
        public MapEditorRoomAirOverlay RoomAirOverlay => _roomAir;

        /// <summary>
        /// Every frame while the editor is open, after <see cref="Update"/>: Room air shows only with the Atmos view on.
        /// Returns true when that changed, so the server must be told (it sends a pure client every chunk once). On a host
        /// the server's snapshot is widened to the whole station while it shows.
        /// </summary>
        public bool UpdateRoomAir(bool wanted)
        {
            bool show = wanted && Requested == SystemsViewKind.Atmos;
            bool changed = show != RoomAirRequested;
            if (changed)
            {
                RoomAirRequested = show;
                AtmosSubSystem.SetRoomAirRequested(show);
            }

            _roomAir.SetVisible(show);
            return changed;
        }

        /// <summary>
        /// The editor closed; the server drops the subscription, Room air and any pause it holds with the editor-open report.
        /// Room air goes back off for the next open (creative-mode §4: off by default, so it doesn't bury the pipes).
        /// </summary>
        public void Stop(MapEditorViewModel viewModel = null)
        {
            viewModel?.SetSystemsRoomAir(false);
            Requested = SystemsViewKind.Off;
            Structure = null;
            _overlay.SetStructure(null);
            _overlay.SetVisible(false);
            _roomTint.SetVisible(false);
            RoomAirRequested = false;
            AtmosSubSystem.SetRoomAirRequested(false);
            _roomAir.SetVisible(false);
            EndTrace(tellServer: false);
            _traceRequestPending = false;
        }

        private readonly List<SystemsViewTraceSide> _traceSides = new();
        private readonly List<SystemsViewTraceSide> _targets = new();
        private int _nextTraceId;
        private bool _traceRequestPending;

        /// <summary>The networks the trace follows, in panel order, each with its side of the clicked device; empty when none.</summary>
        public IReadOnlyList<SystemsViewTraceSide> TraceSides => _traceSides;

        public bool IsTracing => _traceSides.Count > 0;

        /// <summary>This client's number for its current trace (or for ending one); the server echoes it on every live message.</summary>
        public int TraceId { get; private set; }

        /// <summary>The tile Select traced from: where the trace label sits.</summary>
        public Vector2Int TraceOrigin { get; private set; }

        /// <summary>The latest live numbers for the current trace; null until the first arrives.</summary>
        public SystemsViewLive? Live { get; private set; }

        /// <summary>Moves on every trace change: a new trace, a side dropped, live numbers, a label renamed. The panel redraws on it.</summary>
        public int TraceRevision { get; private set; }

        /// <summary>
        /// Select with a view on (Q11): traces what the domain says the object on <paramref name="layer"/> at
        /// <paramref name="tile"/> belongs to (<see cref="ISystemsViewStyle.TraceTargets"/>). An object that isn't part
        /// of the view ends the trace instead. Returns whether a trace is on now. Either way the hub sends the server
        /// the change (<see cref="TakeTraceRequest"/>).
        /// </summary>
        public bool BeginTrace(Vector2Int tile, TileLayer layer) => TraceSelect(tile, layer, null);

        /// <summary>
        /// Select's trace from a click: the object it picked (<paramref name="pickedTile"/> on <paramref name="pickedLayer"/>)
        /// when the view shows it, else whatever the view shows on <paramref name="pointedTile"/>, the tile under the
        /// cursor, trying the view's own layer first (<see cref="MapEditorCursorPick.TraceLayerOrder"/>), so a pipe under
        /// a table or behind the wall the raycast stopped at still traces. Never a tile other than those two. Nothing
        /// there ends the trace.
        /// </summary>
        public bool TraceSelect(Vector2Int? pickedTile, TileLayer pickedLayer, Vector2Int? pointedTile)
        {
            if (pickedTile is { } picked && TryTraceTargets(picked, pickedLayer))
                return StartTraceAt(picked);

            if (pointedTile is { } pointed)
            {
                foreach (TileLayer layer in MapEditorCursorPick.TraceLayerOrder(SystemsViewDomains.For(Requested)?.OwnLayer))
                {
                    if (TryTraceTargets(pointed, layer))
                        return StartTraceAt(pointed);
                }
            }

            ClearTrace();
            return false;
        }

        private bool TryTraceTargets(Vector2Int tile, TileLayer layer)
        {
            _targets.Clear();
            if (Structure != null && Requested != SystemsViewKind.Off)
                SystemsViewStyles.For(Structure.View)?.TraceTargets(Structure, tile, layer, _targets);
            return _targets.Count > 0;
        }

        private bool StartTraceAt(Vector2Int tile)
        {
            _traceSides.Clear();
            _traceSides.AddRange(_targets);
            TraceOrigin = tile;
            EndTrace(tellServer: true);
            return true;
        }

        /// <summary>Ends the trace (the panel's Clear, or a Select on something outside the view) and tells the server.</summary>
        public void ClearTrace()
        {
            if (!IsTracing && Live == null)
                return;

            _traceSides.Clear();
            EndTrace(tellServer: true);
        }

        /// <summary>The trace change the server has not heard yet: its id and view ids, empty to end it. False when there is none.</summary>
        public bool TakeTraceRequest(out int traceId, out int[] viewIds)
        {
            traceId = TraceId;
            viewIds = null;
            if (!_traceRequestPending)
                return false;

            _traceRequestPending = false;
            viewIds = new int[_traceSides.Count];
            for (int i = 0; i < viewIds.Length; i++)
                viewIds[i] = _traceSides[i].ViewId;
            return true;
        }

        /// <summary>
        /// Live numbers from the server. One for an older trace or another view is dropped. A network missing from it is
        /// gone (an edit removed it), so its side leaves the trace; when none is left the trace ends, as the server's has.
        /// </summary>
        public bool ReceiveLive(SystemsViewLive live)
        {
            if (live.View != Requested || live.TraceId != TraceId || !IsTracing)
                return false;

            for (int i = _traceSides.Count - 1; i >= 0; i--)
            {
                if (!live.TryFind(_traceSides[i].ViewId, out _))
                    _traceSides.RemoveAt(i);
            }

            Live = IsTracing ? live : null;
            TraceChanged();
            return true;
        }

        /// <summary>
        /// The trace label's lines, one per side ("Waste 4 · Back"), each with its network's view id for its colour. The
        /// label names the network from the structure, so a rename reaches it with the next delta.
        /// </summary>
        public string TraceLabel(SystemsViewTraceSide side)
        {
            string name = Structure != null && Structure.Networks.TryGetValue(side.ViewId, out SystemsViewNetwork network)
                && !string.IsNullOrEmpty(network.Label) ? network.Label : $"Network {side.ViewId}";
            return string.IsNullOrEmpty(side.Side) ? name : name + " · " + side.Side;
        }

        /// <summary>A delta arrived while tracing: a traced network the structure no longer has is gone (identity carries through the tracker otherwise).</summary>
        private void FollowStructure()
        {
            int before = _traceSides.Count;
            _traceSides.RemoveAll(side => !Structure.Networks.ContainsKey(side.ViewId));
            if (_traceSides.Count == 0)
            {
                EndTrace(tellServer: true);
                return;
            }

            if (_traceSides.Count != before)
                _traceRequestPending = true;
            TraceChanged();
        }

        /// <summary>
        /// Moves to a fresh trace id with whatever <see cref="_traceSides"/> now holds (a new trace, or none), so the
        /// server's replies to the old one are ignored. <paramref name="tellServer"/> is false when the server has
        /// already dropped the trace (a view switch, the editor closing); a call with nothing to end does nothing then.
        /// </summary>
        private void EndTrace(bool tellServer)
        {
            if (!tellServer)
            {
                if (!IsTracing && Live == null)
                    return;
                _traceSides.Clear();
            }

            TraceId = ++_nextTraceId;
            Live = null;
            _traceRequestPending |= tellServer;
            TraceChanged();
        }

        private void TraceChanged()
        {
            TraceRevision++;
            bool focusChanged = _focusIds.Count != _traceSides.Count;
            for (int i = 0; !focusChanged && i < _traceSides.Count; i++)
                focusChanged = _focusIds[i] != _traceSides[i].ViewId;
            if (!focusChanged)
                return;

            _focusIds.Clear();
            foreach (SystemsViewTraceSide side in _traceSides)
                _focusIds.Add(side.ViewId);
            _overlay.SetFocus(_focusIds);
            RefreshRoomTint();
        }

        private readonly List<int> _focusIds = new();
        private readonly Dictionary<ushort, MapEditorAreaOverlay.Tint> _tintApplied = new();
        private readonly List<ushort> _tintChanged = new();

        /// <summary>How many times a room's tint changed and its chunks were marked to rebuild; for tests.</summary>
        public int RoomTintInvalidations { get; private set; }

        /// <summary>The rooms whose tint the last refresh changed (only their chunks rebuild); for tests.</summary>
        public IReadOnlyList<ushort> LastTintChangedRooms => _tintChanged;

        /// <summary>
        /// Compares every room's tint with the one last drawn and rebuilds only the chunks of rooms that changed: a delta
        /// or a trace that leaves the rooms' colours alone rebuilds nothing.
        /// </summary>
        private void RefreshRoomTint()
        {
            _tintChanged.Clear();
            if (Structure != null)
            {
                foreach (ushort areaId in Structure.Rooms.Keys)
                {
                    MapEditorAreaOverlay.Tint tint = RoomTint(areaId);
                    if (_tintApplied.TryGetValue(areaId, out MapEditorAreaOverlay.Tint drawn) && drawn.Equals(tint))
                        continue;

                    _tintApplied[areaId] = tint;
                    _tintChanged.Add(areaId);
                }
            }

            foreach (ushort areaId in _tintApplied.Keys)
            {
                if (Structure == null || !Structure.Rooms.ContainsKey(areaId))
                    _tintChanged.Add(areaId);
            }

            foreach (ushort areaId in _tintChanged)
            {
                if (Structure == null || !Structure.Rooms.ContainsKey(areaId))
                    _tintApplied.Remove(areaId);
            }

            if (_tintChanged.Count > 0)
            {
                _roomTint.InvalidateAreas(_tintChanged);
                RoomTintInvalidations++;
            }
        }

        public void Dispose()
        {
            _overlay.Dispose();
            _roomTint.Dispose();
            _roomAir.Dispose();
            // The hub going away (OnDestroyed, a disconnect with the editor open) must never leave the static request latched,
            // or the host uploads the whole station every tick of the next session.
            AtmosSubSystem.SetRoomAirRequested(false);
            RoomAirRequested = false;
        }
    }
}
