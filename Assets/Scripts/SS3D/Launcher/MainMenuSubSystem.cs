using System;
using System.Collections;
using System.Collections.Generic;
using Coimbra;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using SS3D.Application;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Settings;
using SS3D.Data.Generated;
using SS3D.Logging;
using SS3D.Networking;
using SS3D.Networking.Settings;
using SS3D.Networking.Status;
using SS3D.Systems.Settings;
using SS3D.UI.Shell;
using SS3D.UI.Shell.Catalog;
using SS3D.UI.Shell.MainMenu;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace SS3D.Launcher
{
    /// <summary>
    /// Client main menu. Shows <see cref="MainMenuView"/> on the shell's Modal layer whenever the
    /// Launcher scene loads (no command-line args, or <c>-forceLauncher</c> / the editor Launcher
    /// toggle), and drives join, local host, server status and the update button from it. The view
    /// is DDOL with the shell, so a rejected join that swaps the scenes for Empty keeps its dialog.
    /// A join stays on that dialog, over the loaded Game scene, until the server accepts the login;
    /// a lost session comes back to it from the Game scene with the reason.
    /// </summary>
    public sealed class MainMenuSubSystem : SubSystem
    {
        public const ushort DefaultPort = 1151;
        private const string DiscordUrl = "https://discord.gg/3ny9tdH";
        private const string ArtCreditsUrl = "https://github.com/RE-SS3D/SS3D-Art";
        private const string RebuildMenuPath = "SS3D → Data → Rebuild All UI Catalogs";
        private const int MaxNameLength = 32;

        private const int LoggingInStep = 3;

        private enum Operation { None = 0, Joining = 1, Hosting = 2, Updating = 3 }

        private readonly MainMenuPresentation _model = new();
        private readonly Dictionary<string, ServerStatusResult> _results = new();
        private readonly HashSet<string> _expanded = new();
        private MainMenuAssetCatalog _catalog;
        private MainMenuView _view;
        private MainMenuStore _store;
        private MainMenuState _state;
        private ServerStatusQuery _query;
        private bool _advanced;
        private string _portText = "";
        private string _address = "";
        private string _addressError = "";
        private string _localError = "";
        private string _latestTag;
        private bool _updateCheckStarted;
        private DateTime? _checkedAt;
        private bool _presentPending;
        private MainMenuMusic _music;
        private MainMenuVideoWallpaper _video;
        private MainMenuWallpaperRotation _rotation;
        private AudioListener _listener;
        private float _nextListenerCheck;
        private int _lastWallpaper = -1;
#if UNITY_EDITOR
        private (NetworkType Type, string Ckey, string Address, ushort Port)? _editorNetworkSettings;
#endif

        private Operation _operation;
        private ushort _operationPort;
        private MainMenuJoinAttempt _join;
        private bool _lostPending;
        private bool _subscribed;

        public bool IsVisible => _view != null;

        public MainMenuView View => _view;

        protected override void OnAwake()
        {
            base.OnAwake();
            if (UnityEngine.Application.isBatchMode || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return;
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
            ClientConnectionRecovery.ConnectionLost += HandleConnectionLost;
            ConnectionNotices.LoginAccepted += HandleLoginAccepted;
        }

        protected override void OnDestroyed()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            ClientConnectionRecovery.ConnectionLost -= HandleConnectionLost;
            ConnectionNotices.LoginAccepted -= HandleLoginAccepted;
            Hide();
#if UNITY_EDITOR
            // NetworkSettings is an asset: Play Mode writes would otherwise outlive the session.
            if (_editorNetworkSettings is { } saved)
            {
                NetworkSettings settings = ScriptableSettings.GetOrFind<NetworkSettings>();
                settings.NetworkType = saved.Type;
                settings.Ckey = saved.Ckey;
                settings.ServerAddress = saved.Address;
                settings.ServerPort = saved.Port;
            }
#endif
            base.OnDestroyed();
        }

        private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            if (scene.name == Scenes.Launcher)
            {
                Show();
            }
            else if (scene.name == Scenes.Game && IsVisible && !_model.OperationFailed)
            {
                if (_operation == Operation.Joining)
                {
                    // Not joined yet: the login can still be refused. Game brings its own listener.
                    RemoveListener();
                    SetStep(LoggingInStep);
                    return;
                }

                CompleteOperation();
                Hide();
            }
        }

        private void HandleLoginAccepted()
        {
            if (_operation == Operation.Joining)
            {
                CompleteOperation();
                Hide();
            }
        }

        /// <summary>A session dropped with no menu up: go back to it and open the dialog there.</summary>
        private void HandleConnectionLost()
        {
            if (!string.IsNullOrEmpty(ScriptableSettings.GetOrFind<ApplicationSettings>().ActiveTestScriptPath)
                || !SubSystems.TryGet(out NetworkSessionSubSystem session))
            {
                return;
            }

            if (ScriptableSettings.GetOrFind<NetworkSettings>().NetworkType == NetworkType.Client)
            {
                _lostPending = true;
            }
            else
            {
                _localError = "The local session ended unexpectedly.";
            }

            session.LeaveToMainMenu();
        }

        private void Update()
        {
            // Music keeps fading out after the menu hides (Game loading).
            bool trackChanged = _music != null && _music.Tick();
            if (_view == null)
            {
                return;
            }

            if (Time.unscaledTime >= _nextListenerCheck)
            {
                _nextListenerCheck = Time.unscaledTime + 0.5f;
                EnsureListener();
            }

            if (trackChanged)
            {
                RequestPresent();
            }

            TickWallpaper();

            _query?.Tick();
            if (_presentPending && !IsEditingServerList())
            {
                Present();
            }
        }

        public void Show()
        {
            if (_view != null)
            {
                return;
            }

            if (_catalog == null && !UiCatalogRuntimeLoader.TryLoad(MainMenuAssetPaths.ResourcesCatalogName, RebuildMenuPath, this, out _catalog))
            {
                return;
            }

            if (!SubSystems.TryGet(out UiShellSubSystem shell) || !shell.TryGetLayer(UiLayer.Modal, out VisualElement layer))
            {
                Log.Error(this, "Main menu needs the UI shell's Modal layer", Logs.Important);
                return;
            }

            _store ??= new MainMenuStore(MainMenuStore.DefaultPath);
            if (_state == null)
            {
                bool saved = _store.TryLoad(DefaultPort, out _state);
                if (!saved || string.IsNullOrWhiteSpace(_state.PlayerName))
                {
                    _state.PlayerName = DefaultPlayerName();
                }

                _portText = _state.LocalPort.ToString();
            }

            ClientConnectionRecovery.FailureUiOwnedElsewhere = true;
            // Back from a round: the game may have locked or hidden the cursor.
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            _view = new MainMenuView(_catalog);
            _view.CommandIssued += HandleCommand;
            _view.FieldChanged += HandleField;
            _view.Attach(layer);
            _model.Screen = MainMenuScreen.Menu;
            _video ??= new MainMenuVideoWallpaper(gameObject);
            ChangeWallpaper();
            // A video opens on black: its fallback still would flash up before the first frame.
            _rotation = new MainMenuWallpaperRotation(Time.unscaledTime);
            _model.WallpaperVeiled = _catalog.WallpaperVideo(_model.Wallpaper) != null;
            if (_model.WallpaperVeiled)
            {
                _rotation.Reveal(Time.unscaledTime);
            }
            _music ??= new MainMenuMusic(gameObject, _catalog.Music);
            // A fresh launcher visit starts the current track from the beginning.
            _music.Stop();
            EnsureListener();
            _query ??= new ServerStatusQuery();
            _query.Completed += HandleStatus;
            RefreshStatuses();
            StartUpdateCheck();
            if (_lostPending)
            {
                ShowConnectionLost();
            }

            Present();
        }

        public void Hide()
        {
            Unsubscribe();
            _music?.SetAudible(false);
            _video?.Stop();
            RemoveListener();
            if (_query != null)
            {
                _query.Completed -= HandleStatus;
                _query.Dispose();
                _query = null;
            }

            if (_view != null)
            {
                _view.CommandIssued -= HandleCommand;
                _view.FieldChanged -= HandleField;
                _view.Detach();
                _view = null;
            }

            ClientConnectionRecovery.FailureUiOwnedElsewhere = false;
        }

        private void HandleCommand(MainMenuCommand command, string id)
        {
            if (_operation != Operation.None && command != MainMenuCommand.Cancel)
            {
                return;
            }

            switch (command)
            {
                case MainMenuCommand.Join:
                    _model.Screen = MainMenuScreen.Servers;
                    RefreshStatuses();
                    break;
                case MainMenuCommand.PlayLocally:
                    StartHost();
                    break;
                case MainMenuCommand.ToggleAdvanced:
                    _advanced = !_advanced;
                    break;
                case MainMenuCommand.Settings:
                    _model.Screen = MainMenuScreen.Settings;
                    break;
                case MainMenuCommand.Back:
                    if (_model.OperationFailed)
                    {
                        BackToServers();
                    }
                    else
                    {
                        _model.Screen = MainMenuScreen.Menu;
                    }

                    break;
                case MainMenuCommand.Retry:
                    if (_model.OperationFailed)
                    {
                        StartJoin(_join.Host, _join.Port, _join.Token, _join.Name);
                    }

                    break;
                case MainMenuCommand.Quit:
                    Quit();
                    return;
                case MainMenuCommand.Refresh:
                    RefreshStatuses();
                    break;
                case MainMenuCommand.Discord:
                    UnityEngine.Application.OpenURL(DiscordUrl);
                    return;
                case MainMenuCommand.Credits:
                    UnityEngine.Application.OpenURL(ArtCreditsUrl);
                    return;
                case MainMenuCommand.Connect:
                    ConnectToAddress();
                    break;
                case MainMenuCommand.Update:
                case MainMenuCommand.ServerUpdate:
                    StartUpdate();
                    break;
                case MainMenuCommand.Cancel:
                    CancelOperation();
                    break;
                case MainMenuCommand.ServerJoin:
                    if (_state.Find(id) is { } server)
                    {
                        StartJoin(server.Host, (ushort)server.Port, server.AdminToken, DisplayName(server));
                    }

                    break;
                case MainMenuCommand.ServerDetails:
                    if (!_expanded.Remove(id))
                    {
                        _expanded.Add(id);
                    }

                    break;
                case MainMenuCommand.ToggleMusic:
                    _state.MusicOn = !_state.MusicOn;
                    Save();
                    break;
                case MainMenuCommand.NextTrack:
                    _music?.Next();
                    break;
                case MainMenuCommand.ServerRemove:
                    _state.Recent.RemoveAll(s => s.Id == id);
                    _expanded.Remove(id);
                    _results.Remove(id);
                    Save();
                    break;
            }

            Present();
        }

        private void HandleField(MainMenuField field, string id, string value)
        {
            switch (field)
            {
                case MainMenuField.PlayerName:
                    // Kept untrimmed while typing (re-presenting a trimmed value would eat spaces).
                    _state.PlayerName = value ?? "";
                    Save();
                    Present();
                    break;
                case MainMenuField.Port:
                    _portText = value ?? "";
                    if (MainMenuAddress.TryParsePort(_portText, out ushort port))
                    {
                        _state.LocalPort = port;
                        Save();
                    }

                    _localError = "";
                    Present();
                    break;
                case MainMenuField.MusicVolume:
                    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float volume)
                        && SubSystems.TryGet(out PlayerSettingsSubSystem playerSettings))
                    {
                        playerSettings.SetVolume(PlayerVolume.Music, volume);
                        if (volume > 0 && !_state.MusicOn)
                        {
                            _state.MusicOn = true;
                            Save();
                            Present();
                        }
                        else
                        {
                            UpdateMusic();
                        }
                    }

                    break;
                case MainMenuField.Address:
                    _address = value ?? "";
                    _addressError = "";
                    break;
                case MainMenuField.ServerName:
                    if (_state.Find(id) is { } named)
                    {
                        named.Name = (value ?? "").Trim();
                        Save();
                    }

                    break;
                case MainMenuField.OperationToken:
                    if (_join != null && _join.SetToken(_state, value))
                    {
                        Save();
                    }

                    _model.OperationToken = value ?? "";
                    break;
                case MainMenuField.AdminToken:
                    if (_state.Find(id) is { } tokened)
                    {
                        tokened.AdminToken = value ?? "";
                        Save();
                    }

                    break;
            }
        }

        private void ConnectToAddress()
        {
            if (!MainMenuAddress.TryParse(_address, DefaultPort, out string host, out ushort port, out string error))
            {
                _addressError = error;
                return;
            }

            RecentServer known = _state.Find($"{host}:{port}");
            StartJoin(host, port, known?.AdminToken ?? "", known != null ? DisplayName(known) : "Direct connect");
        }

        private void StartJoin(string host, ushort port, string token, string displayName)
        {
            if (string.IsNullOrWhiteSpace(_state.PlayerName) || !TryGetSession(out NetworkSessionSubSystem session))
            {
                return;
            }

            NetworkSettings settings = EditableNetworkSettings();
            settings.NetworkType = NetworkType.Client;
            settings.Ckey = _state.PlayerName.Trim();
            settings.ServerAddress = host;
            settings.ServerPort = port;
            LocalPlayer.UpdateAccessToken(string.IsNullOrEmpty(token) ? null : token);

            _operation = Operation.Joining;
            _join = new MainMenuJoinAttempt(host, port, displayName, token);
            _addressError = "";
            ClearFailure();
            _model.Screen = MainMenuScreen.Connecting;
            _model.OperationName = displayName;
            _model.OperationAddress = MainMenuAddress.Format(host, port);
            _model.OperationStep = 0;
            Log.Information(this, "Main menu joining {address} as {ckey}", Logs.Important, _model.OperationAddress, settings.Ckey);
            BeginSession(session);
        }

        private void StartHost()
        {
            if (string.IsNullOrWhiteSpace(_state.PlayerName) || !TryGetSession(out NetworkSessionSubSystem session))
            {
                return;
            }

            if (!MainMenuAddress.TryParsePort(_portText, out ushort port))
            {
                _localError = "The port must be a number from 1 to 65535.";
                _advanced = true;
                return;
            }

            NetworkSettings settings = EditableNetworkSettings();
            settings.NetworkType = NetworkType.Host;
            settings.Ckey = _state.PlayerName.Trim();
            settings.ServerAddress = "127.0.0.1";
            settings.ServerPort = port;
            LocalPlayer.UpdateAccessToken(null);

            _operation = Operation.Hosting;
            _operationPort = port;
            _localError = "";
            ClearFailure();
            _model.Screen = MainMenuScreen.Hosting;
            _model.OperationName = "Local session";
            _model.OperationAddress = $"Port {port}";
            _model.OperationStep = 0;
            Log.Information(this, "Main menu hosting on port {port} as {ckey}", Logs.Important, port, settings.Ckey);
            BeginSession(session);
        }

        private void BeginSession(NetworkSessionSubSystem session)
        {
            Subscribe();
            ClientConnectionRecovery.EnsureOn(InstanceFinder.NetworkManager);
            ClientConnectionRecovery.Instance.ArmEmptyOfflineScene();
            session.StartNetworkSession();
            ClientConnectionRecovery recovery = ClientConnectionRecovery.Instance;
            if (_operation != Operation.None && recovery != null && recovery.State != SessionState.Connecting)
            {
                // StartNetworkSession refused (a previous session still settling) or StartConnection failed.
                Fail(recovery.State == SessionState.WaitingForServer
                    ? "Couldn't start the connection."
                    : "The previous connection is still closing. Try again in a moment.");
            }
        }

        private NetworkSettings EditableNetworkSettings()
        {
            NetworkSettings settings = ScriptableSettings.GetOrFind<NetworkSettings>();
#if UNITY_EDITOR
            _editorNetworkSettings ??= (settings.NetworkType, settings.Ckey, settings.ServerAddress, settings.ServerPort);
#endif
            return settings;
        }

        private bool TryGetSession(out NetworkSessionSubSystem session)
        {
            session = null;
            if (InstanceFinder.NetworkManager == null || !SubSystems.TryGet(out session))
            {
                Log.Error(this, "Main menu cannot start a session: no NetworkManager or NetworkSessionSubSystem", Logs.Important);
                return false;
            }

            return true;
        }

        private void HandleClientState(ClientConnectionStateArgs args)
        {
            if (_operation is not (Operation.Joining or Operation.Hosting))
            {
                return;
            }

            if (args.ConnectionState == LocalConnectionState.Started && _operation == Operation.Joining)
            {
                SetStep(1);
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                Fail(_operation == Operation.Hosting
                    ? $"Couldn't start a local session on port {_operationPort}."
                    : JoinFailureReason());
            }
        }

        private string JoinFailureReason()
        {
            if (ConnectionNotices.HasReason)
            {
                return ConnectionNotices.LastReason;
            }

            // Past the first step the transport had connected, so the server was there.
            return _model.OperationStep == 0
                ? "Couldn't reach the server. Check the address and that it is running."
                : ConnectionNotices.Lost;
        }

        private void HandleServerState(ServerConnectionStateArgs args)
        {
            if (_operation != Operation.Hosting)
            {
                return;
            }

            if (args.ConnectionState == LocalConnectionState.Started)
            {
                SetStep(1);
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                Fail($"Couldn't start a local session on port {_operationPort}. Another program may be using it.");
            }
        }

        private void HandleAuthenticated()
        {
            if (_operation == Operation.Joining)
            {
                SetStep(2);
            }
        }

        private void SetStep(int step)
        {
            _model.OperationStep = Math.Max(_model.OperationStep, step);
            Present();
        }

        private void Fail(string reason)
        {
            Operation operation = _operation;
            Unsubscribe();
            _operation = Operation.None;
            StopConnections(operation == Operation.Hosting);
            Log.Information(this, "Main menu {operation} failed: {reason}", Logs.Important, operation, reason);
            if (operation == Operation.Hosting)
            {
                _localError = reason;
                _model.Screen = MainMenuScreen.Menu;
            }
            else
            {
                ShowFailure(reason);
            }

            Present();
        }

        /// <summary>Holds the dialog on the failure until the player retries or goes back.</summary>
        private void ShowFailure(string reason)
        {
            _model.Screen = MainMenuScreen.Connecting;
            _model.OperationFailed = true;
            _model.OperationReason = reason;
            _model.OperationNeedsToken = ConnectionNotices.LastCode == DisconnectCode.AdminToken;
            _model.OperationToken = _join.Token;
            if (_join.RecordRefusal(_state, reason, DateTime.Now.ToString("HH:mm")))
            {
                Save();
            }
        }

        private void ShowConnectionLost()
        {
            _lostPending = false;
            NetworkSettings settings = ScriptableSettings.GetOrFind<NetworkSettings>();
            string address = MainMenuAddress.Format(settings.ServerAddress, settings.ServerPort);
            RecentServer known = _state.Find($"{settings.ServerAddress}:{settings.ServerPort}");
            _join = new MainMenuJoinAttempt(settings.ServerAddress, settings.ServerPort,
                known != null ? DisplayName(known) : address, known?.AdminToken ?? LocalPlayer.AccessToken);
            _model.OperationName = _join.Name;
            _model.OperationAddress = address;
            _model.OperationLost = true;
            ShowFailure(ConnectionNotices.LastReason);
        }

        private void BackToServers()
        {
            // A first-time address has no row to carry the reason.
            if (_state.Find(_join.Id) == null)
            {
                _addressError = _model.OperationReason;
            }

            ClearFailure();
            _model.Screen = MainMenuScreen.Servers;
            RefreshStatuses();
        }

        private void ClearFailure()
        {
            _model.OperationFailed = false;
            _model.OperationLost = false;
            _model.OperationNeedsToken = false;
            _model.OperationReason = "";
        }

        private void CancelOperation()
        {
            Operation operation = _operation;
            if (operation is not (Operation.Joining or Operation.Hosting))
            {
                return;
            }

            Unsubscribe();
            _operation = Operation.None;
            StopConnections(operation == Operation.Hosting);
            _model.Screen = operation == Operation.Joining ? MainMenuScreen.Servers : MainMenuScreen.Menu;
        }

        private static void StopConnections(bool server)
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;
            if (networkManager == null)
            {
                return;
            }

            networkManager.ClientManager.StopConnection();
            if (server)
            {
                networkManager.ServerManager.StopConnection(true);
            }
        }

        /// <summary>The join worked (login accepted, or a local session's Game loaded): remember the server, newest first.</summary>
        private void CompleteOperation()
        {
            if (_operation == Operation.Joining)
            {
                string reported = _results.TryGetValue(_join.Id, out ServerStatusResult result) && result.Reachable ? result.Status.Name : "";
                _join.Complete(_state, reported);
                Save();
            }

            Unsubscribe();
            _operation = Operation.None;
        }

        private void StartUpdate()
        {
            if (_latestTag == null || !UpdaterLauncher.CanSelfUpdate)
            {
                UnityEngine.Application.OpenURL(ReleaseCheck.ReleasesPage);
                return;
            }

            if (!UpdaterLauncher.TryStart(out string error))
            {
                _model.UpdateMessage = error;
                return;
            }

            _operation = Operation.Updating;
            _model.Screen = MainMenuScreen.Updating;
            _model.OperationName = $"SS3D {_latestTag}";
            _model.OperationAddress = "The game restarts when the update is done.";
            _model.OperationStep = 0;
            StartCoroutine(QuitForUpdate());
        }

        private static IEnumerator QuitForUpdate()
        {
            // Let the dialog draw; the updater waits for this process to exit.
            yield return new WaitForSecondsRealtime(1.5f);
            Quit();
        }

        private static void Quit()
        {
            UnityEngine.Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void StartUpdateCheck()
        {
            if (_updateCheckStarted || UnityEngine.Application.isEditor)
            {
                return;
            }

            _updateCheckStarted = true;
            StartCoroutine(ReleaseCheck.Run(UnityEngine.Application.version, tag =>
            {
                _latestTag = tag;
                if (tag != null)
                {
                    Log.Information(this, "Newer stable release available: {tag}", Logs.Important, tag);
                }

                RequestPresent();
            }));
        }

        private void RefreshStatuses()
        {
            if (_query == null)
            {
                return;
            }

            foreach (RecentServer server in _state.Recent)
            {
                _query.Query(server.Id, server.Host, (ushort)server.Port);
            }
        }

        private void HandleStatus(string id, ServerStatusResult result)
        {
            _results[id] = result;
            if (!_state.Recent.Exists(s => _query.IsPending(s.Id)))
            {
                _checkedAt = DateTime.Now;
            }

            RequestPresent();
        }

        private void RequestPresent()
        {
            if (IsEditingServerList())
            {
                // Re-presenting rebuilds the list and would drop focus from a field being typed in.
                _presentPending = true;
                return;
            }

            Present();
        }

        private bool IsEditingServerList()
        {
            if (_view?.Root.panel?.focusController?.focusedElement is not VisualElement focused)
            {
                return false;
            }

            VisualElement content = _view.Root.Q("server-content");
            return content != null && content.Contains(focused);
        }

        private void Present()
        {
            _presentPending = false;
            if (_view == null)
            {
                return;
            }

            UpdateMusic();
            UpdateVideo();
            _model.MusicOn = _state.MusicOn;
            _model.MusicTitle = _music?.Title ?? "";
            _model.MusicVolume = SubSystems.TryGet(out PlayerSettingsSubSystem playerSettings) ? playerSettings.Settings.Audio.Music : 0.8f;
            _model.PlayerName = _state.PlayerName;
            _model.Port = _portText;
            _model.Version = UnityEngine.Application.version;
            _model.UpdateAvailable = _latestTag != null;
            _model.LatestVersion = _latestTag?.TrimStart('v') ?? "";
            _model.CanSelfUpdate = UpdaterLauncher.CanSelfUpdate;
            _model.FirstLaunch = _state.Recent.Count == 0;
            _model.Advanced = _advanced;
            _model.Address = _address;
            _model.AddressError = _addressError;
            _model.LocalError = _localError;
            _model.PublicList = MainMenuPublicListState.Empty;
            _model.Public = Array.Empty<MainMenuServerPresentation>();

            bool checking = _query != null && _state.Recent.Exists(s => _query.IsPending(s.Id));
            _model.CheckedText = checking ? "Checking…" : _checkedAt is { } at ? $"Checked {at:HH:mm}" : "";

            List<MainMenuServerPresentation> recent = new();
            int needUpdate = 0;
            foreach (RecentServer server in _state.Recent)
            {
                ServerStatusResult? result = _results.TryGetValue(server.Id, out ServerStatusResult r) ? r : null;
                MainMenuServerPresentation row = MainMenuServers.Present(server, result, _expanded.Contains(server.Id), ProtocolVersion.Current);
                needUpdate += row.State == MainMenuServerState.NeedsUpdate ? 1 : 0;
                recent.Add(row);
            }

            _model.Recent = recent;
            if (_operation != Operation.Updating)
            {
                _model.UpdateMessage = needUpdate switch
                {
                    0 => "",
                    1 => "1 server in your list needs it.",
                    _ => $"{needUpdate} servers in your list need it.",
                };
            }

            _view.Present(_model);
        }

        /// <summary>Music plays on the menu, server list, settings and while connecting; not while hosting or updating.</summary>
        private void UpdateMusic()
        {
            bool screen = _model.Screen is MainMenuScreen.Menu or MainMenuScreen.Servers or MainMenuScreen.Settings or MainMenuScreen.Connecting;
            _music?.SetAudible(_view != null && _state.MusicOn && screen);
        }

        /// <summary>Video plays behind the menu; the operation dialogs hold its frame for their blurred backdrop.</summary>
        private void UpdateVideo()
        {
            _video?.SetPlaying(IsBrowsing);
            _model.VideoWallpaper = _video?.Frame;
        }

        private bool IsBrowsing => _model.Screen is MainMenuScreen.Menu or MainMenuScreen.Servers or MainMenuScreen.Settings;

        private void ChangeWallpaper()
        {
            _lastWallpaper = _model.Wallpaper = PickOtherWallpaper(_catalog, _lastWallpaper, () => Random.value);
            _video?.Load(_catalog.WallpaperVideo(_model.Wallpaper));
        }

        /// <summary>A random wallpaper other than <paramref name="previous"/>; -1 allows any.</summary>
        public static int PickOtherWallpaper(MainMenuAssetCatalog catalog, int previous, Func<float> roll)
        {
            int pick = catalog.PickWallpaper(roll(), roll());
            for (int attempt = 0; attempt < 16 && pick == previous; attempt++)
            {
                pick = catalog.PickWallpaper(roll(), roll());
            }

            return pick != previous ? pick : (previous + 1) % catalog.WallpaperCount;
        }

        private void TickWallpaper()
        {
            bool frameChanged = _video != null && _video.Tick();
            bool ready = _catalog.WallpaperVideo(_model.Wallpaper) == null || _video?.Frame != null;
            switch (_rotation.Tick(Time.unscaledTime, IsBrowsing, ready))
            {
                case MainMenuWallpaperStep.FadeOut:
                    _model.WallpaperVeiled = true;
                    break;
                case MainMenuWallpaperStep.Swap:
                    ChangeWallpaper();
                    break;
                case MainMenuWallpaperStep.FadeIn:
                    _model.WallpaperVeiled = false;
                    break;
                default:
                    if (!frameChanged) return;
                    break;
            }

            UpdateVideo();
            _view.PresentWallpaper(_model);
        }

        /// <summary>A failed join swaps the Launcher scene (and its camera's listener) for Empty; keep the music audible.</summary>
        private void EnsureListener()
        {
            if (_listener == null && FindAnyObjectByType<AudioListener>() == null)
            {
                _listener = gameObject.AddComponent<AudioListener>();
            }
        }

        private void RemoveListener()
        {
            if (_listener != null)
            {
                Destroy(_listener);
                _listener = null;
            }
        }

        private static string DisplayName(RecentServer server) =>
            string.IsNullOrWhiteSpace(server.Name) ? server.Address : server.Name;

        private void Save() => _store?.Save(_state);

        private static string DefaultPlayerName()
        {
            string name = (Environment.UserName ?? "").Trim();
            if (name.Length > MaxNameLength)
            {
                name = name.Substring(0, MaxNameLength);
            }

            return name;
        }

        private void Subscribe()
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;
            if (_subscribed || networkManager == null)
            {
                return;
            }

            networkManager.ClientManager.OnClientConnectionState += HandleClientState;
            networkManager.ClientManager.OnAuthenticated += HandleAuthenticated;
            networkManager.ServerManager.OnServerConnectionState += HandleServerState;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;
            if (!_subscribed || networkManager == null)
            {
                _subscribed = false;
                return;
            }

            networkManager.ClientManager.OnClientConnectionState -= HandleClientState;
            networkManager.ClientManager.OnAuthenticated -= HandleAuthenticated;
            networkManager.ServerManager.OnServerConnectionState -= HandleServerState;
            _subscribed = false;
        }
    }
}
