using FishNet.Object;
using Serilog;
using FishNet.Transporting;
using FishNet.Connection;
using Serilog.Sinks.Unity3D;
using SS3D.Core.Behaviours;
using Serilog.Core;
using Serilog.Events;
using UnityEngine;

namespace SS3D.Core
{
    // Maybe add property for local client ?
    public class SSLog : NetworkSystem
    {

        private static LoggingLevelSwitch _levelSwitch;

        private void Awake()
        {
            _levelSwitch= new LoggingLevelSwitch();
            _levelSwitch.MinimumLevel = LogEventLevel.Warning;
        }

        public override void OnStartClient()
        {

            base.OnStartClient();
            //ClientLog = new LoggerConfiguration().WriteTo.Unity3D().WriteTo.File("Logs/Client" + id + ".txt").CreateLogger();
            //ClientLog.Information("##########  CLIENT STARTING !  ##########");
            //ClientManager.OnClientConnectionState

            if (IsHost)
            {
                return;
            }

            Log.Logger = new LoggerConfiguration()
                .Enrich.With(new ClientIdEnricher())
                .WriteTo.Unity3D()
                .WriteTo.File(Application.dataPath + "/Logs/LogSession.txt", outputTemplate: "{Timestamp:HH:mm} [{Level}] [ID = {ClientId}] {Message}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("##########  CLIENT STARTING !  ##########");
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Log.Logger = new LoggerConfiguration()
                .Enrich.With(new ClientIdEnricher())
                .WriteTo.Unity3D()
                .WriteTo.File(Application.dataPath + "Logs/LogSession.txt", outputTemplate: "{Timestamp:HH:mm} [{Level}] [ID = {ClientId}] {Message}{NewLine}{Exception}")
                .CreateLogger();
            Log.Information("##########  SERVER STARTING !  ##########");
            ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;
        }

        [Server]
        public void HandleRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs remoteConnectionStateArgs)
        {
            if (remoteConnectionStateArgs.ConnectionState == RemoteConnectionState.Started)
            {
                Log.Information("##########  CLIENT " + conn.ClientId + " STARTING !  ##########");
            }
        }
    }

}

