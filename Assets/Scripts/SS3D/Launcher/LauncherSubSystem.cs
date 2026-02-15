using Coimbra;
using SS3D.Core.Behaviours;
using SS3D.Data.Generated;
using SS3D.Logging;
using SS3D.Networking;
using SS3D.Networking.Settings;
using SS3D.SceneManagement;
using System;

namespace SS3D.Launcher
{
    /// <summary>
    /// System used to control launcher functions.
    /// </summary>
    public sealed class LauncherSubSystem : SubSystem
    {
        /// <summary>
        /// Launches the game.
        /// </summary>
        /// <param name="networkType">The network mode used on the game.</param>
        /// <param name="ckey">The ckey used on the game.</param>
        /// <param name="ip">The IP address to connect to.</param>
        /// <param name="port">The port number to use.</param>
        public void LaunchGame(NetworkType networkType, string ckey, string ip, string port)
        {
            NetworkSettings networkSettings = ScriptableSettings.GetOrFind<NetworkSettings>();

            networkSettings.NetworkType = networkType;
            networkSettings.Ckey = ckey;
            networkSettings.ServerAddress = ip;
            networkSettings.ServerPort = Convert.ToUInt16(port);

            Log.Information(this, $"Launching game with on {networkType} for {ip} {port} as {ckey}", Logs.Important);

            Scene.LoadAsync(Scenes.Intro);
        }
    }
}