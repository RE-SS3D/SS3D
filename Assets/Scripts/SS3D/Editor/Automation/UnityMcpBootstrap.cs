using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

namespace SS3D.Editor.Automation
{
    /// <summary>Explicit command-line entry point; does not change scenes or assets.</summary>
    public static class UnityMcpBootstrap
    {
        [InitializeOnLoadMethod]
        private static void ResumeRequestedConnection()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(),
                "SS3D.Editor.Automation.UnityMcpBootstrap.Connect") >= 0)
                EditorApplication.delayCall += Connect;
        }

        [MenuItem("SS3D/Testing/Connect Unity MCP")]
        public static async void Connect()
        {
            if (MCPServiceLocator.Bridge.IsRunning) return;
            EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
            // SS3D_MCP_URL overrides the port where 8080 is taken; keep it in sync with start-mcp.sh.
            string url = System.Environment.GetEnvironmentVariable("SS3D_MCP_URL");
            EditorPrefs.SetString("MCPForUnity.HttpUrl", string.IsNullOrEmpty(url) ? "http://127.0.0.1:8080" : url);
            EditorPrefs.SetBool("MCPForUnity.TelemetryDisabled", true);
            bool connected = await MCPServiceLocator.Bridge.StartAsync();
            Debug.Log($"[SS3D Automation] MCP connected: {connected}");
        }
    }
}
