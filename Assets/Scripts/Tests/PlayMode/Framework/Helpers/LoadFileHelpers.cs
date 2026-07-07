using NUnit.Framework;
using SS3D.CommandLine;
using SS3D.Networking;
using SS3D.Tests;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Tests.Play_Mode.Framework.Helpers
{
    /// <summary>
    /// This class manages all actions relating to opening and closing the built executable automatically.
    /// </summary>
    public static class LoadFileHelpers
    {
        public const string IpAddress = "127.0.0.1";
        public const string Port = "1151";
        public const int MaxExpectedServerLoadTimeMillis = 10000;

        public const string MissingBuildMessage = CompiledBuildPaths.MissingBuildMessage;

        public static bool HasCompiledBuild => CompiledBuildPaths.HasCompiledBuild;

        public static void RequireCompiledBuild()
        {
            if (!HasCompiledBuild)
            {
                Assert.Ignore(MissingBuildMessage);
            }
        }

        public static string GetExecutableFileName() => CompiledBuildPaths.GetExecutableFileName();

        public static string GetBuildDirectory() => CompiledBuildPaths.GetBuildDirectory();

        public static bool TryResolveExecutablePath(out string executablePath) =>
            CompiledBuildPaths.TryResolveExecutablePath(out executablePath);

        public static Process OpenCompiledBuild(NetworkType networkType = NetworkType.DedicatedServer, string Ckey = "client", ProcessWindowStyle windowStyle = ProcessWindowStyle.Minimized)
        {
            RequireCompiledBuild();

            if (!TryResolveExecutablePath(out string executablePath))
            {
                throw new FileNotFoundException(MissingBuildMessage);
            }

            string arguments = $"{CommandLineArgs.Ip}{IpAddress} {CommandLineArgs.Port}{Port} {CommandLineArgs.SkipIntro} ";
            switch (networkType)
            {
                case NetworkType.DedicatedServer: arguments += CommandLineArgs.ServerOnly; break;
                case NetworkType.Host: arguments += CommandLineArgs.Host; break;
                case NetworkType.Client: arguments += CommandLineArgs.Ckey + Ckey; break;
            }

            Process process = new Process();
            process.StartInfo.WindowStyle = windowStyle;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.FileName = executablePath;
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(executablePath);

            UnityEngine.Debug.Log($"Attempting to load {executablePath} {arguments}");

            process.Start();

            if (networkType != NetworkType.Client)
            {
                Sleep(MaxExpectedServerLoadTimeMillis);
            }

            return process;
        }

        public static void Sleep(int durationInMilliseconds)
        {
            System.Threading.Thread.Sleep(durationInMilliseconds);
        }

        public static void PlaceQuadWindow(Process process, int windowNumber = 0)
        {
            if (process == null || process.MainWindowHandle == IntPtr.Zero)
            {
                return;
            }

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
