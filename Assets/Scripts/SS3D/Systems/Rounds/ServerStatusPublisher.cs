using System.Linq;
using FishNet;
using FishNet.Transporting.Tugboat.Server;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Networking;
using SS3D.Networking.Status;
using SS3D.Systems.PlayerControl;
using UnityEngine;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// Answers the main menu's status query (<see cref="ServerStatusPacket"/>) on the game port while
    /// this process runs a server (dedicated or host). The reply is rebuilt here on the main thread
    /// once a second; the transport's handler may run on its socket thread and only reads the cached
    /// bytes. Process-wide, created by <c>SystemsBootstrap</c>.
    /// </summary>
    public sealed class ServerStatusPublisher : SubSystem
    {
        private const float RefreshSeconds = 1f;
        private static volatile byte[] _reply;
        private float _nextRefresh;

        protected override void OnAwake()
        {
            base.OnAwake();
            ServerSocket.UnconnectedRequestHandler = HandleRequest;
        }

        protected override void OnDestroyed()
        {
            if (ServerSocket.UnconnectedRequestHandler == HandleRequest)
            {
                ServerSocket.UnconnectedRequestHandler = null;
            }

            _reply = null;
            base.OnDestroyed();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh)
            {
                return;
            }

            _nextRefresh = Time.unscaledTime + RefreshSeconds;
            _reply = InstanceFinder.IsServer ? ServerStatusPacket.EncodeReply(CurrentStatus()) : null;
        }

        private static ServerStatus CurrentStatus()
        {
            string name = ServerInfoText.DefaultName;
            int cap = ServerSettings.Default.PlayerCap;
            if (SubSystems.TryGet(out RoundSubSystem rounds))
            {
                ServerSettings settings = rounds.SettingsForServer;
                if (!string.IsNullOrWhiteSpace(settings.ServerName))
                {
                    name = settings.ServerName;
                }

                cap = rounds.PlayerCap;
            }

            int players = SubSystems.TryGet(out PlayerSubSystem playerSubSystem) ? playerSubSystem.OnlinePlayers.Count() : 0;
            return new ServerStatus(ProtocolVersion.Current, UnityEngine.Application.version, name, players, cap);
        }

        private static byte[] HandleRequest(byte[] request) => ServerStatusPacket.IsRequest(request) ? _reply : null;
    }
}
