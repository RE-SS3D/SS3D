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
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "ClientId", InstanceFinder.ClientManager.Connection.ClientId));
        }
    }
}