using FishNet.Managing.Server;
using FishNet.Broadcast;
using FishNet.Connection;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.InputHandling;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using SS3D.UI.Buttons;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using FishNet;
using SS3D.Core.Settings;
using SS3D.Core.Utils;

namespace SS3D.Tests
{
    /// <summary>
    /// This class manages all actions relating to opening and closing the built executable automatically.
    /// </summary>
    public static class LoadFileHelpers
    {
        public const string ExecutableName = "SS3D.exe";
        public const string LocalHost = "localhost";
        public const int MaxExpectedServerLoadTimeMillis = 10000;

        /// <summary>
        /// Runs the compiled SS3D executable with the selected settings.
        /// </summary>
        /// <param name="networkType">Server (default), Host or Client.</param>
        /// <param name="Ckey">Ckey. Applicable only for Client network type, but must not be empty if client</param>
        /// <param name="windowStyle">Whether minimized (default), maximized, normal or hidden</param>
        /// <returns>The process handle for the running SS3D build</returns>
        public static Process OpenCompiledBuild(NetworkType networkType = NetworkType.ServerOnly, string Ckey = "client", ProcessWindowStyle windowStyle = ProcessWindowStyle.Minimized)
        {
            // Confirm all arguments
            string arguments = $"{CommandLineArgs.Ip}{LocalHost} {CommandLineArgs.SkipIntro} ";
            switch (networkType)
            {
                case NetworkType.ServerOnly: arguments += CommandLineArgs.ServerOnly; break;
                case NetworkType.Host: arguments += CommandLineArgs.Host; break;
                case NetworkType.Client: arguments += CommandLineArgs.Ckey + Ckey; break;
            }

            // Create a process that will be able to launch the build.
            Process process = new Process();
            process.StartInfo.WindowStyle = windowStyle;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.FileName = ExecutableName;
            process.StartInfo.WorkingDirectory = GetFilePath();

            // Execute the process
            process.Start();

            // Enforce a delay if we are loading a server (either dedicated or host).
            // This prevents us loading our client scene before server is ready to handle us.
            if (networkType != NetworkType.Client) Sleep(MaxExpectedServerLoadTimeMillis);

            // Return the process handle so we can close it correctly later
            return process;
        }

        /// <summary>
        /// Pauses execution. For example, used so the server has time to load.
        /// </summary>
        /// <param name="durationInMilliseconds">How long to pause for (in milliseconds)</param>
        public static void Sleep(int durationInMilliseconds)
        {
            System.Threading.Thread.Sleep(durationInMilliseconds);
        }
        

        #region Private helper methods
        /// <summary>
        /// This method gets the filepath of the Builds directory where the compiled build is saved.
        /// </summary>
        /// <returns>The filepath of the Builds folder.</returns>
        private static string GetFilePath()
        {
            // Get relevant executable file path
            string filePath = Application.dataPath;
            filePath = filePath.Substring(0, filePath.Length - 6);     // Needed to remove the "Assets" folder.
            filePath += "Builds";                                      // Needed to add the "Builds" folder.
            return filePath;
        }
        #endregion


    }
}