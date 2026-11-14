using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using SS3D.Interactions;
using SS3D.Systems.Electricity;
using SS3D.Systems.Entities;
using UnityEngine;
using SS3D.Core;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Builder-facing map editor settings that outlive the editor overlay, so a map can be play-tested
    /// right after closing it: always-powered light fixtures (server-wide) and per-builder invincibility.
    /// </summary>
    public sealed partial class MapEditorSubSystem
    {
        private bool _invincibleSentForSession;

        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            ResetAuthoringState();
            ServerInteractionSteps.Wrap = null;
            ServerManager.OnRemoteConnectionState -= HandleRemoteConnectionState;
            LightingDevBypass.MapEditorOverride = false;
        }

        private void HandleRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            // FishNet reuses client ids; never let a new joiner inherit a builder's invincibility or open editor.
            if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                EntityInvincibility.ClearServer(conn.ClientId);
                SetEditing(conn.ClientId, false);
            }
        }

        /// <summary>Invincible defaults on; push it the first time the editor opens this session.</summary>
        private void ApplySessionSettingsOnOpen()
        {
            if (_invincibleSentForSession)
                return;

            _invincibleSentForSession = true;
            RpcSetInvincible(_viewModel.Invincible, LocalConnection);
        }

        private void OnAlwaysPowerLightsChanged(bool value)
        {
            _viewModel.AlwaysPowerLights = value;
            RpcSetAlwaysPowerLights(value, LocalConnection);
        }

        private void OnInvincibleChanged(bool value)
        {
            _viewModel.Invincible = value;
            RpcSetInvincible(value, LocalConnection);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcSetAlwaysPowerLights(bool enabled, NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn))
                return;

            LightingDevBypass.MapEditorOverride = enabled;
            LightPower.RefreshAllFixtures();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcSetInvincible(bool enabled, NetworkConnection conn = null)
        {
            if (conn == null || !MapEditorPermissions.TryAuthorize(conn))
                return;

            if (!MapEditorPermissions.MayToggleInvincibility(conn))
            {
                // Sandbox players stay vulnerable while editing; correct the client's mirror.
                EntityInvincibility.SetServer(conn.ClientId, false);
                TargetSetInvincible(conn, false);
                return;
            }

            EntityInvincibility.SetServer(conn.ClientId, enabled);
            TargetSetInvincible(conn, enabled);
        }

        [TargetRpc]
        private void TargetSetInvincible(NetworkConnection conn, bool enabled) =>
            EntityInvincibility.LocalPlayerInvincible = enabled;
    }
}
