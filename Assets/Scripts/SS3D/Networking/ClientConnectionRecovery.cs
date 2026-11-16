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
    /// arms Empty as FishNet offline after the first successful connect, and reports a dropped
    /// session through <see cref="ConnectionLost"/>, which sends the client to the main menu. Intro
    /// failure UI stays on <see cref="ServerConnectionView"/>; the main menu owns its own
    /// (<see cref="FailureUiOwnedElsewhere"/>).
    /// </summary>
    public sealed class ClientConnectionRecovery : MonoBehaviour
    {
        private bool _reachedOnline;

        public SessionState State { get; private set; } = SessionState.Cold;

        public static ClientConnectionRecovery Instance { get; private set; }

        /// <summary>
        /// The session ended with no screen left to say so. <see cref="ConnectionNotices"/> holds the
        /// reason, if the server gave one. Not raised on a dedicated server.
        /// </summary>
        public static event Action ConnectionLost;

        /// <summary>
        /// Set while a stopped client needs no <see cref="ConnectionLost"/>: the main menu's dialog is up
        /// and shows the failure itself, the player chose to leave, or a PlayMode test ends the session.
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
            ConnectionNotices.Clear();
            SetState(SessionState.Connecting);
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
            // those scenes are gone, so the main menu takes over. Intro can still be loaded here
            // yet about to go: a server rejection (version mismatch) stops the client after the
            // transport Started armed Empty, and DefaultScene swaps Intro for Empty right after
            // this event.
            if (FailureUiOwnedElsewhere || (IsConnectionUiSceneLoaded() && !WillLoadEmptyOfflineScene()))
            {
                return;
            }

            Log.Information(this, "{status}", Logs.Important, ConnectionNotices.LastReason);
            ConnectionLost?.Invoke();
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
    }
}
