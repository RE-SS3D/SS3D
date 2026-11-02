using System;
using Coimbra;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using SS3D.Core;
using SS3D.Core.Settings;
using SS3D.Data.Generated;
using SS3D.Logging;
using SS3D.Networking.Settings;
using UnityEngine;

namespace SS3D.Networking
{
    /// <summary>
    /// Session lifecycle owner (DDOL on NetworkManager). Tracks <see cref="SessionState"/>,
    /// arms Empty as FishNet offline after the first successful connect, and offers OnGUI
    /// Retry/Quit when Boot is gone. Intro failure UI stays on <see cref="ServerConnectionView"/>;
    /// the main menu owns its own (<see cref="FailureUiOwnedElsewhere"/>).
    /// </summary>
    public sealed class ClientConnectionRecovery : MonoBehaviour
    {
        private bool _reachedOnline;
        private bool _recoveryVisible;
        private string _status = "Disconnected from server.";
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;

        public SessionState State { get; private set; } = SessionState.Cold;

        public static ClientConnectionRecovery Instance { get; private set; }

        /// <summary>
        /// Set while another UI shows connection failures itself (the main menu's Connecting dialog
        /// returns to the server list with the reason). The OnGUI Retry/Quit box then stays hidden,
        /// including when a rejected join swaps the scenes for Empty.
        /// </summary>
        public static bool FailureUiOwnedElsewhere { get; set; }

        public static void EnsureOn(NetworkManager networkManager)
        {
            if (networkManager == null)
            {
                return;
            }

            ClientConnectionRecovery existing = networkManager.GetComponent<ClientConnectionRecovery>();
            if (existing != null)
            {
                Instance = existing;
                return;
            }

            Instance = networkManager.gameObject.AddComponent<ClientConnectionRecovery>();
        }

        /// <summary>
        /// Whether a new join/host attempt may call StartConnection.
        /// </summary>
        public bool CanStart
        {
            get
            {
                if (State is SessionState.Connecting or SessionState.Online or SessionState.Disconnecting)
                {
                    return false;
                }

                NetworkManager networkManager = InstanceFinder.NetworkManager;
                if (networkManager == null)
                {
                    return false;
                }

                NetworkSettings settings = ScriptableSettings.GetOrFind<NetworkSettings>();
                if (settings.NetworkType is not (NetworkType.Client or NetworkType.Host))
                {
                    return true;
                }

                LocalConnectionState clientState = networkManager.TransportManager.Transport.GetConnectionState(false);
                return clientState is not (LocalConnectionState.Starting
                    or LocalConnectionState.Started
                    or LocalConnectionState.Stopping);
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            if (InstanceFinder.ClientManager != null)
            {
                InstanceFinder.ClientManager.OnClientConnectionState += HandleClientConnectionState;
            }

            if (InstanceFinder.ServerManager != null)
            {
                InstanceFinder.ServerManager.OnServerConnectionState += HandleServerConnectionState;
            }
        }

        private void OnDisable()
        {
            if (InstanceFinder.ClientManager != null)
            {
                InstanceFinder.ClientManager.OnClientConnectionState -= HandleClientConnectionState;
            }

            if (InstanceFinder.ServerManager != null)
            {
                InstanceFinder.ServerManager.OnServerConnectionState -= HandleServerConnectionState;
            }
        }

        /// <summary>
        /// Called from <see cref="NetworkSessionSubSystem"/> when a join/host attempt begins.
        /// </summary>
        public void NotifySessionStartAttempted()
        {
            SetState(SessionState.Connecting);
            _recoveryVisible = false;
        }

        /// <summary>
        /// Called when <c>StartConnection</c> returns false before any transport Started event.
        /// </summary>
        public void NotifySessionStartFailed()
        {
            if (State == SessionState.Connecting)
            {
                SetState(SessionState.WaitingForServer);
            }
        }

        private void HandleServerConnectionState(ServerConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case LocalConnectionState.Started:
                    EnterOnline();
                    break;
                case LocalConnectionState.Stopping when State == SessionState.Online:
                    SetState(SessionState.Disconnecting);
                    break;
                case LocalConnectionState.Stopped when State is SessionState.Online or SessionState.Disconnecting or SessionState.Connecting:
                    // Host/dedicated: server stop alone does not drive client WaitingForServer UI.
                    if (!_reachedOnline)
                    {
                        SetState(SessionState.WaitingForServer);
                    }
                    else if (ScriptableSettings.GetOrFind<NetworkSettings>().NetworkType == NetworkType.DedicatedServer)
                    {
                        // No client connection will finish this disconnect; without this a stopped
                        // dedicated server stays Disconnecting and CanStart refuses the next session.
                        SetState(SessionState.Cold);
                    }

                    break;
            }
        }

