using System;
using Coimbra;
using FishNet;
using FishNet.Managing;
using SS3D.Core.Events;
using SS3D.Core.Settings;
using UnityEngine;

namespace SS3D.Core
{
    /// <summary>
    /// Helps the NetworkManager to understand what we should do in this instance,
    /// if we are a server, or a client, and process respective data.
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
                    Punpun.Say(this, "Hosting a new headless server");
                    networkManager.ServerManager.StartConnection();
                    break;
                case NetworkType.Client:
                    Punpun.Say(this, $"Joining server {serverAddress} as {ckey}", Logs.Important);
                    networkManager.ClientManager.StartConnection(serverAddress);
                    break;
                case NetworkType.Host:
                    Punpun.Say(this, "Hosting a new server");
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