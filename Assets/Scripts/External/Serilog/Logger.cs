using FishNet.Object;
using Serilog;
using Serilog.Sinks.Unity3D;
using Serilog.Sinks.File;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Serializing;

public class Logger : NetworkBehaviour
{

    public static Serilog.ILogger ServerLog;
    public static Serilog.ILogger ClientLog;
    public override void OnStartClient()
    {
        
        base.OnStartClient();
        int id = this.OwnerId;
        ClientLog = new LoggerConfiguration().WriteTo.Unity3D().WriteTo.File("Logs/Client" + id + ".txt").CreateLogger();
        ClientLog.Information("##########  CLIENT STARTING !  ##########");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ServerLog = new LoggerConfiguration().WriteTo.Unity3D().WriteTo.File("Logs/ServerLog.txt").CreateLogger();

        ServerLog.Information("##########  SERVER STARTING !  ##########");

    }
}
