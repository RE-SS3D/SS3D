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
