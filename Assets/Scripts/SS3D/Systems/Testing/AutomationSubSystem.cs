using Coimbra;
using Coimbra.Services.Events;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Statistic;
using FishNet.Transporting;
using SS3D.Application;
using SS3D.Application.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Settings;
using SS3D.Data.Generated;
using SS3D.Networking;
using SS3D.Networking.Settings;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Audio;
using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Humanoid;
using SS3D.Systems.Health;
using SS3D.Systems.IngameConsoleSystem;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RoundStateUpdated = SS3D.Systems.Rounds.Events.RoundStateUpdated;

namespace SS3D.Systems.Testing
{
    /// <summary>
    /// Drives a headless client/host/dedicated-server process through a scripted sequence of real
    /// gameplay actions for the multiplayer test harness (Testing/multiplayer/), using the same
    /// client-&gt;server broadcast/RPC APIs the UI calls
    /// (SS3D.UI.Lobby.LobbyUiSubSystem) - never
    /// simulated input, never the Editor-only stub broadcasts on
    /// <see cref="ReadyPlayersSubSystem"/>/<see cref="RoundSubSystem"/>, since those don't compile
    /// into a real built player/server.
    /// <para>
    /// Graphical (non-batchmode) clients can also probe and drive the UI Toolkit lobby with
    /// <c>ui_wait</c>/<c>ui_text</c>/<c>ui_submit</c>/<c>screenshot</c> - synthetic UI events, not OS input.
    /// <c>os_click</c>/<c>os_type</c>/<c>os_key</c> only emit a request signal (with screen pixels for a click);
    /// the driving harness performs the real OS input on this window (uinput), so they need one.
    /// </para>
    /// <para>
    /// Bootstrapped by <see cref="SS3D.Systems.Bootstrap.SystemsBootstrap"/> — a no-op unless
    /// <see cref="ApplicationSettings.ActiveTestScriptPath"/> is set (the "-testscript=" CLI arg, or
    /// a Multiplayer Play Mode player's <c>smoke:</c> tag in the editor), so this has zero effect on
    /// normal play.
    /// </para>
    /// <para>
    /// In the editor the script never quits the process: it writes its result to
    /// <see cref="MultiplayerPlayModePlayer.ResultDirectory"/> for the editor driver and stays in
    /// Play Mode until the driver stops it.
    /// </para>
    /// </summary>
    public sealed class AutomationSubSystem : SubSystem
    {
        private const float DefaultWaitTimeoutSeconds = 30f;

        private float _waitTimeoutSeconds = DefaultWaitTimeoutSeconds;

        private RoundState _currentRoundState = RoundState.Stopped;
        private bool _clientConnected;
        private bool _serverStarted;
        private bool _scriptStarted;
        private readonly List<string> _editorErrors = new();
        private readonly HashSet<int> _clientsInStartScenes = new();

        protected override void OnAwake()
        {
            base.OnAwake();

            AddHandle(ApplicationInitializing.AddListener(HandleApplicationInitializing));
        }

        private void HandleApplicationInitializing(ref EventContext context, in ApplicationInitializing e)
        {
            ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

            string scriptPath = applicationSettings.ActiveTestScriptPath;

            if (string.IsNullOrEmpty(scriptPath))
            {
                return;
            }

            // Disconnect / scene unload re-fires ApplicationInitializing. Re-running the script
            // (and NetworkSession re-join) is what produced post-ScriptComplete Errors and RoleSubSystem
            // duplicate-key exceptions in the multiplayer harness.
            if (_scriptStarted)
            {
                return;
            }

            _scriptStarted = true;

            // Capture stacks for FishNet SyncVar-on-client warnings (smoke denylist).
            // Qualify UnityEngine.Application — this file imports SS3D.Application.
            UnityEngine.Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);
            UnityEngine.Application.logMessageReceived += HandleUnityLogMessage;

            if (UnityEngine.Application.isEditor)
            {
                UnityEngine.Application.logMessageReceived += CollectEditorError;
            }

