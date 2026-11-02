using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace SS3D.Launcher
{
    /// <summary>
    /// Starts the release updater shipped next to the game (<c>Update_SS3D.ps1</c> /
    /// <c>update_ss3d.sh</c>, one folder above <c>Game/</c>). The updater waits for this process to
    /// exit, installs, then relaunches the game; the caller quits right after <see cref="TryStart"/>.
    /// </summary>
    public static class UpdaterLauncher
    {
        /// <summary>The release root (holds <c>Game/</c> and the updater), or null outside an extracted release.</summary>
        public static string InstallRoot
        {
            get
            {
                // <root>/Game/<Product>_Data
                DirectoryInfo game = Directory.GetParent(UnityEngine.Application.dataPath);
                return game?.Parent?.FullName;
            }
        }

        public static string UpdaterPath
        {
            get
            {
                string root = InstallRoot;
                if (UnityEngine.Application.isEditor || root == null)
                {
                    return null;
                }

#if UNITY_STANDALONE_WIN
                string path = Path.Combine(root, "Update_SS3D.ps1");
#elif UNITY_STANDALONE_LINUX
                string path = Path.Combine(root, "update_ss3d.sh");
#else
                string path = null;
#endif
                return path != null && File.Exists(path) ? path : null;
            }
        }

        public static bool CanSelfUpdate => UpdaterPath != null;

        public static bool TryStart(out string error)
        {
            error = "";
            string updater = UpdaterPath;
            if (updater == null)
            {
                error = "The updater isn't next to this game. Download the new version instead.";
                return false;
            }

            int pid = Process.GetCurrentProcess().Id;
            try
            {
#if UNITY_STANDALONE_WIN
                // A visible console shows download progress; it pauses on failure before restarting.
                Process.Start(new ProcessStartInfo("powershell.exe",
                    $"-NoProfile -ExecutionPolicy Bypass -File \"{updater}\" -WaitPid {pid} -Restart")
                {
                    UseShellExecute = true,
                    WorkingDirectory = InstallRoot,
                });
#else
                // No portable terminal on Linux: run detached and log next to the installation.
                string log = Path.Combine(InstallRoot, "update.log");
                Process.Start(new ProcessStartInfo("/bin/sh",
                    $"-c 'nohup \"$0\" --wait-pid {pid} --restart > \"$1\" 2>&1 &' \"{updater}\" \"{log}\"")
                {
                    UseShellExecute = false,
                    WorkingDirectory = InstallRoot,
                });
#endif
                return true;
            }
            catch (System.Exception exception) when (exception is System.ComponentModel.Win32Exception or IOException)
            {
                Debug.LogWarning($"[MainMenu] Could not start the updater: {exception.Message}");
                error = "Couldn't start the updater. Run it from the game folder instead.";
                return false;
            }
        }
    }
}
