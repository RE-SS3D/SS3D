using FishNet.Object;
using Serilog;
using FishNet.Transporting;
using FishNet.Connection;
using Serilog.Sinks.Unity3D;
using SS3D.Core.Behaviours;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Formatting.Compact;
using UnityEngine;
using Serilog.Configuration;
using Serilog.Formatting;
using Serilog.Sinks.File;
using System.Runtime.Serialization;
using System.Text;
using System;
using UnityEditor;

namespace SS3D.Core
{
    // Maybe add property for local client ?
    public class LogManager : NetworkSystem
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
                if (IsHost)
                {
                    return;
                }
                Log.Logger = new LoggerConfiguration()
               .Enrich.With(new ClientIdEnricher())
               .WriteTo.Unity3D()
               .WriteTo.File( new CompactJsonFormatter()
               , "C:/Users/Nat/Documents/GitHub/StilnatSS3DMain/Logs/LogClient" + ClientManager.Connection.ClientId + ".json")
               .CreateLogger();
               Log.Information("##########  CLIENT STARTING !  ##########");
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (IsServerOnly)
            {
                  Log.Logger = new LoggerConfiguration()
                 .Enrich.With(new ClientIdEnricher())
                 .WriteTo.Unity3D()
                 .WriteTo.File(new CompactJsonFormatter()
                 , "C:/Users/Nat/Documents/GitHub/StilnatSS3DMain/Logs/LogServer.json")
                 .CreateLogger();
            }
            else
            {
                   Log.Logger = new LoggerConfiguration()
                  .Enrich.With(new ClientIdEnricher())
                  .WriteTo.Unity3D()
                  .WriteTo.File(new CompactJsonFormatter()
                  , "C:/Users/Nat/Documents/GitHub/StilnatSS3DMain/Logs/LogHost.json")
                  .CreateLogger();
            }

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

