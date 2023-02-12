using FishNet.Object;
using Serilog;
using System.Collections.Generic;
using FishNet.Transporting;
using FishNet.Connection;
using System.Linq;
using Serilog.Sinks.Unity3D;
using SS3D.Core.Behaviours;

namespace SS3D.Core
{
    // Maybe add property for local client ?
    public class LogManager: NetworkSystem
    {

        private struct ClientLogger
        {
            public Serilog.ILogger ClientLog;
            public NetworkConnection Conn;

            public ClientLogger(Serilog.ILogger clientLog, NetworkConnection conn)
            {
                ClientLog = clientLog;
                Conn = conn;
            }
        }

        private Serilog.ILogger _serverLog;
        private List<ClientLogger> _clientLogs;

        public Serilog.ILogger ClientLog(NetworkConnection conn)
        {
            return _clientLogs.FirstOrDefault(log => log.Conn == conn).ClientLog;
        }


        public override void OnStartClient()
        {

            base.OnStartClient();
            //ClientLog = new LoggerConfiguration().WriteTo.Unity3D().WriteTo.File("Logs/Client" + id + ".txt").CreateLogger();
            //ClientLog.Information("##########  CLIENT STARTING !  ##########");
            //ClientManager.OnClientConnectionState
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            _serverLog = new LoggerConfiguration().WriteTo.Unity3D().WriteTo.File("Logs/ServerLog.txt").CreateLogger();
            _clientLogs = new List<ClientLogger>();

            _serverLog.Information("##########  SERVER STARTING !  ##########");

            ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;

            if (IsHost)
            {
                AddClientLogger(LocalConnection);
            }

        }

        [Server]
        public void HandleRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs remoteConnectionStateArgs)
        {
            if (remoteConnectionStateArgs.ConnectionState == RemoteConnectionState.Started)
            {
                AddClientLogger(conn);
            }
        }

        private void AddClientLogger(NetworkConnection conn)
        {
            _clientLogs.Add(
                    new ClientLogger(
                        new LoggerConfiguration().WriteTo.Unity3D().WriteTo.File("Logs/Client" + conn.ClientId + "Log.txt").CreateLogger(),
                        conn
                    )
                );
            ClientLog(conn).Information("##########  CLIENT " + conn.ClientId + " STARTING !  ##########");
        }
    }

}

