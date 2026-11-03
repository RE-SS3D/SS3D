using System;
using System.IO;

namespace SS3D.Application
{
    /// <summary>
    /// Which Unity Multiplayer Play Mode (MPPM) player this editor process is, and which smoke
    /// script its MPPM tags ask it to run. Clones (Player2..4) are separate editor processes under
    /// <c>Library/VP/PlayerN</c>; the main editor is the host. Outside the editor, or with MPPM
    /// off, this reports the main editor with no script.
    /// </summary>
    /// <remarks>
    /// A tag <c>smoke:&lt;name&gt;</c> runs <c>Testing/multiplayer/mppm/&lt;name&gt;.txt</c>, or
    /// <c>Testing/multiplayer/scenarios/&lt;name&gt;.txt</c> when there is no MPPM-specific file.
    /// The editor driver is <c>SS3D.Editor.Automation.MultiplayerPlayModeSmoke</c>.
    /// </remarks>
    public static class MultiplayerPlayModePlayer
    {
        public const string ScriptTagPrefix = "smoke:";

        private const string VirtualProjectMarker = "/Library/VP/";

        /// <summary>True in an MPPM clone editor (Player2..4).</summary>
        public static bool IsVirtualPlayer
        {
            get
            {
#if UNITY_EDITOR
                return !Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor;
#else
                return false;
#endif
            }
        }

        /// <summary>"Main" for the main editor, otherwise the clone's MPPM name ("Player2").</summary>
        public static string Name
        {
            get
            {
                if (!IsVirtualPlayer)
                {
                    return "Main";
                }

                string[] args = Environment.GetCommandLineArgs();

                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (args[i] == "-name")
                    {
                        return args[i + 1].Trim('"');
                    }
                }

                return "Clone";
            }
        }

        /// <summary>The main project's root, also from inside a clone's virtual project.</summary>
        public static string MainProjectRoot
        {
            get
            {
                string root = Path.GetDirectoryName(UnityEngine.Application.dataPath).Replace('\\', '/');
                int marker = root.IndexOf(VirtualProjectMarker, StringComparison.Ordinal);

                return marker < 0 ? root : root.Substring(0, marker);
            }
        }

        /// <summary>Where each player writes its script result for the editor driver.</summary>
        public static string ResultDirectory => Path.Combine(MainProjectRoot, "Temp", "mppm-automation");

        /// <summary>
        /// Written by the MPPM host once its start scenes are up. The clones wait for it before
        /// connecting, the way the built smoke starts clients only after the server reports ready;
        /// connecting earlier drops Game's scene objects on the client (FishNet SceneId errors).
        /// </summary>
        public static string HostReadyPath => Path.Combine(ResultDirectory, "host-ready");

        /// <summary>The script named by this player's <c>smoke:</c> tag, or empty.</summary>
        public static string TaggedScriptPath
        {
            get
            {
#if UNITY_EDITOR
                foreach (string tag in Unity.Multiplayer.PlayMode.CurrentPlayer.Tags)
                {
                    if (!tag.StartsWith(ScriptTagPrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string name = tag.Substring(ScriptTagPrefix.Length);
                    string multiplayer = Path.Combine(MainProjectRoot, "Testing", "multiplayer");
                    string mppm = Path.Combine(multiplayer, "mppm", name + ".txt");

                    return File.Exists(mppm) ? mppm : Path.Combine(multiplayer, "scenarios", name + ".txt");
                }
#endif
                return string.Empty;
            }
        }
    }
}
