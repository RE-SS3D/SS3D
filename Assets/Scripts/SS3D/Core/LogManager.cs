using Serilog;
using Serilog.Sinks.Unity3D;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using UnityEngine;
using FishNet;
using System;
using System.Linq;
using System.Collections.Generic;
using SS3D.Logging.LogSettings;
using SS3D.Core.Logging;
using SS3D.Data;


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

        private static LogSetting settings;

        static LogManager()
        {
            defaultUnityLogTemplate = "{SourceContext} {Message:lj}{NewLine}{Exception}";
            LogFolderPath = Application.dataPath + "/Logs/";
            _levelSwitch = new LoggingLevelSwitch();
            _levelSwitch.MinimumLevel = LogEventLevel.Warning;
            settings = Assets.Get<LogSetting>(Data.Enums.AssetDatabases.Settings, (int)Data.Enums.SettingIds.LogSetting);
        }

        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;



            // Add enricher and configure the global logging level.
            var configuration = new LoggerConfiguration()
                                .Enrich.With(new ClientIdEnricher());
            configuration = ConfigureMinimumLevel(configuration);

            // Apply some override on the minimum logging level for some namespaces.
            // Does not apply override if the logging level corresponds to the global minimum level.
            foreach (var levelForNameSpace in settings.SS3DNameSpaces)
            {
                if (levelForNameSpace.Level == settings.defaultLogLevel) continue;

                configuration = configuration.MinimumLevel.Override(levelForNameSpace.Name, levelForNameSpace.Level);
            }

            // Configure writing to Unity's console, using our custom text formatter.
            configuration = configuration.WriteTo.Unity3D(formatter: new SS3DUnityTextFormatter(outputTemplate: defaultUnityLogTemplate));

            // Configure writing to log files using a CompactJsonFormatter. The path of the log file depends if connection is host, server only, or client.
            // Write in a different file depending on client's connection id.
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

            // Create the logger from the configuration.
            Log.Logger = configuration.CreateLogger();
        }

        /// <summary>
        /// Suscribe to event ServerOrClientStarted, to initialize Log Manager when stuff networking wise are correctly set up.
        /// </summary>
        public static void OnServerStarted(object sender, EventArgs e)
        {
            Initialize();
        }

        private static LoggerConfiguration ConfigureMinimumLevel(LoggerConfiguration loggerConfiguration)
        {
            switch (settings.defaultLogLevel)
            {
                case LogEventLevel.Verbose: return loggerConfiguration.MinimumLevel.Verbose();
                case LogEventLevel.Debug: return loggerConfiguration.MinimumLevel.Debug();
                case LogEventLevel.Information: return loggerConfiguration.MinimumLevel.Information();
                case LogEventLevel.Warning: return loggerConfiguration.MinimumLevel.Warning();
                case LogEventLevel.Error: return loggerConfiguration.MinimumLevel.Error();
                case LogEventLevel.Fatal: return loggerConfiguration.MinimumLevel.Fatal();
                default: return loggerConfiguration.MinimumLevel.Information();
            }
        }
    }

}