        private void HandleClientConnectionState(ClientConnectionStateArgs args)
        {
            switch (args.ConnectionState)
            {
                case LocalConnectionState.Started:
                    EnterOnline();
                    _recoveryVisible = false;
                    return;

                case LocalConnectionState.Stopping when State == SessionState.Online:
                    SetState(SessionState.Disconnecting);
                    return;

                case LocalConnectionState.Stopped:
                    if (State is not (SessionState.Connecting or SessionState.Online or SessionState.Disconnecting))
                    {
                        return;
                    }

                    EnterWaitingForServer();
                    return;
            }
        }

        private void EnterWaitingForServer()
        {
            SetState(SessionState.WaitingForServer);
            SubSystems.SetSuppressMissingErrors(true);

            NetworkSettings settings = ScriptableSettings.GetOrFind<NetworkSettings>();
            if (settings.NetworkType == NetworkType.DedicatedServer)
            {
                return;
            }

            // Intro/Boot still loaded: ServerConnectionView owns retry. After Empty offline,
            // those scenes are gone — show OnGUI even though NetworkSession is DDOL now.
            // Intro can still be loaded here yet about to go: a server rejection (version
            // mismatch) stops the client after the transport Started armed Empty, and
            // DefaultScene swaps Intro for Empty right after this event. Then OnGUI owns it.
            if (FailureUiOwnedElsewhere || (IsConnectionUiSceneLoaded() && !WillLoadEmptyOfflineScene()))
            {
                return;
            }

            _status = ProtocolVersionAuthenticator.LastRejection ?? "Could not connect, or connection to the server was lost.";
            _recoveryVisible = true;
            Log.Information(this, "{status}", Logs.Important, _status);
        }

        private static bool IsConnectionUiSceneLoaded()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                string name = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name;
                if (name == Scenes.Intro || name == Scenes.Boot || name == Scenes.Launcher)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether FishNet's <see cref="DefaultScene"/> will replace the loaded scenes with Empty
        /// on this client stop (it skips the offline load while the local server runs).
        /// </summary>
        private bool WillLoadEmptyOfflineScene()
        {
            DefaultScene defaultScene = GetComponent<DefaultScene>();
            return defaultScene != null
                && defaultScene.GetOfflineScene() == Scenes.EmptyPath
                && !InstanceFinder.IsServer;
        }

        private void EnterOnline()
        {
            _reachedOnline = true;
            SubSystems.SetSuppressMissingErrors(false);
            ArmEmptyOfflineScene();
            SetState(SessionState.Online);
            if (InstanceFinder.IsServer)
            {
                NetworkSystemsHub.EnsureSpawned();
            }
        }

        private void SetState(SessionState next)
        {
            if (State == next)
            {
                return;
            }

            Log.Debug(this, "SessionState {from} → {to}", Logs.Important, State, next);
            State = next;

            // Hub NetworkObjects despawn while Stopping/Disconnecting, before WaitingForServer.
            // Missing Get lookups from device OnDestroy are expected then — same class as teardown.
            if (next == SessionState.Disconnecting)
            {
                SubSystems.SetSuppressMissingErrors(true);
            }
        }

        /// <summary>
        /// Makes FishNet load Empty, not Boot, when the client stops. Done after the first connect,
        /// and by the main menu before it starts one: a failed join there would otherwise reload
        /// Boot over the Launcher scene (duplicate NetworkManager, re-run Boot objects).
        /// </summary>
        public void ArmEmptyOfflineScene()
        {
            DefaultScene defaultScene = GetComponent<DefaultScene>();
            if (defaultScene == null)
            {
                return;
            }

            if (defaultScene.GetOfflineScene() == Scenes.EmptyPath)
            {
                return;
            }

            defaultScene.SetOfflineScene(Scenes.EmptyPath);
            Log.Debug(this, "Armed Empty offline scene for disconnect (avoid Boot/Intro reload storm)", Logs.Important);
        }

