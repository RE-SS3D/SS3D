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
            public static LoggerConfiguration File(this LoggerSinkConfiguration sinkConfiguration, string path, LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose, string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", IFormatProvider? formatProvider = null, long? fileSizeLimitBytes = 1073741824L, LoggingLevelSwitch? levelSwitch = null, bool buffered = false, bool shared = false, TimeSpan? flushToDiskInterval = null, RollingInterval rollingInterval = RollingInterval.Infinite, bool rollOnFileSizeLimit = false, int? retainedFileCountLimit = 31, Encoding? encoding = null, FileLifecycleHooks? hooks = null, TimeSpan? retainedFileTimeLimit = null)

                base.OnStartClient();
                if (IsHost)
                {
                    return;
                }
                Log.Logger = new LoggerConfiguration()
                .Enrich.With(new ClientIdEnricher())
                .WriteTo.Unity3D()
                .WriteTo.File( new CompactJsonFormatter()
                , "C:/Users/Nat/Documents/GitHub/StilnatSS3DMain/Logs/LogClient" + ClientManager.Connection.ClientId + ".txt")
                .CreateLogger();
                Log.Information("##########  CLIENT STARTING !  ##########");
            }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (IsServerOnly)
            {
                Log.Logger = new LoggerConfiguration()
                .WriteTo.Unity3D()
                .WriteTo.File("C:/Users/Nat/Documents/GitHub/StilnatSS3DMain/Logs/LogServer.txt"
                , outputTemplate: "{Timestamp:HH:mm} [{Level}] [ID = SERVER] {Message}{NewLine}{Exception}"
                , shared: true)
                                .CreateLogger();
            }
            else
            {
                Log.Logger = new LoggerConfiguration()
                .WriteTo.Unity3D()
                .WriteTo.File("C:/Users/Nat/Documents/GitHub/StilnatSS3DMain/Logs/LogHost.txt"
                , outputTemplate: "{Timestamp:HH:mm} [{Level}] [ID = HOST] {Message}{NewLine}{Exception}"
                , shared: true)
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

