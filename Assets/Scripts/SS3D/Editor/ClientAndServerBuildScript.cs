#if UNITY_EDITOR
using UnityEditor;

namespace SS3D.Editor
{
    /// <summary>
    /// One-click build of both Linux binaries the multiplayer harness needs
    /// (<see cref="ServerBuildScript"/> then <see cref="ClientBuildScript"/>).
    /// </summary>
    public static class ClientAndServerBuildScript
    {
        private const string ServerBuildPath = "Builds/GameServer/SS3D.x86_64";
        private const string ClientBuildPath = "Builds/Game/SS3D.x86_64";

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
    }
}
#endif
