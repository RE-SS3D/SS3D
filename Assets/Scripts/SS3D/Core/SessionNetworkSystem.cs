using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using FishNet;
using FishNet.Managing;
using SS3D.Core.Events;
using SS3D.Core.Settings;
using SS3D.Core.Utils;
using SS3D.Logging;
using UnityEngine;
using Actor = SS3D.Core.Behaviours.Actor;

namespace SS3D.Core
{
    /// <summary>
    /// Helps the NetworkManager to understand what we should do in this instance,
    /// if we are a server, or a client, and process respective data.
    ///
    /// TODO: Could use a refactor
    /// </summary>
    public sealed class SessionNetworkSystem : Behaviours.System
    {
        /// <summary>
        /// Uses the processed args to proceed with game network initialization
        /// </summary>
        public void InitializeNetworkSession()
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;
            if (networkManager == null)
            {
                networkManager = InstanceFinder.NetworkManager;
                InitializeNetworkSession();
            }

            ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

            string ckey = applicationSettings.Ckey;
            string serverAddress = applicationSettings.ServerAddress;
            NetworkType networkType = applicationSettings.NetworkType;

            switch (networkType)
            {
                case NetworkType.ServerOnly:
                    Debug.Log($"[{nameof(SessionNetworkSystem)}] - Hosting a new headless server");
                    networkManager.ServerManager.StartConnection();
                    break;
                case NetworkType.Client:

                    Punpun.Say(this, $"Joining server {serverAddress} as {ckey}", Logs.Important);
                    networkManager.ClientManager.StartConnection(serverAddress);
                    break;
                case NetworkType.Host:
                    Debug.Log($"[{nameof(SessionNetworkSystem)}] - Hosting a new server");
                    networkManager.ServerManager.StartConnection();
                    networkManager.ClientManager.StartConnection();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ApplicationStartedEvent applicationStartedEvent = new(ckey, networkType);
            applicationStartedEvent.Invoke(this);
        }
    }
}