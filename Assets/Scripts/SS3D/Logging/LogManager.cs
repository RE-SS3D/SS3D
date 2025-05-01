using Coimbra;
using FishNet;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Unity3D;
using SS3D.Logging.LogSettings;
using System.IO;
using UnityEngine;

namespace SS3D.Logging
{
    /// <summary>
    /// Set up Serilog's Logger for clients, host and server. 
    /// This heavily rely on LogSetting scriptable object to configure the logger.
    /// Check in Assets/Settings the LogSetting scriptable object, there you can configure the logging level per namespace,
    /// as well as the default global logging level.
    /// </summary>
    public static class LogManager
    {
        private static readonly string DefaultUnityLogTemplate;
        private static readonly string LogFolderPath;
        private static readonly LogSettings.LogSettings Settings;
        private static bool IsInitialized;

        static LogManager()
        {
            DefaultUnityLogTemplate = "{SourceContext} {Message}{NewLine}{Exception}";
            LogFolderPath = Path.Combine(Application.dataPath, "../Logs");

            if (Application.isPlaying)
            {
                Settings = ScriptableSettings.GetOrFind<LogSettings.LogSettings>();
            }
        }

        public static void Initialize(string logPath = null)
        {
            if (IsInitialized)
            {
                return;
            }

            LoggerConfiguration configuration = new LoggerConfiguration();

            if (Application.isPlaying)
            {
                configuration = ConfigureForPlayMode(configuration, logPath);
            }

            // Configure writing to Unity's console, using our custom text formatter.
            configuration = configuration.WriteTo.Unity3D(formatter: new SS3DUnityTextFormatter(outputTemplate: DefaultUnityLogTemplate));

            // Create the logger from the configuration.
            Serilog.Log.Logger = configuration.CreateLogger();

            Log.Information(typeof(LogManager), "Logging settings loaded and initialized", Logs.Important);

            IsInitialized = true;
        }

        private static LoggerConfiguration ConfigureForPlayMode(LoggerConfiguration configuration, string overrideLogPath = null)
        {
            configuration.Enrich.With(new ClientIdEnricher());
            configuration = ConfigureMinimumLevel(configuration);

            // Apply some override on the minimum logging level for some namespaces using the log settings.
            // Does not apply override if the logging level corresponds to the global minimum level.
            foreach (NamespaceLogLevel levelForNameSpace in Settings.SS3DNameSpaces)
            {
                if (levelForNameSpace.Level == Settings.DefaultLogLevel)
                {
                    continue;
                }

                configuration = configuration.MinimumLevel.Override(levelForNameSpace.Name, levelForNameSpace.Level);
            }

            // The path of the log file depends if connection is host, server only, or client.
            // Write in a different file depending on client's connection id.
            string path = "Log.json";

            if (!string.IsNullOrEmpty(overrideLogPath))
            {
                path = overrideLogPath;
            }
            else if (InstanceFinder.IsHost)
            {
                path = "LogHost.json";
            }
            else if (InstanceFinder.IsClientOnly)
            {
                path = $"LogClient_{InstanceFinder.NetworkManager.ClientManager.Connection.ClientId}.json";
            }
            else if (InstanceFinder.IsServerOnly)
            {
                path = "LogServer.json";
            }

            path = Path.Combine(LogFolderPath, path);

            if (Settings.UseCompactJsonFormatter)
			{
				configuration.WriteTo.File(new CompactJsonFormatter(), path);
			}
            else
			{
				path = Path.ChangeExtension(path, "log");
				configuration.WriteTo.File(path);
			}

            return configuration;
        }

        /// <summary>
        /// Simply configure the global log level using the one chosen in LogSetting.
        /// </summary>
        private static LoggerConfiguration ConfigureMinimumLevel(LoggerConfiguration loggerConfiguration)
        {
             switch (Settings.DefaultLogLevel)
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
