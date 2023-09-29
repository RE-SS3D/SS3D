using System;
using Coimbra;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Server;
using SS3D.Core.Events;
using SS3D.Core.Settings;
using SS3D.Logging;
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
            NetworkSettings networkSettings = ScriptableSettings.GetOrFind<NetworkSettings>();

            LocalPlayer.UpdateCkey(networkSettings.Ckey);

            string ckey = networkSettings.Ckey;
            string serverAddress = networkSettings.ServerAddress;
            ushort port = Convert.ToUInt16(networkSettings.ServerPort);

            NetworkType networkType = networkSettings.NetworkType;

            switch (networkType)
            {
                case NetworkType.DedicatedServer:
                    Log.Information(this, "Hosting a new headless server on port {port}", Logs.Important, port);
                    networkManager.ServerManager.StartConnection(port);
                    break;
                case NetworkType.Client:
                    Log.Information(this, "Joining server {serverAddress}:{port} as {ckey}", Logs.Important, serverAddress, port, ckey);
                    networkManager.ClientManager.StartConnection(serverAddress, port);
                    break;
                case NetworkType.Host:
                    Log.Information(this, "Hosting a new server on port {port}", Logs.Important, port);
                    networkManager.ServerManager.StartConnection(port);
                    networkManager.ClientManager.StartConnection();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            NetworkSessionStartedEvent networkSessionStartedEvent = new(ckey, networkType);
            networkSessionStartedEvent.Invoke(this);
        }

        private void OnApplicationQuit()
        {
            CloseNetworkSession();
            InstanceFinder.ServerManager.StopConnection(true);
        }

        /// <summary>
        ///Shuts down client and server to ensure no lingering connections
        /// </summary>
        public void CloseNetworkSession()
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;
            if (networkManager == null)
            {
                Log.Warning(this, "No NetworkManager found", Logs.Important);
                return;
            }

            Log.Information(this, "Closing network session", Logs.Important);
            networkManager.TransportManager.Transport.Shutdown();
        }
    }
}