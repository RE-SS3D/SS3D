using System;
using Coimbra;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Server;
using SS3D.Core.Events;
using SS3D.Core.Settings;
using SS3D.Logging;

namespace SS3D.Core
{
    /// <summary>
    /// Helps the NetworkManager to understand what we should do in this instance,
    /// if we are a server, or a client, and process respective data.
    /// </summary>
    public sealed class SessionNetworkSubsystem : Behaviours.Subsystem
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
            ushort port = Convert.ToUInt16(applicationSettings.ServerPort);

            NetworkType networkType = applicationSettings.NetworkType;

            switch (networkType)
            {
                case NetworkType.ServerOnly:
                    Punpun.Information(this, "Hosting a new headless server on port {port}", Logs.Important, port);
                    networkManager.ServerManager.StartConnection(port);
                    break;
                case NetworkType.Client:
                    Punpun.Information(this, "Joining server {serverAddress}:{port} as {ckey}", Logs.Important, serverAddress, port, ckey);
                    networkManager.ClientManager.StartConnection(serverAddress, port);
                    break;
                case NetworkType.Host:
                    Punpun.Information(this, "Hosting a new server on port {port}", Logs.Important, port);
                    networkManager.ServerManager.StartConnection(port);
                    networkManager.ClientManager.StartConnection();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            ApplicationStartedEvent applicationStartedEvent = new(ckey, networkType);
            applicationStartedEvent.Invoke(this);
        }

        public void OnDestroy()
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;
            networkManager.ServerManager.StopConnection(true);
        }
    }
}