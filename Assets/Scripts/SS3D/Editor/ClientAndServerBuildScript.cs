#if UNITY_EDITOR
using UnityEditor;

namespace SS3D.Editor
{
    /// <summary>
    /// One-click / one-Editor-session build of both Linux binaries the multiplayer harness needs
    /// (<see cref="ServerBuildScript"/> then <see cref="ClientBuildScript"/>).
    /// Prefer this over two separate game-ci <c>unity-builder</c> steps: restarting the Editor
    /// between UNITY_SERVER and Player builds races PackageCache (missing localization sources /
    /// <c>Unity.Cecil.Awesome.dll</c>) on self-hosted Docker runners.
    /// </summary>
    public static class ClientAndServerBuildScript
    {
        private const string ServerBuildPath = "Builds/GameServer/SS3D.x86_64";
        private const string ClientBuildPath = "Builds/Game/SS3D.x86_64";

        /// <summary>
        /// Paths matching <c>multiplayer-smoke-test.yml</c> / game-ci
        /// <c>buildsPath</c>+<c>buildName</c> for StandaloneLinux64 (no <c>.x86_64</c> suffix).
        /// </summary>
        private const string CiServerBuildPath = "build/GameServer/StandaloneLinux64/SS3D-Server";
        private const string CiClientBuildPath = "build/Game/StandaloneLinux64/SS3D-Client";

        [MenuItem("SS3D/Build/Client + Dedicated Server (Linux)")]
        public static void BuildBothFromMenu()
        {
            ServerBuildScript.BuildServer(ServerBuildPath);
            ClientBuildScript.BuildClient(ClientBuildPath);
            EditorUtility.RevealInFinder("Builds/");
        }

        /// <summary>
        /// Batchmode entry point for <c>Tools/build_client_and_server.sh</c> /
        /// <c>-executeMethod SS3D.Editor.ClientAndServerBuildScript.BuildBothBatch</c>.
        /// Uses the same default paths as the menu item (no <c>-customBuildPath</c>).
        /// </summary>
        public static void BuildBothBatch()
        {
            ServerBuildScript.BuildServer(ServerBuildPath);
            ClientBuildScript.BuildClient(ClientBuildPath);
        }

        /// <summary>
        /// game-ci entry point (<c>buildMethod: SS3D.Editor.ClientAndServerBuildScript.BuildBothForCi</c>).
        /// Writes both CI binaries in one Editor session; ignores <c>-customBuildPath</c> (game-ci
        /// only supplies one path — configure the builder step's primary output as the client so
        /// game-ci's existence check still passes).
        /// </summary>
        public static void BuildBothForCi()
        {
            ServerBuildScript.BuildServer(CiServerBuildPath);
            ClientBuildScript.BuildClient(CiClientBuildPath);
        }
    }
}
#endif
