using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.TileMapCreator;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Systems views transport on the hub (Documents/architecture/2026-11_map-editor-systems-views.md Q2, Q10): the
    /// structure stream, and the live stream of a traced network to the one client tracing it. The server half is
    /// <see cref="SystemsViewPublisher"/>, the client half <see cref="MapEditorSystemsViewClient"/>.
    /// </summary>
    public sealed partial class MapEditorSubSystem
    {
        private const float SystemsViewPollSeconds = 0.25f;
        /// <summary>Traced numbers are re-read every this many structure polls: about 2 Hz (Q2).</summary>
        private const int SystemsViewPollsPerLive = 2;

        private readonly MapEditorSystemsViewClient _systemsViewClient = new();
        private float _systemsViewNextPoll;
        private int _systemsViewPolls;

        /// <summary>Server: domain sources register here; only editor clients are ever subscribed.</summary>
        public SystemsViewPublisher SystemsViews { get; } = new();

        private void OnSystemsViewSelected(SystemsViewKind view)
        {
            _viewModel.SetSystemsView(view);
            MapEditorLayerVisibility.Sync(_viewModel);
        }

        private void UpdateSystemsViewClient()
        {
            bool online = NetworkObject != null && NetworkObject.IsSpawned && IsClient;
            if (_systemsViewClient.Update(_viewModel.ActiveSystemsView, _viewModel.IsLayerCategoryVisible(TileLayerCategory.Areas)) && online)
                RpcSetSystemsView(_systemsViewClient.Requested, LocalConnection);
            // After the view request, so the server has the subscription the trace belongs to.
            if (_systemsViewClient.TakeTraceRequest(out int traceId, out int[] viewIds) && online)
                RpcTraceSystemsView(_systemsViewClient.Requested, traceId, viewIds, LocalConnection);
            _view?.UpdateSystemsTrace(_systemsViewClient, PickCamera);
        }

        /// <summary>
        /// Select with a view on also traces (Q11): the clicked object's network or networks, beside whatever Select
        /// does to the object itself. The pick's raycast stops at the first collider, often a dimmed floor, table or
        /// wall, so when that object isn't part of the view the trace reads the tile under the cursor at the overlay's
        /// height, the view's own layer first (<see cref="MapEditorSystemsViewClient.TraceSelect"/>). Nothing in the
        /// view there ends the trace.
        /// </summary>
        private void TraceSystemsView(PlacedTileObject placed)
        {
            if (_viewModel.ActiveSystemsView == SystemsViewKind.Off)
                return;

            Vector2Int? pointed = MapEditorCursorPick.TryGetPointedTile(PickCamera, MapEditorSystemsOverlay.Height, out Vector2Int tile)
                ? tile : null;
            _systemsViewClient.TraceSelect(placed != null ? placed.WorldOrigin : null, placed != null ? placed.Layer : default, pointed);
        }

        private void OnSystemsTraceLocate(Vector2Int tile) => _session.CenterOn(new Vector3(tile.x, 0f, tile.y));

        private void OnSystemsTraceClosed() => _systemsViewClient.ClearTrace();

        private void BindSystemsTraceView(bool bind)
        {
            if (_view == null)
                return;

            _view.SystemsTraceLocateRequested -= OnSystemsTraceLocate;
            _view.SystemsTraceCloseRequested -= OnSystemsTraceClosed;
            if (!bind)
                return;

            _view.SystemsTraceLocateRequested += OnSystemsTraceLocate;
            _view.SystemsTraceCloseRequested += OnSystemsTraceClosed;
        }

        /// <summary>Hovering names what the view shows under the cursor (creative-mode §4: a pipe's line and network).</summary>
        private string SystemsViewHoverLabel()
        {
            if (_viewModel.ActiveSystemsView == SystemsViewKind.Off)
                return string.Empty;

            // The overlay's own plane, as Select's trace reads it, so what hover names is what a click traces.
            return MapEditorCursorPick.TryGetPointedTile(PickCamera, MapEditorSystemsOverlay.Height, out Vector2Int tile)
                ? _systemsViewClient.HoverText(tile) : string.Empty;
        }

        /// <summary>The client half, for the visual pilot and tests.</summary>
        public MapEditorSystemsViewClient SystemsViewClient => _systemsViewClient;

        [ServerRpc(RequireOwnership = false)]
        private void RpcSetSystemsView(SystemsViewKind view, NetworkConnection conn = null)
        {
            if (conn != null)
                SystemsViews.Subscribe(conn.ClientId, IsEditing(conn) && MapEditorPermissions.TryAuthorize(conn) ? view : SystemsViewKind.Off);
        }

        [TargetRpc]
        private void TargetSystemsViewDelta(NetworkConnection conn, SystemsViewDelta delta)
        {
            if (_active)
                _systemsViewClient.Receive(delta);
        }

        /// <summary>Starts, replaces or (no ids) ends this client's trace; gated like <see cref="RpcSetSystemsView"/>.</summary>
        [ServerRpc(RequireOwnership = false)]
        private void RpcTraceSystemsView(SystemsViewKind view, int traceId, int[] viewIds, NetworkConnection conn = null)
        {
            if (conn == null)
                return;

            if (IsEditing(conn) && MapEditorPermissions.TryAuthorize(conn))
                SystemsViews.Trace(conn.ClientId, view, traceId, viewIds);
            else
                SystemsViews.ClearTrace(conn.ClientId);
        }

        [TargetRpc]
        private void TargetSystemsViewLive(NetworkConnection conn, SystemsViewLive live)
        {
            if (_active)
                _systemsViewClient.ReceiveLive(live);
        }

        // Map Check reports sent while a systems view's analysis was still catching up (right after a load, when the power
        // graph rebuilds a tick later): each such client gets its report again once every source is current.
        private readonly HashSet<int> _validationAwaitingSystems = new();

        /// <summary>After a Map Check send: if the systems' problems were not current yet, send this client the report again later.</summary>
        private void AwaitSystemsForValidation(NetworkConnection conn)
        {
            if (conn != null && !SystemsViews.AnalysisCurrent)
                _validationAwaitingSystems.Add(conn.ClientId);
        }

        private void ResendValidationOnceSystemsSettle()
        {
            if (_validationAwaitingSystems.Count == 0 || !SystemsViews.AnalysisCurrent)
                return;

            int[] clientIds = new int[_validationAwaitingSystems.Count];
            _validationAwaitingSystems.CopyTo(clientIds);
            _validationAwaitingSystems.Clear();
            foreach (int clientId in clientIds)
            {
                if (ServerManager.Clients.TryGetValue(clientId, out NetworkConnection conn) && IsEditing(conn))
                    SendMapValidation(conn);
            }
        }

        private void HandleSystemsViewServerUpdate()
        {
            if (IsServer)
                ResendValidationOnceSystemsSettle();
            if (!IsServer || !SystemsViews.HasSubscribers || Time.unscaledTime < _systemsViewNextPoll)
                return;

            _systemsViewNextPoll = Time.unscaledTime + SystemsViewPollSeconds;
            SystemsViews.Publish((clientId, delta) =>
            {
                if (ServerManager.Clients.TryGetValue(clientId, out NetworkConnection conn))
                    TargetSystemsViewDelta(conn, delta);
            });

            // Right after the structure, so traced view ids are those just sent.
            if (++_systemsViewPolls % SystemsViewPollsPerLive != 0 || !SystemsViews.HasTraces)
                return;

            SystemsViews.PublishLive((clientId, live) =>
            {
                if (ServerManager.Clients.TryGetValue(clientId, out NetworkConnection conn))
                    TargetSystemsViewLive(conn, live);
            });
        }
    }
}
