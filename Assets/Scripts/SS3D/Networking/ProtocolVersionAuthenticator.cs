using System;
using System.Collections;
using FishNet.Authenticating;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Networking
{
    /// <summary>Client → server on connect: what this build speaks.</summary>
    public struct ProtocolHelloBroadcast : IBroadcast
    {
        public int Protocol;
        public string Version;
    }

    /// <summary>
    /// Server → client: accept/reject plus the server's own build, so a rejected client can say why.
    /// Unlike every other message this one crosses versions, so its name and fields must not change.
    /// </summary>
    public struct ProtocolResultBroadcast : IBroadcast
    {
        public bool Accepted;
        public int ServerProtocol;
        public string ServerVersion;
    }

    /// <summary>
    /// FishNet authenticator that only admits clients whose <see cref="ProtocolVersion.Current"/>
    /// matches the server's. Installed at runtime by <see cref="EnsureOn"/> (no scene wiring), before
    /// any StartConnection. Host's local client goes through the same check and always passes.
    /// </summary>
    public sealed class ProtocolVersionAuthenticator : Authenticator
    {
        public override event Action<NetworkConnection, bool> OnAuthenticationResult;

        private static string LocalVersion => UnityEngine.Application.version;

        public static void EnsureOn(NetworkManager networkManager)
        {
            if (networkManager == null || networkManager.ServerManager.GetAuthenticator() != null)
            {
                return;
            }

            if (!networkManager.TryGetComponent(out ProtocolVersionAuthenticator authenticator))
            {
                authenticator = networkManager.gameObject.AddComponent<ProtocolVersionAuthenticator>();
            }

            networkManager.ServerManager.SetAuthenticator(authenticator);
        }

        public override void InitializeOnce(NetworkManager networkManager)
        {
            base.InitializeOnce(networkManager);
            NetworkManager.ClientManager.OnClientConnectionState += HandleClientConnectionState;
            NetworkManager.ServerManager.RegisterBroadcast<ProtocolHelloBroadcast>(HandleHello, false);
            NetworkManager.ClientManager.RegisterBroadcast<ProtocolResultBroadcast>(HandleResult);
            NetworkManager.ClientManager.RegisterBroadcast<DisconnectBroadcast>(HandleDisconnect);
            NetworkManager.ClientManager.RegisterBroadcast<LoginAcceptedBroadcast>(HandleLoginAccepted);
        }

        private void OnDestroy()
        {
            if (NetworkManager == null)
            {
                return;
            }

            NetworkManager.ClientManager.OnClientConnectionState -= HandleClientConnectionState;
            NetworkManager.ServerManager.UnregisterBroadcast<ProtocolHelloBroadcast>(HandleHello);
            NetworkManager.ClientManager.UnregisterBroadcast<ProtocolResultBroadcast>(HandleResult);
            NetworkManager.ClientManager.UnregisterBroadcast<DisconnectBroadcast>(HandleDisconnect);
            NetworkManager.ClientManager.UnregisterBroadcast<LoginAcceptedBroadcast>(HandleLoginAccepted);
        }

        private void HandleClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started)
            {
                return;
            }

            ConnectionNotices.Clear();
            NetworkManager.ClientManager.Broadcast(new ProtocolHelloBroadcast
            {
                Protocol = ProtocolVersion.Current,
                Version = LocalVersion,
            });
        }

        private void HandleHello(NetworkConnection conn, ProtocolHelloBroadcast hello)
        {
            if (conn.Authenticated)
            {
                ConnectionNotices.Disconnect(conn, DisconnectCode.Kicked, "duplicate protocol hello");
                return;
            }

            bool accepted = hello.Protocol == ProtocolVersion.Current;
            if (!accepted)
            {
                Log.Warning(this, "Rejected client {clientId}: {reason}", Logs.Important, conn.ClientId,
                    ProtocolVersion.Describe(LocalVersion, ProtocolVersion.Current, hello.Version, hello.Protocol));
            }

            // Send before the result: a rejected connection is disconnected (non-immediately) by ServerManager.
            NetworkManager.ServerManager.Broadcast(conn, new ProtocolResultBroadcast
            {
                Accepted = accepted,
                ServerProtocol = ProtocolVersion.Current,
                ServerVersion = LocalVersion,
            }, false);
            OnAuthenticationResult?.Invoke(conn, accepted);
        }

        private void HandleResult(ProtocolResultBroadcast result)
        {
            if (result.Accepted)
            {
                return;
            }

            string reason = ProtocolVersion.Describe(result.ServerVersion, result.ServerProtocol, LocalVersion, ProtocolVersion.Current);
            ConnectionNotices.Record(DisconnectCode.VersionMismatch, reason);
            Log.Error(this, "{reason}", Logs.Important, reason);
        }

        private void HandleDisconnect(DisconnectBroadcast notice)
        {
            ConnectionNotices.Record(notice.Code, notice.Text);
            Log.Information(this, "Server is disconnecting us: {reason}", Logs.Important, ConnectionNotices.LastReason);
            NetworkManager.ClientManager.StopConnection();
        }

        /// <summary>Server: the fallback for a client that was told to leave and doesn't.</summary>
        internal void CloseAfterGrace(NetworkConnection conn) => StartCoroutine(CloseAfterGraceRoutine(conn));

        private static IEnumerator CloseAfterGraceRoutine(NetworkConnection conn)
        {
            yield return new WaitForSecondsRealtime(ConnectionNotices.GraceSeconds);
            if (conn.IsActive)
            {
                conn.Disconnect(false);
            }
        }

        private void HandleLoginAccepted(LoginAcceptedBroadcast accepted) => ConnectionNotices.RaiseLoginAccepted();
    }
}
