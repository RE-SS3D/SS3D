using System;
using System.Collections.Generic;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Client half of the systems views (Documents/architecture/2026-11_map-editor-systems-views.md Q2, Q10): which view
    /// it asked the server for, the structure the server has sent for it, the overlay drawing that structure, and the
    /// trace (Phase 2): which networks a Select followed, the live numbers the server sends for them, and the label.
    /// The RPCs live on the hub, in <c>MapEditorSystemsView.cs</c>; it sends what <see cref="TakeTraceRequest"/> hands it.
    /// </summary>
    public sealed class MapEditorSystemsViewClient : IDisposable
    {
        private readonly MapEditorSystemsOverlay _overlay = new();

        /// <summary>The view last asked of the server.</summary>
        public SystemsViewKind Requested { get; private set; }

        /// <summary>What the server has sent for <see cref="Requested"/>; null until its reset arrives.</summary>
        public SystemsViewStructure Structure { get; private set; }

        /// <summary>
        /// Every frame while the editor is open, with the view now on. Returns true when that differs from
        /// <see cref="Requested"/>, so the server must be asked for it; the old view's structure is dropped.
        /// </summary>
        public bool Update(SystemsViewKind active)
        {
            bool changed = active != Requested;
            if (changed)
            {
                Requested = active;
                Structure = null;
                _overlay.SetStructure(null);
                _hoverRevision++;
                // The server starts the new view (or none) with no trace, so nothing needs asking.
                EndTrace(tellServer: false);
            }

            _overlay.SetVisible(active != SystemsViewKind.Off);
            return changed;
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
            return _described;
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

        /// <summary>The editor closed; the server drops the subscription with the editor-open report.</summary>
        public void Stop()
        {
            Requested = SystemsViewKind.Off;
            Structure = null;
            _overlay.SetStructure(null);
            _overlay.SetVisible(false);
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
            _focusIds.Clear();
            foreach (SystemsViewTraceSide side in _traceSides)
                _focusIds.Add(side.ViewId);
            _overlay.SetFocus(_focusIds);
        }

        private readonly List<int> _focusIds = new();

        public void Dispose() => _overlay.Dispose();
    }
}
