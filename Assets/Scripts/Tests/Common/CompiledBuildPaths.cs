using System.IO;
using UnityEngine;

namespace SS3D.Tests
{
    /// <summary>
    /// Resolves paths to the compiled SS3D player used by PlayMode tests.
    /// </summary>
    public static class CompiledBuildPaths
    {
        public const string MissingBuildMessage =
            "Compiled SS3D player build not found. Run SS3D → Build → Client (Linux) " +
            "(or Client + Dedicated Server) so Builds/Game has an executable before tests that launch external processes.";

        public static bool HasCompiledBuild => TryResolveExecutablePath(out _);

        public static string GetExecutableFileName()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "SS3D.exe";
                default:
                    return "SS3D";
            }
        }

        public static string GetBuildDirectory()
        {
            string filePath = Application.dataPath;
            filePath = filePath.Substring(0, filePath.Length - 6);
            filePath += "/Builds";

            const string ciFolder = "StandaloneLinux64";

            if (Directory.Exists($"{filePath}/{ciFolder}"))
            {
                filePath += $"/{ciFolder}";
            }
            else
            {
                filePath += "/Game";
            }

            return filePath;
        }

        public static bool TryResolveExecutablePath(out string executablePath)
        {
            string buildDirectory = GetBuildDirectory();
            string[] candidates =
            {
                GetExecutableFileName(),
                "SS3D",
                "SS3D.exe",
                "SS3D.x86_64",
            };

            foreach (string candidate in candidates)
            {
                string path = Path.Combine(buildDirectory, candidate);
                if (File.Exists(path))
                {
                    executablePath = path;
                    return true;
                }
            }

            executablePath = null;
            return false;
        }
    }
}
