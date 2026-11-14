using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Systems views transport on the hub (Documents/architecture/2026-11_map-editor-systems-views.md Q2, Q10). The server
    /// half is <see cref="SystemsViewPublisher"/>, the client half <see cref="MapEditorSystemsViewClient"/>.
    /// </summary>
    public sealed partial class MapEditorSubSystem
    {
        private const float SystemsViewPollSeconds = 0.25f;

        private readonly MapEditorSystemsViewClient _systemsViewClient = new();
        private float _systemsViewNextPoll;

        /// <summary>Server: domain sources register here; only editor clients are ever subscribed.</summary>
        public SystemsViewPublisher SystemsViews { get; } = new();

        private void OnSystemsViewSelected(SystemsViewKind view)
        {
            _viewModel.SetSystemsView(view);
            MapEditorLayerVisibility.Sync(_viewModel);
        }

        private void UpdateSystemsViewClient()
        {
            if (_systemsViewClient.Update(_viewModel.ActiveSystemsView) && NetworkObject != null && NetworkObject.IsSpawned && IsClient)
                RpcSetSystemsView(_systemsViewClient.Requested, LocalConnection);
        }

        /// <summary>Hovering names what the view shows under the cursor (creative-mode §4: a pipe's line and network).</summary>
        private string SystemsViewHoverLabel()
        {
            if (_viewModel.ActiveSystemsView == SystemsViewKind.Off)
                return string.Empty;

            Vector3 pointed = TileHelper.GetPointedPosition(true, PickCamera);
            return _systemsViewClient.HoverText(new Vector2Int(Mathf.RoundToInt(pointed.x), Mathf.RoundToInt(pointed.z)));
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

        private void HandleSystemsViewServerUpdate()
        {
            if (!IsServer || !SystemsViews.HasSubscribers || Time.unscaledTime < _systemsViewNextPoll)
                return;

            _systemsViewNextPoll = Time.unscaledTime + SystemsViewPollSeconds;
            SystemsViews.Publish((clientId, delta) =>
            {
                if (ServerManager.Clients.TryGetValue(clientId, out NetworkConnection conn))
                    TargetSystemsViewDelta(conn, delta);
            });
        }
    }
}
