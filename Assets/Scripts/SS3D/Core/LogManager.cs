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
               .MinimumLevel.Verbose()
               .WriteTo.File( new CompactJsonFormatter()
               , "C:/Users/Nat/Documents/GitHub/StilnatSS3DMain/Logs/LogClient" + ClientManager.Connection.ClientId + ".json")
               .WriteTo.Unity3D(formatter: new SS3DUnityTextFormatter(outputTemplate: "{SourceContext} {Message:lj}{NewLine}{Exception}"))
               .CreateLogger();
               Log.Information("##########  CLIENT STARTING !  ##########");
            Log.ForContext<LogManager>().Information("Entering MethodName");
            Log.Information("Entering MethodName");
            
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (IsServerOnly)
            {
                  Log.Logger = new LoggerConfiguration()
                 .Enrich.With(new ClientIdEnricher())
                 .MinimumLevel.Verbose()
                 .WriteTo.Unity3D(formatter: new SS3DUnityTextFormatter(outputTemplate: "{SourceContext} {Message:lj}{NewLine}{Exception}"))
                 .WriteTo.File(new CompactJsonFormatter()
                 , "C:/Users/Nat/Documents/GitHub/StilnatSS3DMain/Logs/LogServer.json")
                 .CreateLogger()
                 ;
            }
            else
            {
            
                   Log.Logger = new LoggerConfiguration()
                  .Enrich.With(new ClientIdEnricher())
                  .MinimumLevel.Verbose()
                  .WriteTo.Unity3D(formatter : new SS3DUnityTextFormatter(outputTemplate: "{SourceContext} {Message:lj}{NewLine}{Exception}"))
                  .WriteTo.File(new CompactJsonFormatter()
                  , "C:/Users/Nat/Documents/GitHub/StilnatSS3DMain/Logs/LogHost.json")
                  .CreateLogger();
            }

            Punpun.Verbose(this, "punpun wrapper test with log", Logs.ServerOnly);
            Punpun.Verbose(this, "punpun wrapper test with log, number 1 : {n}, number 2 : {m}, bool 3 : {j}", Logs.ServerOnly, 4, 25,true);
            Punpun.Verbose(Log.Logger, "punpun wrapper test with logger, number 1 : {n}, number 2 : {m}, bool 3 : {j}", Logs.Important, 4, 25, true);
            //Log.Information("##########  SERVER STARTING !  ##########");
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