        private void OnGUI()
        {
            if (!_recoveryVisible)
            {
                return;
            }

            EnsureGuiStyles();

            const float width = 420f;
            const float height = 140f;
            Rect area = new(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);

            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label(_status, _labelStyle);
            GUILayout.Space(12f);

            if (GUILayout.Button("Retry connection", _buttonStyle, GUILayout.Height(32f)))
            {
                Retry();
            }

            if (GUILayout.Button("Quit", _buttonStyle, GUILayout.Height(28f)))
            {
                UnityEngine.Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// Player IMGUI default (LegacyRuntime) is missing under Wine and some stripped
        /// players — empty Retry/Quit labels plus per-frame font warnings. Prefer OS fonts
        /// that exist on the current platform (Segoe UI is Windows-only).
        /// </summary>
        private void EnsureGuiStyles()
        {
            if (_labelStyle != null)
            {
                return;
            }

            Font font = ResolveImguiFont();
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = 14,
                wordWrap = true,
            };
            _labelStyle.normal.textColor = Color.white;
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = font,
                fontSize = 14,
            };
        }

        private static Font ResolveImguiFont()
        {
            // Builtin LegacyRuntime often exists as an asset but fails to resolve a face in
            // Wine / some players ("Unable to load font face for [LegacyRuntime]").
            // CreateDynamicFontFromOSFont(string[]) still binds the first name even when that
            // face is missing (Segoe UI on Linux → blank buttons + per-frame spam). Pick a
            // family that is actually installed.
            string[] preferred = PreferredOsFontFamilies();
            string[] installed = Font.GetOSInstalledFontNames();
            if (installed != null && installed.Length > 0)
            {
                for (int i = 0; i < preferred.Length; i++)
                {
                    string match = FindInstalledFontFamily(preferred[i], installed);
                    if (match == null)
                        continue;

                    Font font = Font.CreateDynamicFontFromOSFont(match, 14);
                    if (font != null)
                        return font;
                }
            }

            Font fallback = Font.CreateDynamicFontFromOSFont(preferred, 14);
            if (fallback != null)
                return fallback;

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static string[] PreferredOsFontFamilies()
        {
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            return new[]
            {
                "DejaVu Sans",
                "Liberation Sans",
                "Noto Sans",
                "Inter",
                "FreeSans",
                "Ubuntu",
                "Cantarell",
                "Arial",
            };
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return new[] { "Helvetica", "Helvetica Neue", "Arial", "Lucida Grande" };
#else
            return new[] { "Segoe UI", "Arial", "Tahoma", "DejaVu Sans", "Liberation Sans" };
#endif
        }

        private static string FindInstalledFontFamily(string preferred, string[] installed)
        {
            for (int i = 0; i < installed.Length; i++)
            {
                if (string.Equals(installed[i], preferred, StringComparison.OrdinalIgnoreCase))
                    return installed[i];
            }

            // Some distros report "DejaVu Sans Book" etc.
            for (int i = 0; i < installed.Length; i++)
            {
                if (installed[i].StartsWith(preferred, StringComparison.OrdinalIgnoreCase))
                    return installed[i];
            }

            return null;
        }

        private void Retry()
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;
            if (networkManager == null)
            {
                return;
            }

            if (!CanStart)
            {
                _status = "Connection still settling — wait a moment, then retry.";
                return;
            }

            _recoveryVisible = false;

            if (!SubSystems.TryGet(out NetworkSessionSubSystem session))
            {
                // Boot was unloaded (offline Empty). Start from NetworkSettings directly.
                NetworkSettings settings = ScriptableSettings.GetOrFind<NetworkSettings>();
                string address = string.IsNullOrWhiteSpace(settings.ServerAddress)
                    ? "127.0.0.1"
                    : settings.ServerAddress;
                LocalPlayer.UpdateCkey(settings.Ckey);
                NotifySessionStartAttempted();
                bool started = networkManager.ClientManager.StartConnection(
                    address,
                    settings.ServerPort);
                if (!started)
                {
                    _status = "Failed to start client connection.";
                    _recoveryVisible = true;
                    SetState(SessionState.WaitingForServer);
                }

                return;
            }

            session.StartNetworkSession();
        }
    }
}
