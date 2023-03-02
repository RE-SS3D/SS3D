using FishNet.Object;
using Serilog;
using FishNet.Transporting;
using FishNet.Connection;
using Serilog.Sinks.Unity3D;
using SS3D.Core.Behaviours;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Core
{
    /// <summary>
    /// Set up Serilog's Logger for clients, host and server. 
    /// </summary>
    public class LogManager : NetworkSystem
    {

        private static LoggingLevelSwitch _levelSwitch;
        private readonly string defaultUnityLogTemplate = "{SourceContext} {Message:lj}{NewLine}{Exception}";
        private string LogFolderPath;

        protected override sealed void OnAwake()
        {
            base.OnAwake();
            LogFolderPath = Application.dataPath+"/Logs/";
            _levelSwitch = new LoggingLevelSwitch();
            _levelSwitch.MinimumLevel = LogEventLevel.Warning;
        }
        public override void OnStartClient()
        {
                base.OnStartClient();
                // Host logger is registered in OnStartServer().
                if (IsHost) return;

                Log.Logger = new LoggerConfiguration()
               .Enrich.With(new ClientIdEnricher())
               .MinimumLevel.Verbose()
               .WriteTo.File( new CompactJsonFormatter()
               , LogFolderPath + "LogClient" + ClientManager.Connection.ClientId + ".json")
               .WriteTo.Unity3D(formatter: new SS3DUnityTextFormatter(outputTemplate: defaultUnityLogTemplate))
               .CreateLogger();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (IsServerOnly)
            {
                  Log.Logger = new LoggerConfiguration()
                 .Enrich.With(new ClientIdEnricher())
                 .MinimumLevel.Verbose()
                 .WriteTo.Unity3D(formatter: new SS3DUnityTextFormatter(outputTemplate: defaultUnityLogTemplate))
                 .WriteTo.File(new CompactJsonFormatter()
                 , LogFolderPath + "LogServer.json")
                 .CreateLogger();
            }
            else
            {
            
                   Log.Logger = new LoggerConfiguration()
                  .Enrich.With(new ClientIdEnricher())
                  .MinimumLevel.Verbose()
                  .WriteTo.Unity3D(formatter : new SS3DUnityTextFormatter(outputTemplate: defaultUnityLogTemplate))
                  .WriteTo.File(new CompactJsonFormatter()
                  , LogFolderPath + "LogHost.json")
                  .CreateLogger();
            }
        }
    }

}

