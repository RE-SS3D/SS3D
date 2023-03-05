using FishNet;
using FishNet.Object;
using Serilog.Core;
using Serilog.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Logging
{
    /// <summary>
    /// Enrich log messages by adding a ID property.
    /// The ID property takes value clientId when the log is called by a client only,
    /// Host when it's the host, Server when it's server only.
    /// </summary>
    class ClientIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
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