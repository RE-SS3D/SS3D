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
using System.Linq;
using System.Collections.Generic;

namespace SS3D.Logging
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
        private static readonly List<string> SS3DNameSpaces;

        static LogManager()
        {
            defaultUnityLogTemplate = "{SourceContext} {Message:lj}{NewLine}{Exception}";
            LogFolderPath = Application.dataPath + "/Logs/";
            _levelSwitch = new LoggingLevelSwitch();
            _levelSwitch.MinimumLevel = LogEventLevel.Warning;
            SS3DNameSpaces = GetAllNameOfSS3DNameSpace();   
        }

        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            // Add enricher and configure the global logging level.
            var configuration = new LoggerConfiguration()
                                .Enrich.With(new ClientIdEnricher())
                                .MinimumLevel.Information();

            // Apply some override on the minimum logging level for some namespaces.
            // Does not apply override if the logging level corresponds to the global minimum level.
            foreach(var name in SS3DNameSpaces)
            {
                
                configuration = configuration.MinimumLevel.Override(name, LogEventLevel.Error);
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

        /// <summary>
        /// Get the name of each namespaces containing SS3D.
        /// </summary>
        /// <returns> A list of those names.</returns>
        private static List<string> GetAllNameOfSS3DNameSpace()
        {
            var Assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var SS3DNameSpaces = new List<string>();
            foreach (var assembly in Assemblies)
            {
                var namespaces = assembly.GetTypes()
                                .Select(t => t.Namespace)
                                .Distinct();


                foreach (var type in namespaces)
                {
                    if (type == null || !type.Contains("SS3D"))
                    {
                        continue;
                    }
                    SS3DNameSpaces.Add(type);
                }
            }

            return SS3DNameSpaces;
        }
    }

}

