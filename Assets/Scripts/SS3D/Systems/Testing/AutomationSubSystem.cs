using Coimbra;
using Coimbra.Services.Events;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using SS3D.Application;
using SS3D.Application.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Core.Settings;
using SS3D.Data.Generated;
using SS3D.Networking;
using SS3D.Networking.Settings;
using SS3D.Systems.Entities;
using SS3D.Systems.IngameConsoleSystem;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
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
    /// <see cref="ApplicationSettings.TestScriptPath"/> is set (the "-testscript=" CLI arg), so this
    /// has zero effect on normal play.
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

        protected override void OnAwake()
        {
            base.OnAwake();

            AddHandle(ApplicationInitializing.AddListener(HandleApplicationInitializing));
        }

        private void HandleApplicationInitializing(ref EventContext context, in ApplicationInitializing e)
        {
            ApplicationSettings applicationSettings = ScriptableSettings.GetOrFind<ApplicationSettings>();

            if (string.IsNullOrEmpty(applicationSettings.TestScriptPath))
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

            SubscribeToConnectionEvents();
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));

            // IntroUIHelper skips auto-join when -testscript= is set so disconnect/reconnect
            // is not raced by SkipIntro. Kick off the first client session here instead.
            if (!IsServerRole())
            {
                SubSystems.Get<NetworkSessionSubSystem>().StartNetworkSession();
            }

            StartCoroutine(RunScript(applicationSettings.TestScriptPath));
        }

        private void SubscribeToConnectionEvents()
        {
            NetworkManager networkManager = InstanceFinder.NetworkManager;

            networkManager.ClientManager.OnClientConnectionState += HandleClientConnectionState;
            networkManager.ServerManager.OnServerConnectionState += HandleServerConnectionState;
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
                TestSignal.Emit(this, "ScriptFailed", failureReason);
                yield return QuitAfterDelay();
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
                    TestSignal.Emit(this, "ScriptFailed", failureReason);
                    yield return QuitAfterDelay();
                    yield break;
                }
            }

            TestSignal.Emit(this, "ScriptComplete");
            yield return QuitAfterDelay();
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

                    TestSignal.Emit(this, IsServerRole() ? "ServerReady" : "ClientConnected");
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
                    RunConsoleCommand(instruction.ArgsJoined);
                    break;

                // Server: the sandbox wrote at least N reset-health lines (one per round reset).
                case "assert_reset_health":
                    int minimum = int.Parse(instruction.Args[0], System.Globalization.CultureInfo.InvariantCulture);
                    yield return WaitUntil(
                        () => SS3D.Data.Management.LocalStorage.ReadJsonlLines(SS3D.Data.Persistence.PersistencePaths.ResetHealth).Length >= minimum,
                        _waitTimeoutSeconds, "assert_reset_health");
                    TestSignal.Emit(this, "ResetHealthVerified", minimum.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
