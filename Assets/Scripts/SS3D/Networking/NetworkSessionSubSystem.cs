using Coimbra;
using Coimbra.Services.Events;
using FishNet;
using FishNet.Managing;
using SS3D.Application.Events;
using SS3D.Core.Behaviours;
using SS3D.Core.Settings;
using SS3D.Logging;
using SS3D.Networking.Settings;
using System;

namespace SS3D.Networking
{
    /// <summary>
    /// Helps the NetworkManager to understand what we should do in this instance,
    /// if we are a server, or a client, and process respective data.
    /// </summary>
    public sealed class NetworkSessionSubSystem : SubSystem
    {
        public NetworkType NetworkType;

        public string ServerAddress;
        public ushort Port;

        protected override void OnAwake()
        {
            base.OnAwake();

            ApplicationPreInitializing.AddListener(HandleApplicationPreInitializing);
        }

        private void HandleApplicationPreInitializing(ref EventContext context, in ApplicationPreInitializing applicationInitializing)
        {
            NetworkSettings networkSettings = ScriptableSettings.GetOrFind<NetworkSettings>();

            string path = String.Empty;

            switch (networkSettings.NetworkType)
            {
                case NetworkType.DedicatedServer:
                    path = "LogServer.json"; 
                    break;
                case NetworkType.Client:
                    path = "LogClient" + networkSettings.Ckey + ".json";
                    break;
                case NetworkType.Host:
                    path = "LogHost.json";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            LogManager.Initialize(path);
        }

        /// <summary>
        /// Uses the processed args to proceed with game network initialization
        /// </summary>
        public void  StartNetworkSession()
        {
            Log.Information(this, "Initializing network session", Logs.Important);

            NetworkManager networkManager = InstanceFinder.NetworkManager;
            NetworkSettings networkSettings = ScriptableSettings.GetOrFind<NetworkSettings>();

            LocalPlayer.UpdateCkey(networkSettings.Ckey);

            string ckey = networkSettings.Ckey;
            ServerAddress  = networkSettings.ServerAddress;
            Port = Convert.ToUInt16(networkSettings.ServerPort);

            NetworkType = networkSettings.NetworkType;

            switch (NetworkType)
            {
                case NetworkType.DedicatedServer:
                    Log.Information(this, "Hosting a new headless server on port {port}", Logs.Important, Port);
                    networkManager.ServerManager.StartConnection(Port);
                    break;
                case NetworkType.Client:
                    Log.Information(this, "Joining server {serverAddress}:{port} as {ckey}", Logs.Important, ServerAddress, Port, ckey);
                    networkManager.ClientManager.StartConnection(ServerAddress, Port);
                    break;
                case NetworkType.Host:
                    Log.Information(this, "Hosting a new server on port {port}", Logs.Important, Port);
                    networkManager.ServerManager.StartConnection(Port);
                    networkManager.ClientManager.StartConnection();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            NetworkSessionStartedEvent networkSessionStartedEvent = new(ckey, NetworkType);
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