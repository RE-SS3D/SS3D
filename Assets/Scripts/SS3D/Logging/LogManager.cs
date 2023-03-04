using FishNet.Object;
using Serilog;
using FishNet.Transporting;
using FishNet.Connection;
using Serilog.Sinks.Unity3D;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using SS3D.Logging;
using UnityEngine;
using FishNet.Managing.Client;
using System.Runtime.CompilerServices;
using FishNet;
using System;

namespace SS3D.Core
{
    /// <summary>
    /// Set up Serilog's Logger for clients, host and server. 
    /// </summary>
    public static class LogManager
    {

        private static LoggingLevelSwitch _levelSwitch;
        private static readonly string defaultUnityLogTemplate;
        private static readonly string LogFolderPath;
        private static bool _isInitialized;

        static LogManager()
        {
            defaultUnityLogTemplate = "{SourceContext} {Message:lj}{NewLine}{Exception}";
            LogFolderPath = Application.dataPath + "/Logs/";
            _levelSwitch = new LoggingLevelSwitch();
            _levelSwitch.MinimumLevel = LogEventLevel.Warning;
        }

        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;


            var configuration = new LoggerConfiguration();
            configuration = new LoggerConfiguration()
                                .Enrich.With(new ClientIdEnricher())
                                .MinimumLevel.Verbose()
                                .WriteTo.Unity3D(formatter: new SS3DUnityTextFormatter(outputTemplate: defaultUnityLogTemplate));

            if (InstanceFinder.IsHost)
            {
                configuration = configuration.WriteTo.File(new CompactJsonFormatter()
                , LogFolderPath + "LogHost.json");
            }
            else if (InstanceFinder.IsClientOnly)
            {
                configuration = configuration.WriteTo.File(new CompactJsonFormatter()
                , LogFolderPath + "LogClient" + InstanceFinder.NetworkManager.ClientManager.Connection.ClientId + ".json");
            }
            else if (InstanceFinder.IsServerOnly)
            {
                configuration = configuration.WriteTo.File(new CompactJsonFormatter()
                , LogFolderPath + "LogServer.json");
            }

            Log.Logger = configuration.CreateLogger();
        }

        public static void OnServerStarted(object sender, EventArgs e)
        {
            Initialize();
        }
    }

}