            SubscribeToConnectionEvents();
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));

            StartCoroutine(RunScriptAfterSession(scriptPath));
        }

        /// <summary>
        /// IntroUIHelper skips auto-join when -testscript= is set so disconnect/reconnect is not
        /// raced by SkipIntro. Kick off the first client session here instead - for an MPPM clone,
        /// only once the host has its start scenes up (see <see cref="WaitForHost"/>).
        /// </summary>
        private IEnumerator RunScriptAfterSession(string scriptPath)
        {
            if (!IsServerRole())
            {
                if (UnityEngine.Application.isEditor && MultiplayerPlayModePlayer.IsVirtualPlayer)
                {
                    yield return WaitForHost();
                }

                SubSystems.Get<NetworkSessionSubSystem>().StartNetworkSession();
            }

            yield return RunScript(scriptPath);
        }

        private IEnumerator WaitForHost()
        {
            const float timeoutSeconds = 180f;
            string marker = MultiplayerPlayModePlayer.HostReadyPath;
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;

            while (!System.IO.File.Exists(marker) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            if (!System.IO.File.Exists(marker))
            {
                UnityEngine.Debug.LogWarning($"MPPM host did not report ready within {timeoutSeconds}s; connecting anyway");
            }
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            // The editor keeps static delegates across Play Mode sessions (domain reload is off).
            UnityEngine.Application.logMessageReceived -= HandleUnityLogMessage;
            UnityEngine.Application.logMessageReceived -= CollectEditorError;

            if (InstanceFinder.NetworkManager != null)
            {
                InstanceFinder.NetworkManager.SceneManager.OnClientLoadedStartScenes -= HandleClientLoadedStartScenes;
            }
        }

        private void SubscribeToConnectionEvents()
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;

            networkManager.ClientManager.OnClientConnectionState += HandleClientConnectionState;
            networkManager.ServerManager.OnServerConnectionState += HandleServerConnectionState;
            networkManager.SceneManager.OnClientLoadedStartScenes += HandleClientLoadedStartScenes;
        }

        /// <summary>
        /// Server side: a remote client has finished loading the start scenes (the online Game
        /// scene). Starting the round before this drops scene objects for that client.
        /// </summary>
        private void HandleClientLoadedStartScenes(FishNet.Connection.NetworkConnection connection, bool asServer)
        {
            if (asServer && !connection.IsHost)
            {
                _clientsInStartScenes.Add(connection.ClientId);
            }
        }

        private void HandleClientConnectionState(ClientConnectionStateArgs args)
        {
            _clientConnected = args.ConnectionState == LocalConnectionState.Started;
        }

        private void HandleServerConnectionState(ServerConnectionStateArgs args)
        {
            _serverStarted = args.ConnectionState == LocalConnectionState.Started;
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            _currentRoundState = e.RoundState;
        }

        /// <summary>
        /// The MPPM main editor hosts; its clones wait for this before connecting, so they never
        /// receive Game scene spawns before they have Game (see HostReadyPath).
        /// </summary>
        private static void MarkHostReady()
        {
            if (!UnityEngine.Application.isEditor || MultiplayerPlayModePlayer.IsVirtualPlayer)
            {
                return;
            }

            System.IO.Directory.CreateDirectory(MultiplayerPlayModePlayer.ResultDirectory);
            System.IO.File.WriteAllText(MultiplayerPlayModePlayer.HostReadyPath, "ready");
        }

        private bool IsRoundState(RoundState target)
        {
            if (_currentRoundState == target)
            {
                return true;
            }

            // SyncVar can advance before/without our event listener seeing every transition
            // (e.g. after scene churn). Prefer live RoundSubSystem state when available.
            if (SubSystems.TryGet(out RoundSubSystem roundSystem) && roundSystem.CurrentRoundState == target)
            {
                _currentRoundState = target;
                return true;
            }

            return false;
        }

        private IEnumerator RunScript(string scriptPath)
        {
            IReadOnlyList<AutomationInstruction> instructions = null;
            string failureReason = null;

            try
            {
                instructions = AutomationScript.Load(scriptPath);
            }
            catch (Exception ex)
            {
                failureReason = $"CouldNotLoadScript:{ex.Message}";
            }

            if (failureReason != null)
            {
                yield return Finish("ScriptFailed", failureReason);
                yield break;
            }

            foreach (AutomationInstruction instruction in instructions)
            {
                IEnumerator step = RunInstruction(instruction);

                while (true)
                {
                    bool moved;

                    try
                    {
                        moved = step.MoveNext();
                    }
                    catch (Exception ex)
                    {
                        failureReason = $"{instruction.Opcode}:{ex.Message}";
                        break;
                    }

                    if (!moved)
                    {
                        break;
                    }

                    // Drive nested enumerators ourselves (e.g. WaitUntil). Yielding them to Unity
                    // lets TimeoutException escape through SetupCoroutine instead of our catch.
                    if (step.Current is IEnumerator nested)
                    {
                        while (true)
                        {
                            bool nestedMoved;

                            try
                            {
                                nestedMoved = nested.MoveNext();
                            }
                            catch (Exception ex)
                            {
                                failureReason = $"{instruction.Opcode}:{ex.Message}";
                                break;
                            }

                            if (!nestedMoved)
                            {
                                break;
                            }

                            yield return nested.Current;
                        }

                        if (failureReason != null)
                        {
                            break;
                        }
                    }
                    else
                    {
                        yield return step.Current;
                    }
                }

                if (failureReason != null)
                {
                    yield return Finish("ScriptFailed", failureReason);
                    yield break;
                }
            }

            yield return Finish("ScriptComplete");
        }

        private IEnumerator Finish(string signal, string reason = "")
        {
            TestSignal.Emit(this, signal, reason);

            if (UnityEngine.Application.isEditor)
            {
                WriteEditorResult(signal, reason);
                yield break;
            }

            yield return QuitAfterDelay();
        }

        /// <summary>
        /// The editor's equivalent of the smoke harness's log gate: the signal plus every error
        /// this player logged since the script started, for MultiplayerPlayModeSmoke to judge.
        /// </summary>
        private void WriteEditorResult(string signal, string reason)
        {
            EditorScriptResult result = new()
            {
                player = MultiplayerPlayModePlayer.Name,
                signal = signal,
                reason = reason,
                errors = _editorErrors.ToArray(),
            };

            string directory = MultiplayerPlayModePlayer.ResultDirectory;
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.WriteAllText(System.IO.Path.Combine(directory, result.player + ".json"), JsonUtility.ToJson(result, true));
        }

        private void CollectEditorError(string condition, string stackTrace, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert)
            {
                _editorErrors.Add($"{type}: {condition}");
            }
        }

        [Serializable]
        private struct EditorScriptResult
        {
            public string player;
            public string signal;
            public string reason;
            public string[] errors;
        }

        private IEnumerator RunInstruction(AutomationInstruction instruction)
        {
            switch (instruction.Opcode)
            {
                case "assert_bullet_decal":
                    TestSignal.Emit(this, "BulletDecalValidated", BulletDecalProbe.Validate());
                    break;

                case "stamp_bullet_holes":
                    TestSignal.Emit(this, "BulletHolesStamped", BulletDecalProbe.StampVisibleSurfaces());
                    break;

                case "wait_connected":
                    yield return WaitUntil(IsConnected, _waitTimeoutSeconds, "wait_connected");

                    // FishNet marks the client Connected before Game loads additively and before
                    // UnauthorizedPlayer auth finishes. Lobby actions need a Player with a ckey
                    // owned by this connection — otherwise start_round hits PermissionSubSystem
                    // with a null ckey and the round never starts.
                    if (!IsServerRole())
                    {
                        yield return WaitUntil(IsLobbyReady, _waitTimeoutSeconds, "wait_lobby");
                    }

                    // The MPPM main editor reports ready only once Game is loaded (see MarkHostReady).
                    if (UnityEngine.Application.isEditor && !MultiplayerPlayModePlayer.IsVirtualPlayer)
                    {
                        yield return WaitUntil(IsGameSceneLoaded, _waitTimeoutSeconds, "wait_game_scene");
                        MarkHostReady();
                    }

                    TestSignal.Emit(this, IsServerRole() ? "ServerReady" : "ClientConnected");
                    break;

                // Server: N remote clients have finished loading the start scenes. Used by the
                // Multiplayer Play Mode host, where starting the round before the clone's client
                // has Game would drop the round's scene objects for it (FishNet SceneId errors).
                case "wait_clients":
                    int clientsInScenes = int.Parse(instruction.Args[0], System.Globalization.CultureInfo.InvariantCulture);
                    yield return WaitUntil(() => _clientsInStartScenes.Count >= clientsInScenes, _waitTimeoutSeconds, "wait_clients");
                    break;

                case "ready":
                    SendReady(true);
                    break;

                case "probe_server_settings_permissions":
                    RoundSubSystem settingsRounds = SubSystems.Get<RoundSubSystem>();
                    settingsRounds.CmdReadServerSettings();
                    ServerSettings unauthorized = ServerSettings.Default;
                    unauthorized.PlayerCap = 1;
                    unauthorized.AllowLateJoin = false;
                    settingsRounds.CmdSaveServerSettings(unauthorized);
                    settingsRounds.CmdUpdateServerSetting(ServerSetting.PlayerCap, unauthorized);
                    foreach (RoundAdminAction action in Enum.GetValues(typeof(RoundAdminAction)))
                        settingsRounds.CmdRoundAdminAction(action);
                    yield return new WaitForSeconds(2);
                    if (settingsRounds.HasAdminSettings || settingsRounds.CurrentRoundState != RoundState.Stopped)
                        throw new InvalidOperationException("Non-admin gained server settings access.");
                    TestSignal.Emit(this, "ServerSettingsPermissionVerified");
                    break;

                case "start_round":
                    InstanceFinder.ClientManager.Broadcast(new ChangeRoundStateMessage(true));
                    break;

                case "wait_round":
                    RoundState target = Enum.Parse<RoundState>(instruction.Args[0], ignoreCase: true);
                    yield return WaitUntil(() => IsRoundState(target), _waitTimeoutSeconds, "wait_round");
                    TestSignal.Emit(this, "RoundStateChanged", target.ToString());
                    break;

                case "embark":
                    Embark();
                    TestSignal.Emit(this, "PlayerEmbarked");
                    break;

                case "assert_embarked":
                    // Confirms this connection currently owns a spawned Entity - unlike "embark",
                    // which only fires the spawn request without waiting on it. Used after
                    // "reconnect" to verify EntitySubSystem.TryReclaimEntity actually handed
                    // control of the pre-existing body back to this connection, not just that the
                    // connection itself re-authorized.
                    yield return WaitUntil(IsEmbarked, _waitTimeoutSeconds, "assert_embarked");
                    TestSignal.Emit(this, "EmbarkVerified");
                    break;

                case "reconnect":
                    // Re-opens the client connection this process already had settings for
                    // (same ip/port/ckey NetworkSessionSubSystem resolved at startup), simulating
                    // a disconnected player rejoining rather than a brand-new client. Must follow
                    // a "disconnect" for the same role; only valid for a client script.
                    yield return Reconnect();
                    break;

                case "console":
                    RunConsoleCommand(ExpandVariables(instruction.ArgsJoined));
                    break;

                // Retries a console line until it stops throwing: waits on an Offline assert command
                // (e.g. observesync) whose state another process changes.
                case "wait_console":
                    yield return WaitForConsole(ExpandVariables(instruction.ArgsJoined));
                    TestSignal.Emit(this, "ConsoleCheckPassed", instruction.ArgsJoined);
                    break;

                // At least N player bodies are spawned, as this process sees them.
                case "wait_players":
                    int players = int.Parse(instruction.Args[0], System.Globalization.CultureInfo.InvariantCulture);
                    yield return WaitUntil(() => CountSpawnedPlayers() >= players, _waitTimeoutSeconds, $"wait_players {players}");
                    TestSignal.Emit(this, "PlayersSpawned", players.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    break;

                // Server: the sandbox wrote at least N reset-health lines (one per round reset).
                case "assert_reset_health":
                    int minimum = int.Parse(instruction.Args[0], System.Globalization.CultureInfo.InvariantCulture);
                    yield return WaitUntil(
                        () => SS3D.Data.Management.LocalStorage.ReadJsonlLines(SS3D.Data.Persistence.PersistencePaths.ResetHealth).Length >= minimum,
                        _waitTimeoutSeconds, "assert_reset_health");
                    TestSignal.Emit(this, "ResetHealthVerified", minimum.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    break;

                // Client: the server's round outcome reached this process (buffered RPC) with the given
                // trigger, and lists this player. Not a UI check: the round-end overlay is visual review.
                // Optional: the winner, then "antagonist" (this player is on the antagonist roster, i.e. was drawn).
                case "assert_outcome":
                    RoundEndTrigger trigger = Enum.Parse<RoundEndTrigger>(instruction.Args[0], ignoreCase: true);
                    RoundWinner? winner = instruction.Args.Length > 1
                        ? Enum.Parse<RoundWinner>(instruction.Args[1], ignoreCase: true) : null;
                    bool antagonist = instruction.Args.Length > 2
                        && string.Equals(instruction.Args[2], "antagonist", StringComparison.OrdinalIgnoreCase);
                    yield return WaitUntil(() => OutcomeListsMe(trigger, winner, antagonist), _waitTimeoutSeconds,
                        $"assert_outcome {instruction.ArgsJoined}");
                    TestSignal.Emit(this, "OutcomeVerified", instruction.ArgsJoined);
                    break;

                // Server: opens a hole to space beside the first body this process does not own.
                case "breach_beside_client":
                    TestSignal.Emit(this, "BreachOpened", BreachBesideClient());
                    break;

                // Host: the next round loads this map and mode. Map names may not contain spaces here.
                case "use_station":
                    UseStation(instruction.Args[0], instruction.Args[1]);
                    yield return new WaitForSeconds(1);
                    break;

                // Server: opens a one-tile hole in the hull, the way destruction does: the wall goes
                // and the plenum stays, so the hole is a neck between the air and space.
                case "breach_hull":
                    TestSignal.Emit(this, "BreachOpened", BreachHull());
                    break;

                // Server: spawns this many loose items on the air tiles nearest the last breach_hull.
                case "scatter_items":
                    ScatterItems(int.Parse(instruction.Args[0]));
                    TestSignal.Emit(this, "ItemsScattered");
                    break;

                // Server: records atmos tick cost and patch traffic for this many seconds.
                case "atmos_budget":
                    yield return SampleAtmosBudget(float.Parse(instruction.Args[0], System.Globalization.CultureInfo.InvariantCulture));
                    break;

                // Client: records the bytes this process really received from the server for this many seconds.
                case "client_traffic":
                    yield return SampleClientTraffic(float.Parse(instruction.Args[0], System.Globalization.CultureInfo.InvariantCulture));
                    break;

                // Client: this process drew wind streaks, its own body was pushed, and it heard the wind and the air thinning.
                case "assert_wind_felt":
                    yield return AssertWindFelt();
                    TestSignal.Emit(this, "WindFelt");
                    break;

                // Client: the owned body's gear offers internals, and a toggle from this process reaches the server.
                case "assert_internals_toggle":
                    yield return AssertInternalsToggle();
                    TestSignal.Emit(this, "InternalsToggled");
                    break;

                // Server: blows this speed at every remote body for this many seconds, reversing every 2 s so a wall cannot end the test.
                case "wind_on_clients":
                    yield return BlowWindOnClients(
                        float.Parse(instruction.Args[0], System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(instruction.Args[1], System.Globalization.CultureInfo.InvariantCulture));
                    break;

                // Client: the wind moves the owned body, boots switched on from this process stop it, and off lets it move again.
                case "assert_magboots_wind":
                    yield return AssertMagbootsWind();
                    TestSignal.Emit(this, "MagbootsHeld");
                    break;

                case "wait_seconds":
                    yield return new WaitForSeconds(float.Parse(instruction.Args[0]));
                    break;

                // Applies to every later wait; use when another process (or a person) drives the next step.
                case "wait_timeout":
                    _waitTimeoutSeconds = float.Parse(instruction.Args[0], System.Globalization.CultureInfo.InvariantCulture);
                    break;

                case "disconnect":
                    Disconnect();
                    break;

                // UI Toolkit probes for graphical clients (not -batchmode). Selectors are
                // "#element-name" or ".uss-class"; the first match across UIDocuments is used.
                case "ui_wait":
                    string state = instruction.Args[1].ToLowerInvariant();
                    yield return WaitUntil(() => IsUiState(instruction.Args[0], state), _waitTimeoutSeconds, $"ui_wait {instruction.ArgsJoined}");
                    TestSignal.Emit(this, "UiState", instruction.ArgsJoined);
                    break;

                case "ui_text":
                    string expected = string.Join(" ", instruction.Args, 1, instruction.Args.Length - 1);
                    yield return WaitUntil(() => UiTextContains(instruction.Args[0], expected), _waitTimeoutSeconds, $"ui_text {instruction.ArgsJoined}");
                    TestSignal.Emit(this, "UiText", instruction.ArgsJoined);
                    break;

                case "ui_submit":
                    SubmitUi(instruction.Args[0]);
                    TestSignal.Emit(this, "UiSubmitted", instruction.Args[0]);
                    break;

                // Real OS input, performed by the harness from these signals; the next ui_* line checks the effect.
                case "os_click":
                    // A just-shown element has no layout until the next panel update (worldBound is NaN).
                    yield return WaitUntil(() => IsUiState(instruction.Args[0], "enabled") && FindUi(instruction.Args[0]).worldBound.width > 0,
                        _waitTimeoutSeconds, $"os_click {instruction.Args[0]}");
                    Vector2Int pixel = ToWindowPixels(FindUi(instruction.Args[0]));
                    TestSignal.Emit(this, "OsClick", $"{pixel.x} {pixel.y} {instruction.Args[0]}");
                    break;

                case "os_type":
                    TestSignal.Emit(this, "OsType", instruction.ArgsJoined);
                    break;

                case "os_key":
                    TestSignal.Emit(this, "OsKey", instruction.Args[0]);
                    break;

                case "screenshot":
                    string screenshotPath = ResolveOutputPath(instruction.Args[0] + ".png");
                    ScreenCapture.CaptureScreenshot(screenshotPath);
                    yield return null;
                    yield return null;
                    TestSignal.Emit(this, "Screenshot", screenshotPath);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown automation instruction '{instruction.Opcode}'");
            }
        }

        private bool IsServerRole()
        {
            NetworkSettings networkSettings = ScriptableSettings.GetOrFind<NetworkSettings>();

            return networkSettings.NetworkType == SS3D.Networking.NetworkType.DedicatedServer;
        }

        private bool IsConnected() => IsServerRole() ? _serverStarted : _clientConnected;

        private static bool IsGameSceneLoaded() =>
            UnityEngine.SceneManagement.SceneManager.GetSceneByName(Scenes.Game).isLoaded;

        private static bool IsLobbyReady()
        {
            if (!SubSystems.TryGet(out PlayerSubSystem playerSystem))
            {
                return false;
            }

            string ckey = playerSystem.GetCkey(InstanceFinder.ClientManager.Connection);
            return !string.IsNullOrEmpty(ckey);
        }

        private void SendReady(bool ready)
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            string ckey = playerSystem.GetCkey(InstanceFinder.ClientManager.Connection);

            InstanceFinder.ClientManager.Broadcast(new ChangePlayerReadyMessage(ckey, ready));
        }

        private void Embark()
        {
            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();

            Player player = playerSystem.GetPlayer(InstanceFinder.ClientManager.Connection);
            entitySystem.CmdSpawnLatePlayer(player);
        }

        private static bool OutcomeListsMe(RoundEndTrigger trigger, RoundWinner? winner = null, bool antagonist = false)
        {
            if (!SubSystems.TryGet(out RoundSubSystem rounds) || rounds.LastOutcome == null
                || rounds.LastOutcome.Trigger != trigger || !SubSystems.TryGet(out PlayerSubSystem players))
            {
                return false;
            }

            if (winner.HasValue && rounds.LastOutcome.Winner != winner.Value)
            {
                return false;
            }

            string ckey = players.GetCkey(InstanceFinder.ClientManager.Connection);
            return Array.Exists(rounds.LastOutcome.Players, p => p.Ckey == ckey && (!antagonist || p.Antagonist));
        }

        private string BreachBesideClient()
        {
            foreach (Entity body in SubSystems.Get<EntitySubSystem>().SpawnedPlayers)
            {
                if (body == null || body.IsOwner)
                    continue;

                TileSubSystem tiles = SubSystems.Get<TileSubSystem>();
                TileCoord beside = tiles.QueryService.WorldToTile(body.transform.position + Vector3.right * 2f, tiles.CurrentMap.MapId);
                Vector3 hole = tiles.QueryService.TileToWorld(beside);
                tiles.Construction.TryClearTile(hole, TileLayer.Turf, Direction.North);
                tiles.Construction.TryClearTile(hole, TileLayer.Plenum, Direction.North);
                return hole.ToString();
            }

            throw new InvalidOperationException("breach_beside_client: no remote body");
        }

        private static void UseStation(string map, string mode)
        {
            RoundSubSystem rounds = SubSystems.Get<RoundSubSystem>();
            ServerSettings settings = rounds.SettingsForServer;
            settings.Map = map;
            settings.Mode = mode;
            rounds.CmdUpdateServerSetting(ServerSetting.Map, settings);
            rounds.CmdUpdateServerSetting(ServerSetting.Mode, settings);
        }

        private static Vector3 _lastBreach;

        private static void ScatterItems(int count)
        {
            AtmosSimulation simulation = SubSystems.Get<AtmosSubSystem>().Simulation;
            TileSubSystem tiles = SubSystems.Get<TileSubSystem>();
            ItemSubSystem items = SubSystems.Get<ItemSubSystem>();
            var open = new List<Vector3>();
            simulation.ForEachCell((coord, index) =>
            {
                if (simulation.CellMeta[index].State != AtmosCellState.Vacuum && simulation.CellMeta[index].State != AtmosCellState.Blocked)
                    open.Add(tiles.QueryService.TileToWorld(coord));
            });
            open.Sort((a, b) => (a - _lastBreach).sqrMagnitude.CompareTo((b - _lastBreach).sqrMagnitude));
            for (int i = 0; i < count; i++)
                items.SpawnItem(Items.Wrench, open[i % open.Count] + Vector3.up * 0.2f, Quaternion.identity);
        }

        private static string BreachHull()
        {
            AtmosSimulation simulation = SubSystems.Get<AtmosSubSystem>().Simulation;
            TileSubSystem tiles = SubSystems.Get<TileSubSystem>();
            string hole = null;
            int candidates = 0;
            simulation.ForEachCell((coord, index) =>
            {
                if (hole != null || !IsHullBetweenAirAndSpace(simulation, coord, index)) return;
                candidates++;
                Vector3 world = tiles.QueryService.TileToWorld(coord);
                if (!tiles.Construction.TryClearTile(world, TileLayer.Turf, Direction.North).Success) return;
                hole = world.ToString();
                _lastBreach = world;
            });
            return hole ?? throw new InvalidOperationException($"breach_hull: {candidates} hull walls between air and space, none cleared");
        }

        private static bool IsHullBetweenAirAndSpace(AtmosSimulation simulation, TileCoord coord, int index)
        {
            if (simulation.CellMeta[index].State != AtmosCellState.Blocked) return false;
            return IsAirAcross(simulation, coord, 1, 0) || IsAirAcross(simulation, coord, 0, 1);
        }

        private static bool IsAirAcross(AtmosSimulation simulation, TileCoord coord, int x, int z)
        {
            var east = new TileCoord(simulation.MapId, coord.Grid.x + x, coord.Grid.y + z);
            var west = new TileCoord(simulation.MapId, coord.Grid.x - x, coord.Grid.y - z);
            if (!simulation.TryGetCellIndex(east, out int a) || !simulation.TryGetCellIndex(west, out int b)) return false;
            AtmosCellMeta metaA = simulation.CellMeta[a];
            AtmosCellMeta metaB = simulation.CellMeta[b];
            return (metaA.State == AtmosCellState.Vacuum && metaB.IsSimulated) || (metaB.State == AtmosCellState.Vacuum && metaA.IsSimulated);
        }

        private IEnumerator SampleAtmosBudget(float seconds)
        {
            AtmosSubSystem atmos = SubSystems.Get<AtmosSubSystem>();
            var tick = new List<float>();
            var sim = new List<float>();
            var chunks = new List<int>();
            int maxActive = 0;
            int maxPushed = 0;
            var sent = new List<float>();
            void OnTraffic(NetworkTrafficArgs traffic) => sent.Add(traffic.FromServerBytes / 1024f);
            NetworkTraficStatistics statistics = InstanceFinder.NetworkManager.StatisticsManager.NetworkTraffic;
            statistics.OnServerNetworkTraffic += OnTraffic;
            int lastTick = atmos.TickCount;
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                if (atmos.TickCount == lastTick) continue;
                lastTick = atmos.TickCount;
                tick.Add(atmos.LastTickMilliseconds);
                sim.Add(atmos.LastSimulationMilliseconds);
                chunks.Add(atmos.LastPatchChunks);
                maxActive = Mathf.Max(maxActive, atmos.Simulation.ActiveCellCount);
                maxPushed = Mathf.Max(maxPushed, atmos.LooseBodies.PushedCount);
            }

            statistics.OnServerNetworkTraffic -= OnTraffic;
            int totalChunks = 0;
            int maxChunks = 0;
            foreach (int count in chunks)
            {
                totalChunks += count;
                maxChunks = Mathf.Max(maxChunks, count);
            }

            float bytesPerSecond = AtmosChunkPatch.WireBytes / AtmosConstants.TickInterval;
            string report = $"ticks={tick.Count} tick p50/max={Percentile(tick, 0.5f):F2}/{Percentile(tick, 1f):F2} ms " +
                $"sim p50/max={Percentile(sim, 0.5f):F2}/{Percentile(sim, 1f):F2} ms active max={maxActive} " +
                $"chunks mean/max={(float)totalChunks / Mathf.Max(tick.Count, 1):F1}/{maxChunks} " +
                $"traffic mean/max={bytesPerSecond * totalChunks / Mathf.Max(tick.Count, 1) / 1024f:F0}/{bytesPerSecond * maxChunks / 1024f:F0} KB/s " +
                $"sent to all connections p50/max={Percentile(sent, 0.5f):F0}/{Percentile(sent, 1f):F0} KB/s pushed max={maxPushed} adrift={atmos.LooseBodies.AdriftCount}";
            Debug.Log($"[atmos-budget] {report}");
            TestSignal.Emit(this, "AtmosBudget", report);
        }

        private IEnumerator SampleClientTraffic(float seconds)
        {
            var received = new List<float>();
            void OnTraffic(NetworkTrafficArgs traffic) => received.Add(traffic.FromServerBytes / 1024f);
            NetworkTraficStatistics statistics = InstanceFinder.NetworkManager.StatisticsManager.NetworkTraffic;
            statistics.OnClientNetworkTraffic += OnTraffic;
            yield return new WaitForSeconds(seconds);
            statistics.OnClientNetworkTraffic -= OnTraffic;
            string series = string.Join(" ", received.ConvertAll(kilobytes => kilobytes.ToString("F0")));
            string report = $"received p50/p90/max={Percentile(received, 0.5f):F0}/{Percentile(received, 0.9f):F0}/{Percentile(received, 1f):F0} KB/s";
            Debug.Log($"[atmos-budget] client {report} per second: {series}");
            TestSignal.Emit(this, "ClientTraffic", report);
        }

        private static float Percentile(List<float> samples, float fraction)
        {
            if (samples.Count == 0) return 0f;
            samples.Sort();
            return samples[Mathf.Clamp(Mathf.CeilToInt(samples.Count * fraction) - 1, 0, samples.Count - 1)];
        }

        private const float MinHeardWindSpeed = 0.5f;
        private const float MaxHeardAir = 0.95f;

        private IEnumerator AssertWindFelt()
        {
            yield return WaitUntil(IsEmbarked, _waitTimeoutSeconds, "assert_wind_felt embarked");
            SubSystems.Get<EntitySubSystem>().TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out Entity entity);
            Ragdoll body = entity.GetComponent<Ragdoll>();
            AtmosWindView view = SubSystems.Get<AtmosSubSystem>().GetComponent<AtmosWindView>();
            Vector3 start = body.WindSamplePosition;
            AmbienceSubSystem ambience = SubSystems.Get<AmbienceSubSystem>();
            bool sawStreaks = false;
            bool heardWind = false;
            yield return WaitUntil(() =>
            {
                sawStreaks |= view != null && view.VisibleStreaks > 0;
                heardWind |= ambience.ListenerWindSpeed > MinHeardWindSpeed;
                return sawStreaks && heardWind && Vector3.Distance(body.WindSamplePosition, start) > 0.3f;
            }, _waitTimeoutSeconds, "assert_wind_felt");

            yield return WaitUntil(() => AudioSourceOcclusion.ListenerAir < MaxHeardAir, _waitTimeoutSeconds,
                "assert_wind_felt thin air");
        }

        private IEnumerator AssertInternalsToggle()
        {
            yield return WaitUntil(IsEmbarked, _waitTimeoutSeconds, "assert_internals_toggle embarked");
            SubSystems.Get<EntitySubSystem>().TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out Entity entity);
            EvaGearController gear = entity.GetComponent<EvaGearController>();
            HumanHealthController health = entity.GetComponent<HumanHealthController>();
            yield return WaitUntil(() => gear.InternalsAvailable, _waitTimeoutSeconds, "assert_internals_toggle available");

            gear.ClientToggleInternals();
            yield return WaitUntil(
                () => gear.InternalsOn && health.Snapshot.Environment.OnInternals && health.Snapshot.Environment.TankFill < 1f,
                _waitTimeoutSeconds,
                "assert_internals_toggle on");

            gear.ClientToggleInternals();
            yield return WaitUntil(
                () => !gear.InternalsOn && !health.Snapshot.Environment.OnInternals,
                _waitTimeoutSeconds,
                "assert_internals_toggle off");
        }

        private static IEnumerator BlowWindOnClients(float speed, float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += AtmosConstants.TickInterval)
            {
                float direction = Mathf.Repeat(elapsed, 4f) < 2f ? 1f : -1f;
                foreach (Entity body in SubSystems.Get<EntitySubSystem>().SpawnedPlayers)
                {
                    if (body != null && !body.IsOwner && body.TryGetComponent(out Ragdoll ragdoll))
                    {
                        ragdoll.ServerApplyWind(Vector3.right * (speed * direction));
                    }
                }

                yield return new WaitForSeconds(AtmosConstants.TickInterval);
            }
        }

        private IEnumerator AssertMagbootsWind()
        {
            const float settleSeconds = 2f;
            const float holdSeconds = 5f;
            const float movedMetres = 0.3f;
            const float heldMetres = 0.05f;

            yield return WaitUntil(IsEmbarked, _waitTimeoutSeconds, "assert_magboots_wind embarked");
            SubSystems.Get<EntitySubSystem>().TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out Entity entity);
            EvaGearController gear = entity.GetComponent<EvaGearController>();
            Ragdoll body = entity.GetComponent<Ragdoll>();
            yield return WaitUntil(() => gear.MagbootsAvailable, _waitTimeoutSeconds, "assert_magboots_wind available");

            Vector3 start = body.WindSamplePosition;
            yield return WaitUntil(() => Vector3.Distance(body.WindSamplePosition, start) > movedMetres, _waitTimeoutSeconds, "assert_magboots_wind blown");

            gear.ClientToggleMagboots();
            yield return WaitUntil(() => gear.MagbootsOn, _waitTimeoutSeconds, "assert_magboots_wind on");
            yield return new WaitForSeconds(settleSeconds);
            Vector3 anchored = body.WindSamplePosition;
            yield return new WaitForSeconds(holdSeconds);
            float drift = Vector3.Distance(body.WindSamplePosition, anchored);
            if (drift > heldMetres)
            {
                throw new InvalidOperationException($"assert_magboots_wind: the anchored body drifted {drift} m");
            }

            gear.ClientToggleMagboots();
            yield return WaitUntil(() => !gear.MagbootsOn, _waitTimeoutSeconds, "assert_magboots_wind off");
            yield return WaitUntil(() => Vector3.Distance(body.WindSamplePosition, anchored) > movedMetres, _waitTimeoutSeconds, "assert_magboots_wind blown again");
        }

        private bool IsEmbarked()
        {
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();

            return entitySystem.TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out _);
        }

        private IEnumerator Reconnect()
        {
            if (IsServerRole())
            {
                throw new InvalidOperationException("'reconnect' is a client-only instruction.");
            }

            // Disconnect is async (Stopping → Stopped). Starting while still Stopping makes
            // ClientManager.StartConnection return false and spam NetworkSession errors.
            yield return WaitUntil(IsClientFullyStopped, _waitTimeoutSeconds, "reconnect_stopped");

            // NetworkSessionSubSystem lives on Boot and is gone after the first online load
            // (and after Empty offline). Re-join with the same CLI-resolved NetworkSettings.
            // Keep Empty offline — never restore Boot (that re-arms the Intro storm).
            StartClientConnectionFromSettings();

            yield return WaitUntil(() => _clientConnected, _waitTimeoutSeconds, "reconnect_started");
        }

        /// <summary>Replaces {ckey} with this connection's ckey, so scripts can target their own player.</summary>
        private static string ExpandVariables(string commandLine)
        {
            if (!commandLine.Contains("{ckey}"))
            {
                return commandLine;
            }

            string ckey = SubSystems.Get<PlayerSubSystem>().GetCkey(InstanceFinder.ClientManager.Connection);
            return commandLine.Replace("{ckey}", ckey);
        }

        private static int CountSpawnedPlayers()
        {
            int count = 0;
            foreach (Entity entity in SubSystems.Get<EntitySubSystem>().SpawnedPlayers)
            {
                if (entity != null)
                {
                    count++;
                }
            }

            return count;
        }

        private IEnumerator WaitForConsole(string commandLine)
        {
            float startTime = Time.time;
            while (true)
            {
                string failure = null;
                try
                {
                    RunConsoleCommand(commandLine);
                }
                catch (Exception ex)
                {
                    failure = ex.Message;
                }

                if (failure == null)
                {
                    yield break;
                }

                if (Time.time - startTime > _waitTimeoutSeconds)
                {
                    throw new TimeoutException($"Timed out after {_waitTimeoutSeconds}s waiting for '{commandLine}': {failure}");
                }

                yield return new WaitForSeconds(0.25f);
            }
        }

        private void RunConsoleCommand(string commandLine)
        {
            CommandsController commandsController = FindFirstObjectByType<CommandsController>();

            if (commandsController == null)
            {
                throw new InvalidOperationException("No CommandsController found in the scene to run a console instruction against.");
            }

            commandsController.ClientProcessCommand(commandLine);
        }

        private void Disconnect()
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;

            if (IsServerRole())
            {
                networkManager.ServerManager.StopConnection(true);
            }
            else
            {
                // DefaultScene offline is Boot until a successful connect (then
                // ClientConnectionRecovery arms Empty). Explicitly set Empty before
                // StopConnection so Boot cannot reload as Single while NetworkManager is
                // DDOL → duplicate managers + Init/Intro storms. Staying in Game also fails:
                // networked scene objects never resync and wait_lobby never sees a ckey.
                RedirectOfflineSceneToEmpty();
                networkManager.ClientManager.StopConnection();
            }
        }

        private static bool IsClientFullyStopped()
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;
            if (networkManager == null)
            {
                return true;
            }

            LocalConnectionState state = networkManager.TransportManager.Transport.GetConnectionState(false);
            return state == LocalConnectionState.Stopped;
        }

        private void StartClientConnectionFromSettings()
        {
            NetworkSettings networkSettings = ScriptableSettings.GetOrFind<NetworkSettings>();
            NetworkManager networkManager = InstanceFinder.NetworkManager;

            LocalPlayer.UpdateCkey(networkSettings.Ckey);

            bool started = networkManager.ClientManager.StartConnection(
                networkSettings.ServerAddress,
                networkSettings.ServerPort);

            if (!started)
            {
                throw new InvalidOperationException(
                    $"Reconnect failed to StartConnection on {networkSettings.ServerAddress}:{networkSettings.ServerPort}.");
            }
        }

        private void RedirectOfflineSceneToEmpty()
        {
            DefaultScene defaultScene = InstanceFinder.NetworkManager.GetComponent<DefaultScene>();
            if (defaultScene == null)
            {
                return;
            }

            // Belt-and-suspenders on top of ClientConnectionRecovery: never leave Boot as
            // offline across StopConnection. Do not restore Boot afterward.
            if (defaultScene.GetOfflineScene() != Scenes.EmptyPath)
            {
                defaultScene.SetOfflineScene(Scenes.EmptyPath);
            }
        }

        private static VisualElement FindUi(string selector)
        {
            foreach (UIDocument document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                VisualElement root = document.rootVisualElement;
                VisualElement match = root == null ? null
                    : selector.StartsWith("#") ? root.Q(selector.Substring(1))
                    : selector.StartsWith(".") ? root.Q(className: selector.Substring(1))
                    : throw new ArgumentException($"UI selector '{selector}' must start with '#' or '.'");

                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static bool IsUiShown(VisualElement element)
        {
            if (element?.panel == null)
            {
                return false;
            }

            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.visibility == Visibility.Hidden)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsUiState(string selector, string state)
        {
            VisualElement element = FindUi(selector);

            return state switch
            {
                "visible" => IsUiShown(element),
                "hidden" => !IsUiShown(element),
                "enabled" => IsUiShown(element) && element.enabledInHierarchy,
                "disabled" => element != null && !element.enabledInHierarchy,
                "absent" => element == null,
                _ => throw new ArgumentException($"Unknown ui_wait state '{state}' (visible|hidden|enabled|disabled|absent)"),
            };
        }

        private static bool UiTextContains(string selector, string expected) =>
            FindUi(selector) is TextElement text && text.text != null
            && text.text.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;

        // Window-relative pixels (X11 origin top-left, like panel space) of the element's center.
        // Scales by the highest laid-out ancestor: the runtime panel's visualTree itself has no size.
        private static Vector2Int ToWindowPixels(VisualElement element)
        {
            VisualElement top = element;
            for (VisualElement current = element.parent; current != null; current = current.parent)
            {
                if (current.worldBound.width > 0 && current.worldBound.height > 0)
                {
                    top = current;
                }
            }

            Rect panel = top.worldBound;
            Vector2 center = element.worldBound.center;

            return new Vector2Int(
                Mathf.RoundToInt(center.x * Screen.width / panel.width),
                Mathf.RoundToInt(center.y * Screen.height / panel.height));
        }

        // Button.clicked ignores synthetic ClickEvents (its Clickable listens to pointer/submit).
        private static void SubmitUi(string selector)
        {
            VisualElement element = FindUi(selector);

            if (!IsUiShown(element) || !element.enabledInHierarchy)
            {
                throw new InvalidOperationException($"ui_submit target '{selector}' is missing, hidden or disabled.");
            }

            element.Focus();
            using NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled();
            submit.target = element;
            element.SendEvent(submit);
        }

        // SS3D_AUTOMATION_OUTPUT_DIR lets the harness collect artifacts per run.
        private static string ResolveOutputPath(string fileName)
        {
            string directory = Environment.GetEnvironmentVariable("SS3D_AUTOMATION_OUTPUT_DIR");

            if (string.IsNullOrEmpty(directory))
            {
                directory = UnityEngine.Application.persistentDataPath;
            }

            System.IO.Directory.CreateDirectory(directory);
            return System.IO.Path.Combine(directory, fileName);
        }

        private IEnumerator WaitUntil(Func<bool> condition, float timeoutSeconds, string label)
        {
            float startTime = Time.time;

            while (!condition())
            {
                if (Time.time - startTime > timeoutSeconds)
                {
                    throw new TimeoutException($"Timed out after {timeoutSeconds}s waiting for {label}");
                }

                yield return null;
            }
        }

        private IEnumerator QuitAfterDelay()
        {
            // One frame so Serilog can flush ScriptComplete/ScriptFailed, then hard-exit.
            // Application.Quit() in player builds still tears scenes down and re-enters
            // ApplicationInitializing (NetworkSession re-joins, duplicate automation) before the
            // process dies — that noise fails the harness after a successful scenario.
            yield return null;
            System.Environment.Exit(0);
        }

        private static void HandleUnityLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Warning
                || string.IsNullOrEmpty(condition)
                || !condition.Contains("Cannot complete operation as server when server is not active"))
            {
                return;
            }

            // FishNet SyncBase now embeds Behaviour=/Object=/ObjectId= in the warning text.
            // Re-log condition + Unity stack so triage can name the writer even when frames are stripped.
            string stack = string.IsNullOrEmpty(stackTrace) ? "(empty)" : stackTrace;
            Debug.Log($"[SS3D SyncVarGuard] FishNet server-write warning:\n{condition}\n--- unity stack ---\n{stack}");
        }
    }
}
