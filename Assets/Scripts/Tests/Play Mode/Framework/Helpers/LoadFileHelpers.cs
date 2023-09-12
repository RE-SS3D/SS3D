using System.Diagnostics;
using UnityEngine;
using SS3D.Core.Settings;
using SS3D.Core.Utils;
using System.Runtime.InteropServices;
using System;
using System.IO;

namespace SS3D.Tests
{
    /// <summary>
    /// This class manages all actions relating to opening and closing the built executable automatically.
    /// </summary>
    public static class LoadFileHelpers
    {
        public const string ExecutableName = "SS3D.exe";
        public const string IpAddress = "127.0.0.1";
        public const string Port = "1151";
        public const int MaxExpectedServerLoadTimeMillis = 10000;

        /// <summary>
        /// Runs the compiled SS3D executable with the selected settings.
        /// </summary>
        /// <param name="networkType">Server (default), Host or Client.</param>
        /// <param name="Ckey">Ckey. Applicable only for Client network type, but must not be empty if client</param>
        /// <param name="windowStyle">Whether minimized (default), maximized, normal or hidden</param>
        /// <returns>The process handle for the running SS3D build</returns>
        public static Process OpenCompiledBuild(NetworkType networkType = NetworkType.DedicatedServer, string Ckey = "client", ProcessWindowStyle windowStyle = ProcessWindowStyle.Minimized)
        {
            // Confirm all arguments
            string arguments = $"{CommandLineArgs.Ip}{IpAddress} {CommandLineArgs.Port}{Port} {CommandLineArgs.SkipIntro} ";
            switch (networkType)
            {
                case NetworkType.DedicatedServer: arguments += CommandLineArgs.ServerOnly; break;
                case NetworkType.Host: arguments += CommandLineArgs.Host; break;
                case NetworkType.Client: arguments += CommandLineArgs.Ckey + Ckey; break;
            }

            // Create a process that will be able to launch the build.
            Process process = new Process();
            process.StartInfo.WindowStyle = windowStyle;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.FileName = ExecutableName;
            process.StartInfo.WorkingDirectory = GetFilePath();

            UnityEngine.Debug.Log($"Attempting to load {ExecutableName} {arguments}" );

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

        public static void PlaceQuadWindow(Process process, int windowNumber = 0)
        {
            const int ScreenWidth = 2000;
            const int MaxWindowsPerRow = 4;

            int row = windowNumber / MaxWindowsPerRow;
            int col = windowNumber % MaxWindowsPerRow;
            int windowWidth = ScreenWidth / MaxWindowsPerRow;

            SetWindowPos(process.MainWindowHandle, new IntPtr((int)SpecialWindowHandles.HWND_TOP), col * windowWidth, row * windowWidth, windowWidth, windowWidth, SetWindowPosFlags.SWP_SHOWWINDOW);
        }


        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);


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

            const string expectedFolderNameForContinuousIntegrationTesting = "StandaloneLinux64";

            if (Directory.Exists($"{filePath}/{expectedFolderNameForContinuousIntegrationTesting}"))
            {
                filePath += $"/{expectedFolderNameForContinuousIntegrationTesting}";
            }
            else
            {
                filePath += "/Game";
            }
            return filePath;
        }
        #endregion

        #region Random Enums for managing loaded application windows
        public enum SpecialWindowHandles : int
        {
            HWND_TOP = 0,
            HWND_BOTTOM = 1,
            HWND_TOPMOST = -1,
            HWND_NOTOPMOST = -2
        }

        [Flags]
        public enum SetWindowPosFlags : uint
        {
            SWP_ASYNCWINDOWPOS = 0x4000,

            SWP_DEFERERASE = 0x2000,

            SWP_DRAWFRAME = 0x0020,

            SWP_FRAMECHANGED = 0x0020,

            SWP_HIDEWINDOW = 0x0080,

            SWP_NOACTIVATE = 0x0010,

            SWP_NOCOPYBITS = 0x0100,

            SWP_NOMOVE = 0x0002,

            SWP_NOOWNERZORDER = 0x0200,

            SWP_NOREDRAW = 0x0008,

            SWP_NOREPOSITION = 0x0200,

            SWP_NOSENDCHANGING = 0x0400,

            SWP_NOSIZE = 0x0001,

            SWP_NOZORDER = 0x0004,

            SWP_SHOWWINDOW = 0x0040,
        }
        #endregion
    }
}