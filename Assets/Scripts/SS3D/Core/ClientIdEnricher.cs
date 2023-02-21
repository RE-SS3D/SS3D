using FishNet;
using FishNet.Object;
using Serilog.Core;
using Serilog.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Core
{
    class ClientIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.Properties.ContainsKey("sender"))
            {

            }
            if (InstanceFinder.IsServerOnly)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                   "ID", "Server"));
            }
            else if (InstanceFinder.IsHost)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ID", "Host"));
            }
            else
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "ID", InstanceFinder.ClientManager.Connection.ClientId));
            }
        }
    }
}