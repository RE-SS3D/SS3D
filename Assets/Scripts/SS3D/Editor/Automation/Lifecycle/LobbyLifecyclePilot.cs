using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FishNet;
using SS3D.Core;
using SS3D.Data.Management;
using SS3D.Data.Persistence;
using SS3D.UI.Lobby;
using SS3D.Permissions;
using SS3D.Systems.Comms;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Character;
using SS3D.Systems.Inputs;
using SS3D.Systems.Persistence;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Roles;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor.Commands;
using SS3D.Systems.Tile.SpawnPoints;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// Host-mode lobby session smoke: ready/unready → admin start → spawn → stop → return,
    /// late join, and local client disconnect/reconnect. Callable through MCP execute_menu_item.
    /// <para>
    /// Remote mode launches the built client (Builds/Game) as a non-admin player running
    /// Testing/multiplayer/scenarios/lobby-remote-client.txt, and drives the admin side from this Host.
    /// </para>
    /// </summary>
    [InitializeOnLoad]
    public static partial class LobbyLifecyclePilot
    {
        private const string ActiveKey = "SS3D.LobbyLifecyclePilot.Active";
        private const string OutputKey = "SS3D.LobbyLifecyclePilot.Output";
        private const string SceneKey = "SS3D.LobbyLifecyclePilot.PreviousStartScene";
        private const string ModeKey = "SS3D.LobbyLifecyclePilot.Mode";
        private const string RemoteMode = "remote";
        private const string RemoteCkey = "remoteprobe";
        private const string ClientBinary = AutomationBuild.ClientBinary;
        private const string RemoteScenario = "Testing/multiplayer/scenarios/lobby-remote-client.txt";

        private static readonly List<string> Errors = new();
        private static readonly List<string> Checks = new();
        private static readonly List<string> Timeline = new();
        private static Step[] _steps;
        private static int _index;
        private static bool _entered;
        private static double _stepDeadline;
        private static double _started;
        private static bool _endBeatShortened;
        private static int _repliesBefore;
        private static bool _running;
        private static string _savedName;
        private static bool _sawStationLoading;
        private static string _alternateStation;
        private static string _validationStation;
        private static uint _previousStationEpoch;

        private const string PilotCharacterName = "Lifecycle Pilot";
        private const string PilotServerName = "Lifecycle Pilot Station";
        private const string PilotMotd = "Line one\n\nLine <b>two</b>";
        private static string _originalServerName;
        private static string _originalMotd;
        private static double _typingUntil;
        private const float PilotHeight = 0.9f;
        private const float PilotFat = 0.7f;
        private static System.Diagnostics.Process _client;
        private static Report _pending;
        private static int _osInputsDone;
        private static string _clientWindow;
        private static System.Diagnostics.Process _input;
        private const string ChatProbe = "remoteprobe real keyboard";
        private static double _settleUntil;
        private static bool _pendingStartClick;

        /// <summary>Round-end beat for every stop that isn't about the summary (see ServerHelpers.TestEndBeatSeconds).</summary>
        private const int PilotEndBeatSeconds = 3;

        private sealed class Step
        {
            public string Name;
            public Action Enter;
            public Func<bool> Until;
            public Action Verify;
            public double Timeout = 30;
        }

        static LobbyLifecyclePilot()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        // Guards: host lobby lifecycle: ready, start, spawn, stop, late join, reconnect, character and settings persistence, map validation · Not cheaper because: drives the real lobby UI across whole rounds in one host; weekly graphical run
        [MenuItem("SS3D/Testing/Run Lobby Lifecycle Pilot")]
        public static void Run() => Start("lifecycle");

        // Guards: a built non-admin client sees the lobby, readies, chats, spawns and files a bug report against the editor host · Not cheaper because: needs a separate client process with its own UI; weekly graphical run
        [MenuItem("SS3D/Testing/Run Lobby Remote Client Pilot")]
        public static void RunRemoteClient() => Start(RemoteMode);

        private static bool Remote => SessionState.GetString(ModeKey, "") == RemoteMode;
        private static bool Sandbox => SessionState.GetString(ModeKey, "") == SandboxMode
            || SessionState.GetString(ModeKey, "") == HealthRegressionMode
            || SessionState.GetString(ModeKey, "") == RoundResetRegressionMode
            || SessionState.GetString(ModeKey, "") == InteractionOverlayMode;

        private static void Start(string mode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before starting the lobby lifecycle pilot.");
            if (EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Fix compilation errors before starting the lobby lifecycle pilot.");
            if (SessionState.GetBool(ActiveKey, false)) RestoreEditorSettings();
            string prefix = mode switch { RemoteMode => "remote", SandboxMode => "sandbox",
                HealthRegressionMode => "health-regressions", RoundResetRegressionMode => "round-reset-regressions", InteractionOverlayMode => "interaction-overlay", _ => "lifecycle" };
            string output = Path.GetFullPath($"artifacts/ui/{prefix}-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(output);
            try
            {
                AutomationPreflight.Require($"lobby {mode} pilot");
                // Testing/ui/validate.sh rebuilds first; a direct run refuses rather than test an old client.
                string stale = mode == RemoteMode ? AutomationBuild.ClientStaleReason() : null;
                if (stale != null)
                    throw new InvalidOperationException($"Refusing the remote client pilot: {stale}. "
                        + "Rebuild with Testing/ui/validate.sh or AutomationBuild.BuildClient().");
            }
            catch (InvalidOperationException exception)
            {
                Write(new Report { status = "failed", failure = exception.Message }, output);
                throw;
            }
            SessionState.SetString(OutputKey, output);
            SessionState.SetString(ModeKey, mode);
            SessionState.SetString(SceneKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(ActiveKey, true);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Content/Scenes/Boot.unity");
            File.WriteAllText(Path.Combine(output, "status.json"), "{\"status\":\"starting\"}");
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Errors.Clear();
                Checks.Clear();
                Timeline.Clear();
                if (Remote) _steps = BuildRemoteSteps();
                else _steps = Sandbox ? BuildSandboxSteps() : BuildSteps();
                if (SessionState.GetString(ModeKey, "") == HealthRegressionMode)
                    _steps = _steps.TakeWhile(step => step.Name != "/bug in chat saves a report with round and state").ToArray();
                if (SessionState.GetString(ModeKey, "") == RoundResetRegressionMode)
                    _steps = BuildRoundResetSteps();
                if (SessionState.GetString(ModeKey, "") == InteractionOverlayMode)
                    _steps = BuildInteractionOverlaySteps();
                _index = 0;
                _entered = false;
                _started = EditorApplication.timeSinceStartup;
                _osInputsDone = 0;
                _sawStationLoading = false;
                _endBeatShortened = false;
                _alternateStation = null;
                _validationStation = null;
                _clientWindow = null;
                _running = true;
                Application.logMessageReceived += OnLog;
                EditorApplication.update += Tick;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (_running) Finish("Play Mode exited before the scenario completed.", false);
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                RestoreEditorSettings();
                if (_pending == null) return;
                Report report = _pending;
                string output = OutputDir;
                _pending = null;
                AutomationPreflight.AfterPlay(problems => Complete(report, output, problems));
            }
        }

        private static void RestoreEditorSettings()
        {
            string previous = SessionState.GetString(SceneKey, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous)
                ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
            SessionState.SetBool(ActiveKey, false);
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Errors.Add($"[{Elapsed():0.0}s step {_index}] {message}\n{stack}");
        }

        private static double Elapsed() => EditorApplication.timeSinceStartup - _started;

        private static void Tick()
        {
            if (!_running) return;
            try
            {
                if (Remote) PerformOsInput();
                if (!_endBeatShortened && Online() && Rounds() != null)
                {
                    Rounds().SetEndBeatSecondsForTesting(PilotEndBeatSeconds);
                    _endBeatShortened = true;
                }
                if (Rounds() != null && Rounds().CurrentStationLoadState == StationLoadState.Loading
                    && Shell()?.Q<Label>("lobby-map-load-status")?.text.StartsWith("Loading map:") == true)
                {
                    if (!_sawStationLoading) Capture("server-settings-loading");
                    _sawStationLoading = true;
                }
                Step step = _steps[_index];
                if (!_entered)
                {
                    _entered = true;
                    _stepDeadline = EditorApplication.timeSinceStartup + step.Timeout;
                    step.Enter?.Invoke();
                    return;
                }

                if (step.Until != null && !step.Until())
                {
                    if (EditorApplication.timeSinceStartup > _stepDeadline)
                        throw new TimeoutException($"Step '{step.Name}' timed out after {step.Timeout}s. State: {Describe()}");
                    return;
                }

                step.Verify?.Invoke();
                Timeline.Add($"{Elapsed():0.0}s {step.Name}: {Describe()}");
                _entered = false;
                if (++_index >= _steps.Length) Finish(null, true);
            }
            catch (Exception exception) { Finish(exception.ToString(), true); }
        }

        private static Vector3 _mapEditSpawnPosition;
        private static DateTime _templateWriteTime;

        /// <summary>Places (or removes) an Assistant spawn through the map editor's command path, as an author would.</summary>
        private static void EditMapSpawn(bool remove)
        {
            if (!remove)
                _mapEditSpawnPosition = SubSystems.Get<TileSubSystem>().CurrentMap.GetAllChunks()
                    .SelectMany(chunk => chunk.GetAllTilePlacedObjects()).First(tile => tile.Layer == TileLayer.Plenum).transform.position;
            SubSystems.Get<MapEditorSubSystem>().SubmitCommands(new MapEditorCommandDto[] { new MapEditorCommandDto
            {
                Kind = remove ? MapEditorCommandKind.ClearSpawnPoint : MapEditorCommandKind.PlaceSpawnPoint,
                AssetName = "spawn:job:Assistant", Position = _mapEditSpawnPosition, Direction = Direction.North,
            } });
        }

        private static bool MapEditSpawnPresent() => SubSystems.Get<TileSubSystem>().SpawnPoints.HasAt(_mapEditSpawnPosition);
        private static bool UnsavedMapEdits() => SubSystems.Get<MapEditorSubSystem>().HasUnsavedChanges;
        private static bool TemplateExists(string name) => SubSystems.Get<PersistenceSubSystem>().StationTemplateExists(name);
        private static string TemplatePath(string name) => PersistencePaths.StationTemplates + "/" + name;

        private static DateTime TemplateWriteTime(string name) => LocalStorage.GetLastWriteTimeUtc(TemplatePath(name));

        private static Step[] BuildSteps() => new Step[]
        {
            new Step
            {
                Name = "Lobby ready on host boot",
                Timeout = 180,
                Until = () => Online() && Local() != null && Rounds() != null && Visible(Shell()) && Rounds().CurrentRoundState == RoundState.Stopped
                    && CountLabel() == "0/1 ready" && ReadyButton().enabledInHierarchy,
                Verify = () =>
                {
                    Require(!LegacyLobbyCanvasPresent(), "No legacy uGUI lobby canvas in the scene");
                    Require(!Visible(Creator()), "Creator hidden on boot");
                    Require(BoothLive(), "Preview booth active with RT bound");
                    Require(AdminButton().enabledInHierarchy, "Host owner has Admin enabled");
                    Require(ReadyButton().enabledInHierarchy && ReadyButton().text == "NOT READY", "Ready button enabled and NOT READY");
                    Require(CountLabel() == "0/1 ready", "Sidebar shows 0/1 ready");
                    Require(SubSystems.Get<SS3D.Systems.Tile.TileSubSystem>().CurrentMap.ChunkCount == 0, "Lobby boots without station chunks");
                    Require(Rounds().CurrentStationLoadState == StationLoadState.NotLoaded
                        && Shell().Q<Label>("lobby-map-load-status").text == "No map loaded", "Lobby reports no map loaded");
                    CreateValidationStation(valid: false);
                    _savedName = CharacterName();
                },
            },
            new Step
            {
                Name = "Open Server Settings and receive authorized snapshot",
                Enter = () => Click(AdminButton()),
                Until = () => Shell().Q<Button>("server-start")?.enabledInHierarchy == true,
                Verify = () =>
                {
                    Require(!Shell().Q<DropdownField>("server-map").choices.Contains("Current station")
                        && Shell().Q<DropdownField>("server-map").choices.Contains(Shell().Q<DropdownField>("server-map").value), "Map selector contains actual saved templates only");
                    Require(Shell().Q<DropdownField>("server-map").enabledInHierarchy, "Map can be changed before the first start");
                    Require(Shell().Q<DropdownField>("server-mode").choices.SequenceEqual(new string[] { "Secret", "Extended", "Nuclear Emergency" }), "Only implemented modes are offered");
                    Require(Shell().Q<Label>("server-ready-ratio").text == "0/1", "Admin ready ratio is live");
                    Require(!Shell().Q<Button>("server-force").enabledInHierarchy && !Shell().Q<Button>("server-extend").enabledInHierarchy
                        && !Shell().Q<Button>("server-cancel").enabledInHierarchy, "Countdown actions disabled while waiting");
                    foreach (string name in new string[] { "server-random-events", "server-friendly-fire", "server-respawn" })
                        Require(!Shell().Q<Toggle>(name).enabledInHierarchy && !string.IsNullOrEmpty(Shell().Q<Toggle>(name).tooltip), "Unsupported rule explains its disabled state: " + name);
                    Capture("server-settings-waiting");
                },
            },
            new Step
            {
                Name = "Inspect numeric fields and round rules",
                Enter = () => Shell().Q<ScrollView>("server-settings-scroll").ScrollTo(Shell().Q("server-respawn")),
                Until = () => Shell().Q("server-respawn").worldBound.yMax <= Shell().Q<ScrollView>("server-settings-scroll").contentViewport.worldBound.yMax,
                Verify = () => Capture("server-settings-rules"),
            },
            new Step
            {
                Name = "Return to round controls",
                Enter = () => Shell().Q<ScrollView>("server-settings-scroll").scrollOffset = Vector2.zero,
                Until = () => Shell().Q<ScrollView>("server-settings-scroll").scrollOffset.y == 0,
            },
            new Step
            {
                Name = "Rapid edits preserve both server settings",
                Enter = () =>
                {
                    Shell().Q<IntegerField>("server-player-cap").value = 24;
                    Shell().Q<IntegerField>("server-round-length").value = 45;
                },
                Until = () => Rounds().SettingsForServer.PlayerCap == 24 && Rounds().AdminSettings.PlayerCap == 24
                    && Rounds().SettingsForServer.RoundLengthMinutes == 45 && Rounds().AdminSettings.RoundLengthMinutes == 45,
                Verify = () => Require(true, "Rapid field edits both reach server and return in admin snapshot"),
            },
            new Step
            {
                Name = "Edit round length through Server Settings",
                Enter = () => Shell().Q<IntegerField>("server-round-length").value = 45,
                Until = () => Rounds().SettingsForServer.RoundLengthMinutes == 45,
            },
            new Step
            {
                Name = "Select Extended mode",
                Enter = () => Shell().Q<DropdownField>("server-mode").value = "Extended",
                Until = () => Rounds().SettingsForServer.Mode == "Extended",
            },
            new Step
            {
                Name = "Disable late join through Server Settings",
                Enter = () => Shell().Q<Toggle>("server-late-join").value = false,
                Until = () => !Rounds().AllowLateJoin && !Rounds().SettingsForServer.AllowLateJoin,
                Verify = () => Require(true, "Late-join policy reaches authoritative server"),
            },
            new Step
            {
                Name = "Reject out-of-range server configuration",
                // A whole-form save round trip can lag behind the rapid UI edits on a small window.
                Timeout = 120,
                Enter = () =>
                {
                    _repliesBefore = Rounds().AdminActionReplies;
                    ServerSettings invalid = Rounds().SettingsForServer;
                    invalid.PlayerCap = 0;
                    Rounds().CmdSaveServerSettings(invalid);
                },
                Until = () => Rounds().AdminActionReplies > _repliesBefore && Rounds().AdminSettingsStatus.Contains("rejected"),
                Verify = () => Require(Rounds().SettingsForServer.PlayerCap == 24, "Invalid request leaves authoritative settings unchanged"),
            },
            new Step
            {
                Name = "Select a real saved station",
                Enter = () => Shell().Q<DropdownField>("server-map").value = _validationStation,
                Until = () => Rounds().SettingsForServer.Map == _validationStation && Rounds().AdminSettings.Map == _validationStation,
                Verify = () => Require(Shell().Q<DropdownField>("server-map").choices.Contains(_validationStation), "Selected map is a saved template offered by the server"),
            },
            new Step
            {
                Name = "Invalid saved map blocks countdown with the full job list",
                Enter = () => _pendingStartClick = true,
                // The settings panel rebuilds after the map selection; clicking in the same frame
                // can see a button with no layout yet. Wait for it, then click once.
                Until = () =>
                {
                    if (_pendingStartClick)
                    {
                        Button start = Shell()?.Q<Button>("server-start");
                        if (start == null || !start.enabledInHierarchy || start.worldBound.width <= 0)
                            return false;
                        Click(start);
                        _pendingStartClick = false;
                    }
                    return Rounds().CurrentStationLoadState == StationLoadState.Failed
                        && Rounds().AdminSettingsStatus.Contains("Missing job spawn");
                },
                Verify = () =>
                {
                    Require(Rounds().CurrentRoundState == RoundState.Stopped && Rounds().CurrentTimerSeconds == 0
                        && !Spawned(), "Invalid map never reaches countdown or spawns players");
                    foreach (JobSlotInfo job in SubSystems.Get<RoleSubSystem>().Jobs)
                        Require(Rounds().AdminSettingsStatus.Contains(job.Name), $"Validation explains missing {job.Name} spawn");
                    Shell().Q<ScrollView>("server-settings-scroll").ScrollTo(Shell().Q("server-settings-status"));
                    Capture("server-settings-validation");
                    CreateValidationStation(valid: true);
                },
            },
            new Step
            {
                Name = "Configure a one-minute Extended round",
                Enter = () => Shell().Q<IntegerField>("server-round-length").value = 1,
                Until = () => Rounds().SettingsForServer.RoundLengthMinutes == 1,
            },
            new Step
            {
                Name = "Start countdown from the admin form",
                Enter = () => Click(Shell().Q<Button>("server-start")),
                Until = () => Rounds().CurrentRoundState == RoundState.WarmingUp && Shell().Q<Button>("server-extend").enabledInHierarchy,
                Verify = () =>
                {
                    Require(!Shell().Q<DropdownField>("server-map").enabledInHierarchy && !Shell().Q<Button>("server-start").enabledInHierarchy,
                        "Configuration and duplicate start disabled during countdown");
                    Require(_sawStationLoading, "Start Round showed a live loading indicator");
                    Require(Rounds().CurrentStationLoadState == StationLoadState.Ready
                        && SubSystems.Get<SS3D.Systems.Tile.TileSubSystem>().CurrentMap.ChunkCount > 0, "Selected map is restored and ready before countdown");
                    Require(Shell().Q<Label>("server-map-load-status").text.StartsWith("Map ready:"), "Admin form reports map ready");
                    Require(!Visible(Shell().Q("server-start")) && !Visible(Shell().Q("server-stop")), "Start and Stop are both hidden during countdown");
                    Capture("server-settings-countdown");
                },
            },
            new Step
            {
                Name = "Extend countdown by sixty seconds",
                Enter = () => Click(Shell().Q<Button>("server-extend")),
                Until = () => Rounds().CurrentTimerSeconds > 50,
                Verify = () => Require(true, "Extend Timer updates the authoritative countdown"),
            },
            new Step
            {
                Name = "Cancel countdown without spawning",
                Enter = () => Click(Shell().Q<Button>("server-cancel")),
                Until = () => Rounds().CurrentRoundState == RoundState.Stopped && Shell().Q<Button>("server-start").enabledInHierarchy,
                Verify = () => Require(!Spawned() && Rounds().CurrentTimerSeconds == 0, "Cancel restores waiting state without a spawn"),
            },
            new Step
            {
                Name = "Offer a second saved station for map switching",
                Enter = () =>
                {
                    _alternateStation = "lobby-pilot-" + Guid.NewGuid().ToString("N");
                    Require(SubSystems.Get<SS3D.Systems.Persistence.PersistenceSubSystem>().SaveStationTemplate(_alternateStation, false), "Created temporary alternate map fixture");
                    Rounds().CmdReadServerSettings();
                },
                Until = () => Shell().Q<DropdownField>("server-map").choices.Contains(_alternateStation),
            },
            new Step
            {
                Name = "Select a different map while an earlier station is loaded",
                Enter = () =>
                {
                    _previousStationEpoch = SubSystems.Get<SS3D.Systems.WorldReadiness.WorldReadinessSubSystem>().Epoch;
                    Shell().Q<DropdownField>("server-map").value = _alternateStation;
                },
                Until = () => Rounds().SettingsForServer.Map == _alternateStation,
                Verify = () => Require(Rounds().StationName == _validationStation, "Changing selection does not silently replace the loaded station"),
            },
            new Step
            {
                Name = "Start the configured Extended round",
                Enter = () => Click(Shell().Q<Button>("server-start")),
                Until = () => Rounds().CurrentRoundState == RoundState.Ongoing && Shell().Q<Button>("server-stop").enabledInHierarchy,
                Verify = () =>
                {
                    Require(SubSystems.Get<SS3D.Systems.Gamemodes.GamemodeSubSystem>().CurrentGamemodeName == "Extended", "Extended configuration reaches gamemode runtime");
                    Require(Rounds().StationName == _alternateStation
                        && SubSystems.Get<SS3D.Systems.WorldReadiness.WorldReadinessSubSystem>().Epoch > _previousStationEpoch,
                        "Next start restores the newly selected map in a fresh readiness epoch");
                    Require(!Visible(Shell().Q("server-start")) && Visible(Shell().Q("server-stop"))
                        && !Visible(Shell().Q("server-force")), "Only Stop Round is shown during an active round");
                    Require(Shell().Q<Label>("lobby-map-load-status").worldBound.height <= 20f,
                        "Long map names keep the sidebar status on one line");
                    Capture("server-settings-running");
                    Require(!ReadyButton().enabledInHierarchy, "Join Round is disabled by server policy");
                    Require(Rounds().CurrentTimerSeconds < 3, "Round elapsed clock excludes countdown time");
                },
            },
            new Step
            {
                Name = "Map edit during the round is tracked as unsaved",
                Enter = () => EditMapSpawn(remove: false),
                Until = () => UnsavedMapEdits() && MapEditSpawnPresent(),
                Verify = () => Require(SubSystems.Get<MapEditorSubSystem>().EditingTemplateName == _alternateStation,
                    "Round start made the loaded station the editing template"),
            },
            new Step
            {
                Name = "Stop asks about unsaved map changes",
                Enter = () => Click(Shell().Q<Button>("server-stop")),
                Until = () => Visible(Shell().Q("map-stop-prompt")),
                Verify = () =>
                {
                    Require(Rounds().CurrentRoundState == RoundState.Ongoing, "Prompt leaves the round running");
                    Require(!Visible(Shell().Q("map-stop-name")), "A known template is overwritten, so no name is asked");
                    Capture("map-stop-unsaved");
                },
            },
            new Step
            {
                Name = "Cancel keeps the round and the edits",
                Enter = () => Click(Shell().Q<Button>("map-stop-cancel")),
                Until = () => !Visible(Shell().Q("map-stop-prompt")) && Rounds().MapStopToken == 0,
                Verify = () => Require(Rounds().CurrentRoundState == RoundState.Ongoing && UnsavedMapEdits() && MapEditSpawnPresent(),
                    "Cancel keeps the round running with the edits"),
            },
            new Step
            {
                Name = "Attempt late join while policy forbids it",
                Enter = () => SubSystems.Get<EntitySubSystem>().CmdSpawnLatePlayer(Local()),
                Until = () => Rounds().CurrentTimerSeconds >= 2,
                Verify = () => Require(!Spawned(), "Server rejects late spawn even when the UI is bypassed"),
            },
            new Step
            {
                Name = "Reject configuration changes during an active round",
                Enter = () =>
                {
                    _repliesBefore = Rounds().AdminActionReplies;
                    ServerSettings settings = ServerSettings.Default;
                    settings.Map = _validationStation;
                    Rounds().CmdSaveServerSettings(settings);
                },
                Until = () => Rounds().AdminActionReplies > _repliesBefore && Rounds().AdminSettingsStatus.Contains("rejected"),
                Verify = () => Require(Rounds().SettingsForServer.RoundLengthMinutes == 1 && !Rounds().AllowLateJoin, "Active round configuration cannot change"),
            },
            new Step
            {
                Name = "MOTD text being typed survives admin settings refreshes",
                Enter = () =>
                {
                    _originalServerName = Rounds().ServerName;
                    _originalMotd = Rounds().Motd;
                    TextField motd = Shell().Q<TextField>("server-motd");
                    motd.Focus();
                    motd.value = PilotMotd;
                    // The lobby re-reads admin settings every second; stay focused across several refreshes.
                    _typingUntil = EditorApplication.timeSinceStartup + 2.5;
                },
                Until = () => EditorApplication.timeSinceStartup >= _typingUntil,
                Verify = () =>
                {
                    Require(Shell().Q<TextField>("server-motd").value == PilotMotd, "Uncommitted MOTD text is not overwritten while focused");
                    Require(Rounds().Motd == _originalMotd, "MOTD is not committed before focus loss");
                },
            },
            new Step
            {
                Name = "Edit server name and MOTD during an active round",
                Enter = () =>
                {
                    // Name/MOTD commit on focus loss, and the field must keep the new text while the periodic
                    // admin snapshot is still the old value.
                    Shell().Q<TextField>("server-motd").Blur();
                    TextField name = Shell().Q<TextField>("server-name");
                    name.Focus();
                    name.value = PilotServerName;
                    name.Blur();
                },
                Until = () => Rounds().ServerName == PilotServerName && Rounds().Motd == PilotMotd,
                Verify = () =>
                {
                    Require(Shell().Q<TextField>("server-motd").value == PilotMotd, "MOTD field keeps the committed text");
                    Require(LocalStorage.TryReadRaw(PersistencePaths.ServerInfo, out string saved)
                        && saved.Contains(PilotServerName) && saved.Contains("Line <b>two</b>"), "Server info is saved server-side");
                    Require(Rounds().PublicMode == "Extended", "Players see the configured mode");
                    Require(!string.IsNullOrEmpty(Rounds().ServerBuild) && Rounds().ServerStarted <= DateTimeOffset.UtcNow,
                        "Server build and start time are replicated");
                },
            },
            new Step
            {
                Name = "Configured round length ends the round",
                Timeout = 90,
                Until = () => Rounds().CurrentRoundState == RoundState.Stopped && Shell().Q<Button>("server-start").enabledInHierarchy,
                Verify = () =>
                {
                    Require(!Spawned(), "One-minute Extended round ended automatically without a late spawn");
                    Require(TemplateExists(_alternateStation + RoundSubSystem.AutosaveSuffix),
                        "Round end autosaved the unsaved map edits to a separate template");
                    Require(!UnsavedMapEdits() && SubSystems.Get<MapEditorSubSystem>().EditingTemplateName == null,
                        "Unloading the station resets the editor's history");
                },
            },
            new Step
            {
                Name = "Restore server defaults before lifecycle",
                Enter = () =>
                {
                    ServerSettings settings = ServerSettings.Default;
                    settings.Map = _validationStation;
                    Rounds().CmdSaveServerSettings(settings);
                },
                Until = () => Rounds().SettingsForServer.PlayerCap == 32 && Rounds().AllowLateJoin
                    && Shell().Q<Toggle>("server-late-join").value && Shell().Q<IntegerField>("server-round-length").value == 90,
            },
            new Step { Name = "Leave admin before lifecycle", Enter = () => Click(AdminButton()), Until = () => Shell().Q("server-settings-form") == null },
            new Step { Name = "Ready", Enter = () => Click(ReadyButton()), Until = () => IsReady() && ReadyButton().text.Contains("READY") && CountLabel() == "1/1 ready",
                Verify = () => Require(ReadyButton().text == "✓  READY", "Ready reflected in button and count") },
            new Step { Name = "Unready", Enter = () => Click(ReadyButton()), Until = () => !IsReady() && CountLabel() == "0/1 ready",
                Verify = () => Require(ReadyButton().text == "NOT READY", "Unready reflected in button and count") },
            new Step { Name = "Ready again", Enter = () => Click(ReadyButton()), Until = IsReady },
            new Step { Name = "Open Jobs tab", Enter = () => Click(Shell().Q<Button>("tab-jobs")), Until = () => SecurityChip("H") != null },
            new Step
            {
                Name = "Security High reaches the server",
                Enter = () => Click(SecurityChip("H")),
                // The shell reads replicated jobs on its 200ms refresh: wait on the UI, not only the server.
                Until = () => Job("Security").Interested == 1 && SecurityRowText().Contains("1 interested"),
                Verify = () =>
                {
                    Require(SecurityChip("H").ClassListContains("lobby-shell__priority-chip--high"), "Security H chip selected");
                    Require(Job("Assistant").Interested == 0, "Only Security counts as interested");
                    Require(SecurityRowText().Contains("1/1"), "Jobs tab shows Security slots");
                    Capture("jobs-security-high");
                },
            },
            new Step { Name = "Back to Server Info", Enter = () => Click(Shell().Q<Button>("tab-server-info")), Until = () => SecurityChip("H") == null },
            new Step
            {
                Name = "Server Info shows live metadata",
                Until = () => Shell().Q<Label>("server-info-motd")?.text == PilotMotd,
                Verify = () =>
                {
                    Require(!Shell().Q<Label>("server-info-motd").enableRichText, "MOTD renders tags literally");
                    Require(Shell().Query<Label>().ToList().Any(l => l.text == PilotServerName.ToUpperInvariant()), "Server Info title is the configured name");
                    Require(Shell().Q<Label>("server-info-meta").text.StartsWith("build " + Rounds().ServerBuild), "Server Info shows the server build");
                    Require(!Shell().Query<Label>().ToList().Any(l => l.text.Contains("USS Horizon") || l.text.Contains("0.14.2")),
                        "No mock server metadata remains");
                    Capture("server-info-live");
                },
            },
            new Step
            {
                Name = "Restore server name and MOTD",
                Enter = () =>
                {
                    ServerSettings original = Rounds().SettingsForServer;
                    original.ServerName = _originalServerName;
                    original.Motd = _originalMotd;
                    Rounds().CmdUpdateServerSetting(ServerSetting.ServerName, original);
                    Rounds().CmdUpdateServerSetting(ServerSetting.Motd, original);
                },
                Until = () => Rounds().ServerName == _originalServerName && Rounds().Motd == _originalMotd,
            },
            new Step { Name = "Open Chat tab", Enter = () => Click(Shell().Q<Button>("tab-chat")), Until = () => ChatInput() != null },
            new Step
            {
                Name = "B typed in lobby chat stays out of the map editor",
                Enter = () =>
                {
                    ChatInput().Focus();
                    TapKey(Key.B);
                    Settle(1);
                },
                Until = Settled,
                Verify = () =>
                {
                    Require(InputInterface.IsCapturingText, "Focused chat field holds text entry");
                    Require(!MapEditorActive(), "B in chat did not open the map editor");
                },
            },
            new Step
            {
                Name = "OOC line round-trips through the server into the chat log",
                Enter = () => SubSystems.Get<CommsSubSystem>().SendOoc(OocChannel.Ooc, "  host <b>probe</b>\n line  "),
                Until = () => ChatLines().Contains("host <b>probe</b> line"),
                Verify = () =>
                {
                    OocChatMessage line = SubSystems.Get<CommsSubSystem>().OocLog.Last(m => !m.IsSystem);
                    Require(line.Sender == SS3D.Core.Settings.LocalPlayer.Ckey, "Server attributes the line to the sender's ckey");
                    Require(Shell().Query<Label>(className: "lobby-shell__chat-text").ToList().All(l => !l.enableRichText),
                        "Chat text renders tags literally");
                    Require(Shell().Q<Button>("chat-channel-admin") != null, "Administrator sees the Admin channel");
                    Capture("chat-ooc");
                },
            },
            new Step
            {
                Name = "LOOC without a body explains why it was not sent",
                Enter = () =>
                {
                    Click(Shell().Q<Button>("chat-channel-looc"));
                    SubSystems.Get<CommsSubSystem>().SendOoc(OocChannel.Looc, "nobody hears this");
                },
                Until = () => SubSystems.Get<CommsSubSystem>().OocLog.Any(m => m.Channel == OocChannel.Looc && m.IsSystem),
                Verify = () => Require(!SubSystems.Get<CommsSubSystem>().OocLog.Any(m => m.Text == "nobody hears this"),
                    "Unspawned LOOC was not delivered"),
            },
            new Step
            {
                Name = "Leaving Chat releases text entry",
                Enter = () => Click(Shell().Q<Button>("tab-server-info")),
                Until = () => ChatInput() == null,
                Verify = () => Require(!InputInterface.IsCapturingText, "Text entry released when the chat field is rebuilt away"),
            },
            new Step
            {
                Name = "Open creator before round start",
                Enter = () => Click(Shell().Q(className: "lobby-shell__preview")),
                Until = () => Visible(Creator()) && !Visible(Shell()),
            },
            new Step
            {
                Name = "Species menu shows unavailable models disabled",
                Enter = () =>
                {
                    DropdownField species = Creator().Q<DropdownField>("character-species");
                    Require(species.value == "Human" && species.choices.SequenceEqual(new string[] { "Human" }), "Human is the only selectable species");
                    // Untargeted navigation events go to the focused element, so focus and target the field.
                    species.Focus();
                    using NavigationSubmitEvent evt = NavigationSubmitEvent.GetPooled();
                    evt.target = species;
                    species.SendEvent(evt);
                },
                Until = () => Creator().panel.visualTree.Query<Label>().ToList().Any(l => l.text == "Skrell (unavailable)"),
                Verify = () =>
                {
                    foreach (string species in new string[] { "Skrell", "Vox" })
                    {
                        Label label = Creator().panel.visualTree.Query<Label>().ToList().First(l => l.text == species + " (unavailable)");
                        Require(!label.enabledInHierarchy, species + " menu item is disabled");
                    }
                    Capture("species-unavailable");
                },
            },
            new Step
            {
                Name = "Close species menu",
                Enter = () =>
                {
                    Label human = Creator().panel.visualTree.Query<Label>().ToList().Last(l => l.text == "Human");
                    using NavigationCancelEvent evt = NavigationCancelEvent.GetPooled();
                    evt.target = human;
                    human.SendEvent(evt);
                },
                Until = () => !Creator().panel.visualTree.Query<Label>().ToList().Any(l => l.text == "Skrell (unavailable)"),
            },
            new Step
            {
                Name = "Creator selector receives a rendered portrait",
                Until = () => Creator()?.Q(className: "char-creator__loadout-thumb")?.resolvedStyle.backgroundImage.renderTexture != null,
                Verify = () =>
                {
                    CharacterPreviewBooth booth = UnityEngine.Object.FindAnyObjectByType<CharacterPreviewBooth>();
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture sentinel = RenderTexture.GetTemporary(16, 16, 0);
                    RenderTexture portrait = RenderTexture.GetTemporary(128, 128, 0);
                    try
                    {
                        RenderTexture.active = sentinel;
                        Require(booth.TryCapturePortrait(portrait), "Front preview produces a selector portrait");
                        Require(RenderTexture.active == sentinel, "Portrait capture restores the active render texture");
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                        RenderTexture.ReleaseTemporary(sentinel);
                        RenderTexture.ReleaseTemporary(portrait);
                    }
                    Capture("character-selector-portrait");
                },
            },
            new Step
            {
                Name = "Creator: name",
                Enter = () =>
                {
                    Creator().Q<TextField>("character-name").value = PilotCharacterName;
                    Click(FindButton(Creator(), "NEXT"));
                },
                Until = () => Creator().Q<Slider>("body-height") != null,
            },
            new Step
            {
                Name = "Creator: body",
                Enter = () =>
                {
                    Creator().Q<Slider>("body-height").value = PilotHeight;
                    Creator().Q<Slider>("body-fat").value = PilotFat;
                    Click(Swatch("skin-color-picker", 5));
                },
                // Each swatch click rebuilds the step; wait for the new elements to be laid out.
                Until = () => SwatchSelectedAndLaidOut("skin-color-picker", 5),
            },
            new Step
            {
                Name = "Creator: eye color",
                Enter = () => Click(Swatch("eye-color-picker", 1)),
                Until = () => SwatchSelectedAndLaidOut("eye-color-picker", 1),
            },
            new Step
            {
                Name = "Creator: body → style",
                Enter = () => Click(FindButton(Creator(), "NEXT")),
                Until = () => StyleTiles().Count > 1,
            },
            new Step
            {
                Name = "Creator: first hair style",
                Enter = () => Click(StyleTiles()[1]),
                Until = () => StyleTiles()[1].ClassListContains("char-creator__style-tile--selected"),
            },
            new Step
            {
                Name = "Creator: eyebrows",
                Enter = () => Click(FindButton(Creator(), "EYEBROWS")),
                Until = () => Creator().Q("eyebrows-humaneyebrowsbold")?.worldBound.width > 0,
            },
            new Step
            {
                Name = "Creator: eyebrow style",
                Enter = () => Click(Creator().Q("eyebrows-humaneyebrowsbold")),
                Until = () => Swatch("style-color-picker", 4)?.worldBound.width > 0,
            },
            new Step
            {
                Name = "Creator: eyebrow color",
                Enter = () => Click(Swatch("style-color-picker", 4)),
                Until = () => SwatchSelectedAndLaidOut("style-color-picker", 4),
                Verify = () =>
                {
                    CharacterPreviewBooth booth = UnityEngine.Object.FindAnyObjectByType<CharacterPreviewBooth>();
                    VerifyFace(booth.Dummy);
                    Capture("creator-face");
                },
            },
            new Step { Name = "Creator: review", Enter = () => Click(FindButton(Creator(), "NEXT")), Until = () => FindButton(Creator(), "SAVE CHARACTER") != null },
            new Step
            {
                Name = "Save character",
                Enter = () => Click(FindButton(Creator(), "SAVE CHARACTER")),
                Until = () => FindButton(Creator(), "SAVED") != null,
                Verify = () =>
                {
                    _savedName = CharacterName();
                    Require(string.Equals(_savedName, PilotCharacterName, StringComparison.OrdinalIgnoreCase), "Lobby sidebar shows the saved name");
                    CharacterProfileStore.Profile profile = new CharacterProfileStore(CharacterProfileStore.DefaultPath).Load();
                    Require(profile != null, "Save writes a readable character profile to disk");
                    CharacterCreatorDraft saved = profile.Characters.First(c => c.Id == profile.SelectedId);
                    Require(saved.Name == PilotCharacterName && Mathf.Approximately(saved.GetMorph("height"), PilotHeight)
                        && Mathf.Approximately(saved.GetMorph("fat"), PilotFat) && saved.HairStyleIndex == 1 && saved.SkinColorIndex == 5 && saved.EyeColorIndex == 1
                        && saved.Eyebrows == "humaneyebrowsbold" && saved.EyebrowColorIndex == 4,
                        "Reloaded selected character preserves name, body and hair");
                },
            },
            new Step
            {
                Name = "Round start spawns ready player and hides lobby",
                Enter = () => InstanceFinder.ClientManager.Broadcast(new ChangeRoundStateMessage(true)),
                Timeout = 120,
                Until = () => Rounds().CurrentRoundState == RoundState.Ongoing && Spawned() && !Visible(Shell()) && !Visible(Creator()),
                Verify = () =>
                {
                    Require(!BoothLive(), "Booth deactivated while in world");
                    Require(AssignedRole() == "Security", "Round start resolved the High preference to Security");
                    Require(Job("Security").Assigned == 1, "Security slot count replicated as taken");
                    VerifySpawnedAppearance();
                    Capture("spawned-appearance");
                },
            },
            new Step
            {
                Name = "Escape opens the lobby over the spawned body",
                Enter = () => TapKey(Key.Escape),
                Until = () => Visible(Shell()),
                Verify = () =>
                {
                    Require(Spawned(), "Still spawned while the menu is open");
                    Require(BoothLive(), "Booth live while the menu is open");
                    Require(!ReadyButton().enabledInHierarchy && ReadyButton().text == "IN ROUND", "Action button reads IN ROUND, disabled");
                    Require(!GameplayMapEnabled(), "Movement input masked while the menu is open");
                    Capture("escape-menu");
                },
            },
            new Step
            {
                Name = "Escape closes the in-round menu",
                Enter = () => TapKey(Key.Escape),
                Until = () => !Visible(Shell()),
                Verify = () =>
                {
                    Require(!BoothLive(), "Booth deactivated after closing the menu");
                    Require(GameplayMapEnabled(), "Movement input restored after closing the menu");
                },
            },
            new Step
            {
                Name = "Unsaved map edit before a player stop request",
                Enter = () => EditMapSpawn(remove: false),
                Until = UnsavedMapEdits,
            },
            new Step
            {
                Name = "Player stop request asks before unloading edits",
                Enter = () => InstanceFinder.ClientManager.Broadcast(new ChangeRoundStateMessage(false)),
                Until = () => Rounds().MapStopToken != 0,
                Verify = () => Require(Rounds().CurrentRoundState == RoundState.Ongoing, "Stop waits for the admin's choice"),
            },
            // Guards: the round-end loop end to end in one host: the server's outcome reaches the overlay over a
            // living player, the admin hold reaches the UI, the beat returns everyone to the lobby and the Last Round
            // tab keeps the outcome (2026-11_round-end-loop P1) · Not cheaper because: overlay, shell and round state
            // only meet in a running host; outcome rules, beat and hold are EditMode (RoundEndLoopTests). Screenshots
            // are the visual review of the summary, banner, held state and Last Round tab (tier 4).
            new Step
            {
                Name = "Round end shows the summary over the living player",
                Enter = () =>
                {
                    // The real beat, so the countdown and bar look as players see them; the captures below happen
                    // while held, and the resumed beat is the one ~30 s this pilot pays for the summary.
                    Rounds().SetEndBeatSecondsForTesting(RoundEndBeat.Seconds);
                    Rounds().CmdResolveMapStop(Rounds().MapStopToken, MapStopChoice.DiscardAndStop, "");
                },
                Until = () => Rounds().CurrentRoundState == RoundState.Ending && Shown(RoundEndPart("round-end-panel")),
                Verify = () =>
                {
                    RoundOutcome outcome = Rounds().LastOutcome;
                    Require(outcome != null && outcome.RoundId == Rounds().RoundId
                        && outcome.Players.Any(p => p.Ckey == Local().Ckey && p.Alive), "The outcome lists the host, alive");
                    Require(Shown(RoundEndPart("round-end-row-you")), "The viewer's own row is highlighted");
                    Require(Spawned() && !Visible(Shell()), "The player keeps their body and the world during the beat");
                    Capture("round-end-summary");
                    Rounds().CmdRoundAdminAction(RoundAdminAction.HoldEnd);
                },
            },
            new Step
            {
                Name = "Admin hold shows on the summary",
                Until = () => Rounds().EndHeld && Shown(RoundEndPart("round-end-paused")),
                Verify = () => Capture("round-end-held"),
            },
            new Step
            {
                Name = "Tab collapses the summary to a banner",
                Enter = () => RoundEndUi().ToggleCollapsed(),
                Until = () => Shown(RoundEndPart("round-end-banner")) && !Shown(RoundEndPart("round-end-panel")),
                Verify = () => Capture("round-end-banner"),
            },
            new Step
            {
                Name = "Resumed summary returns to lobby shell",
                Timeout = 60,
                Enter = () =>
                {
                    Rounds().SetEndBeatSecondsForTesting(PilotEndBeatSeconds);
                    Rounds().CmdRoundAdminAction(RoundAdminAction.ResumeEnd);
                },
                Until = () => Rounds().CurrentRoundState == RoundState.Stopped && !Spawned() && Visible(Shell()),
                Verify = () =>
                {
                    Require(!Shown(RoundEndPart("round-end-panel")) && !Shown(RoundEndPart("round-end-banner")),
                        "The summary overlay is gone in the lobby");
                    Require(!Visible(Creator()), "Return shows shell, not creator");
                    Require(BoothLive(), "Booth restored with RT bound");
                    Require(CharacterName() == _savedName, "Saved character name restored");
                    Require(AssignedRole() == null && Job("Security").Assigned == 0, "Round stop cleared job assignments");
                    Require(!TemplateExists(_validationStation + RoundSubSystem.AutosaveSuffix), "Discard and stop left no autosave");
                },
            },
            new Step
            {
                Name = "Last Round tab shows the ended round",
                Enter = () => Click(Shell().Q<Button>("tab-last-round")),
                Until = () => Shown(Shell().Q("last-round")) && Shown(Shell().Q("last-round").Q("round-end-row-you")),
                Verify = () => Capture("lobby-last-round"),
            },
            new Step { Name = "Ensure unready for late join", Enter = () => { if (IsReady()) Click(ReadyButton()); }, Until = () => !IsReady() },
            new Step { Name = "Open admin tab", Enter = () => Click(AdminButton()), Until = () => Shell().Q<Button>("server-start")?.enabledInHierarchy == true },
            new Step
            {
                Name = "Start countdown for Force Start",
                Enter = () => Click(FindButton(Shell(), "START ROUND")),
                Until = () => Rounds().CurrentRoundState == RoundState.WarmingUp && Shell().Q<Button>("server-force").enabledInHierarchy,
            },
            new Step
            {
                Name = "Admin Force Start leaves unready player in lobby",
                Enter = () => Click(Shell().Q<Button>("server-force")),
                Timeout = 120,
                Until = () => Rounds().CurrentRoundState == RoundState.Ongoing && ReadyButton().text == "JOIN ROUND",
                Verify = () =>
                {
                    Require(!Spawned() && Visible(Shell()), "Unready player stays in lobby");
                    Require(ReadyButton().enabledInHierarchy, "Join Round enabled while Ongoing");
                },
            },
            new Step
            {
                Name = "Late join spawns",
                Enter = () => Click(ReadyButton()),
                Until = () => Spawned() && !Visible(Shell()),
                Verify = () =>
                {
                    Require(!BoothLive(), "Booth deactivated after late join");
                    Require(AssignedRole() == "Security", "Late join took the highest ranked open job");
                    VerifySpawnedAppearance();
                },
            },
            new Step
            {
                Name = "Unsaved map edit before Save and stop",
                Enter = () =>
                {
                    _templateWriteTime = TemplateWriteTime(_validationStation);
                    EditMapSpawn(remove: false);
                },
                Until = UnsavedMapEdits,
            },
            new Step
            {
                Name = "Stop after late join asks first",
                Enter = () => InstanceFinder.ClientManager.Broadcast(new ChangeRoundStateMessage(false)),
                Until = () => Rounds().MapStopToken != 0,
            },
            new Step
            {
                Name = "Stop round after late join returns",
                Enter = () => Rounds().CmdResolveMapStop(Rounds().MapStopToken, MapStopChoice.SaveAndStop, ""),
                Until = () => Rounds().CurrentRoundState == RoundState.Stopped && !Spawned() && Visible(Shell()),
                Verify = () =>
                {
                    Require(BoothLive(), "Booth restored after second return");
                    Require(TemplateWriteTime(_validationStation) > _templateWriteTime, "Save and stop overwrote the editing template");
                    Require(!TemplateExists(_validationStation + RoundSubSystem.AutosaveSuffix), "Saved edits need no autosave");
                },
            },
            // New connections 60s+ after server start used to time out before their first packet.
            new Step { Name = "Wait past client timeout window", Timeout = 90, Until = () => Elapsed() > 70 },
            new Step
            {
                Name = "Client disconnect hides lobby",
                Enter = () =>
                {
                    // On purpose: a lost host session would otherwise go to the main menu and stop the server.
                    SS3D.Networking.ClientConnectionRecovery.FailureUiOwnedElsewhere = true;
                    InstanceFinder.ClientManager.StopConnection();
                },
                Until = () => !InstanceFinder.ClientManager.Started && !Visible(Shell()) && !Visible(Creator()),
                Verify = () => Require(!BoothLive(), "Booth deactivated offline"),
            },
            new Step
            {
                Name = "Client reconnect restores lobby",
                Timeout = 60,
                // A start issued while Tugboat is still stopping is dropped.
                Until = () => RetryConnection() && Online() && Local() != null && Visible(Shell()) && CountLabel() == "0/1 ready" && ReadyButton().enabledInHierarchy,
                Verify = () =>
                {
                    SS3D.Networking.ClientConnectionRecovery.FailureUiOwnedElsewhere = false;
                    Require(BoothLive(), "Booth restored after reconnect");
                    Require(CountLabel() == "0/1 ready", "Player list repopulated after reconnect");
                    Require(ReadyButton().enabledInHierarchy, "Ready usable after reconnect");
                    Capture("lobby-after-reconnect");
                },
            },
            new Step { Name = "Ready after reconnect", Enter = () => Click(ReadyButton()), Until = IsReady },
        };

        // The client script (lobby-remote-client.txt) asserts its own UI; these steps drive the admin side
        // and check the server's view. Each wait mirrors a client step, so a client failure surfaces here too.
        private static Step[] BuildRemoteSteps() => new Step[]
        {
            new Step
            {
                Name = "Host lobby ready",
                Timeout = 180,
                Until = () => Online() && Local() != null && Rounds() != null && Visible(Shell()) && Rounds().CurrentRoundState == RoundState.Stopped
                    && CountLabel() == "0/1 ready",
                Verify = () =>
                {
                    Require(AdminButton().enabledInHierarchy, "Host owner has Admin enabled");
                    CreateValidationStation(valid: true);
                    ServerSettings settings = ServerSettings.Default;
                    settings.Map = _validationStation;
                    Rounds().CmdSaveServerSettings(settings);
                    Require(!SubSystems.Get<PermissionSubSystem>().IsAtLeast(RemoteCkey, ServerRoleTypes.Administrator), $"'{RemoteCkey}' has no Administrator permission");
                    _bugLines = JsonlLines(SS3D.Data.Persistence.PersistencePaths.BugReports).Length;
                },
            },
            new Step
            {
                Name = "Remote client joins lobby",
                Timeout = 120,
                Enter = LaunchClient,
                Until = () => ClientAlive() && RemotePlayer() != null && CountLabel() == "0/2 ready",
                Verify = () =>
                {
                    Require(!RemoteSpawned(), "Remote player joins unspawned");
                    Require(Rounds().CurrentStationLoadState == StationLoadState.NotLoaded
                        && SubSystems.Get<SS3D.Systems.Tile.TileSubSystem>().CurrentMap.ChunkCount == 0,
                        "Remote joins the lobby before a station is loaded");
                },
            },
            new Step
            {
                Name = "Remote non-admin start ignored, remote readies via UI",
                // The client's real OS click can take a while to reach the server on a busy runner.
                Timeout = 180,
                Until = () => ClientAlive() && RemoteReady() && CountLabel() == "1/2 ready",
                Verify = () =>
                {
                    Require(Rounds().CurrentRoundState == RoundState.Stopped, "Non-admin start request did not start a round");
                    Require(ClientLog().Contains("ServerSettingsPermissionVerified"), "Remote cannot read admin configuration");
                    Require(Rounds().SettingsForServer.PlayerCap == 32 && Rounds().AllowLateJoin, "Non-admin configuration writes were rejected");
                    Require(ClientSignal("UiText", ".lobby-shell__countdown-sub Stopped"), "Client observed Stopped after its start request");
                    Require(!IsReady(), "Host stays unready");
                    Require(_osInputsDone >= 1 && !ClientLog().Contains("UiSubmitted"), "Remote readied through a real OS click, not a synthetic submit");
                },
            },
            new Step
            {
                Name = "Remote sends chat with real keyboard input",
                // The client script files its bug-report dialog (including a cooldown reopen) before
                // the chat line; this step waits through that whole sequence.
                Timeout = 240,
                Until = () => ClientAlive() && ClientLog().Contains("Chat send (") && ClientLog().Contains(ChatProbe)
                    && HostChatContains(ChatProbe),
                Verify = () =>
                {
                    Require(_osInputsDone >= 5, "Chat tab click, field click, typing and Return performed as OS input");
                    OocChatMessage line = SubSystems.Get<CommsSubSystem>().OocLog.Last(m => m.Text == ChatProbe);
                    Require(line.Channel == OocChannel.Ooc && line.Sender == "remoteprobe" && !line.IsSystem,
                        "Host received the remote line on OOC, attributed to the sending connection's ckey");
                    Require(JsonlLines(SS3D.Data.Persistence.PersistencePaths.BugReports).Length == _bugLines + 1,
                        "Remote dialog saved exactly one report despite reopening during cooldown");
                    BugReportEntry bug = LastJsonl<BugReportEntry>(SS3D.Data.Persistence.PersistencePaths.BugReports);
                    Require(bug.source == "dialog" && bug.ckey == RemoteCkey && bug.roundId == 0 && bug.state == "lobby",
                        "Server attributed the remote dialog report to its connection, round and state");
                    Require(bug.note == "remote pilot bug report"
                        && bug.details == "reproduced from the remote client with real keyboard input."
                        && bug.client != null && bug.client.networkRole == "Client" && bug.client.context == "LOBBY",
                        "Remote summary, details and captured context survived the broadcast");
                    Require(ClientSignal("UiText", "#toast-text Bug report saved (round 0). Thanks!"),
                        "Remote received the server acknowledgement");
                    Require(ClientSignal("UiText", "#rate-limit You can send another report in"),
                        "Remote reopening showed the rate-limit countdown");
                    string screenshot = SS3D.Data.Paths.GetPath(SS3D.Data.GamePaths.Data, true)
                        + SS3D.Data.Persistence.PersistencePaths.BugScreenshots + "/" + bug.screenshot;
                    Require(!string.IsNullOrEmpty(bug.screenshot) && File.Exists(screenshot), "Server saved the remote screenshot");
                    byte[] jpg = File.ReadAllBytes(screenshot);
                    Require(jpg.Length > 0 && jpg.Length <= SS3D.Systems.BugReports.BugReportText.MaxScreenshotBytes,
                        "Remote JPEG respects the network size limit");
                    Texture2D image = new(2, 2);
                    try
                    {
                        Require(image.LoadImage(jpg) && image.width <= 960 && image.height > 0,
                            "Remote screenshot decodes as a reduced image");
                    }
                    finally { UnityEngine.Object.Destroy(image); }
                },
            },
            new Step { Name = "Open admin tab", Enter = () => Click(AdminButton()), Until = () => Shell().Q<Button>("server-start")?.enabledInHierarchy == true },
            new Step
            {
                Name = "Admin Start Round spawns ready remote only",
                Enter = () => Click(FindButton(Shell(), "START ROUND")),
                Timeout = 120,
                // UI snapshots trail round state by the 200ms refresh: wait for the label too.
                Until = () => Rounds().CurrentRoundState == RoundState.Ongoing && RemoteSpawned() && ReadyButton().text == "JOIN ROUND",
                Verify = () =>
                {
                    Require(!Spawned() && Visible(Shell()), "Unready host stays in lobby");
                    Require(ReadyButton().text == "JOIN ROUND", "Host shows Join Round while Ongoing");
                    Require(!Visible(Shell().Q("server-start")) && Visible(Shell().Q("server-stop")), "Host shows Stop without Start while remote is in round");
                },
            },
            new Step
            {
                Name = "Admin Stop Round returns remote",
                Enter = () => Click(FindButton(Shell(), "STOP ROUND")),
                Until = () => Rounds().CurrentRoundState == RoundState.Stopped && !RemoteSpawned() && CountLabel() == "0/2 ready"
                    && Shell().Q<Button>("server-start").enabledInHierarchy,
            },
            // The client must see Stopped and its shell before the next round starts.
            new Step { Name = "Remote shell restored", Timeout = 60, Until = () => !ClientAlive() || ClientLog().Contains("remote-04-returned.png") },
            new Step
            {
                Name = "Second round: remote late-joins via Join Round",
                Enter = () => Click(FindButton(Shell(), "START ROUND")),
                Timeout = 120,
                Until = () => Rounds().CurrentRoundState == RoundState.Ongoing && RemoteSpawned(),
                Verify = () => Require(ClientSignal("UiText", ".lobby-shell__btn--ready JOIN ROUND"), "Remote saw Join Round before spawning"),
            },
            new Step
            {
                Name = "Stop second round",
                Enter = () => Click(FindButton(Shell(), "STOP ROUND")),
                Until = () => Rounds().CurrentRoundState == RoundState.Stopped && !RemoteSpawned()
                    && Shell().Q<Button>("server-start").enabledInHierarchy,
            },
            new Step
            {
                Name = "Remote script completes and disconnects",
                Timeout = 90,
                // PlayerSubSystem keeps offline players, so wait on the online count, not the record.
                Until = () => ClientLog().Contains("Test signal \"ScriptComplete\"") && CountLabel() == "0/1 ready",
                Verify = () =>
                {
                    string log = ClientLog();
                    Require(!log.Contains("Test signal \"ScriptFailed\""), "Remote script did not fail");
                    // Environment.Exit can stall in a windowed player's teardown (the harness kills leftovers too).
                    Timeline.Add(_client.HasExited ? $"client exited with code {_client.ExitCode}" : "client still tearing down after ScriptComplete; killed");
                    string[] remoteErrors = log.Split('\n').Where(line => line.Contains("Exception") || line.Contains("[Error]") || line.Contains("[ERR]")).ToArray();
                    foreach (string line in remoteErrors) Errors.Add("[remote] " + line);
                    Capture("host-after-remote");
                },
            },
        };

        private static string OutputDir => SessionState.GetString(OutputKey, "");

        private static string ClientLogPath => Path.Combine(OutputDir, "client.log");

        private static void LaunchClient()
        {
            ushort port = InstanceFinder.TransportManager.Transport.GetPort();
            System.Diagnostics.ProcessStartInfo info = new()
            {
                FileName = Path.GetFullPath(ClientBinary),
                WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(ClientBinary)),
                Arguments = $"-ip=127.0.0.1 -port={port} -ckey={RemoteCkey} -skipintro -testscript=\"{Path.GetFullPath(RemoteScenario)}\" "
                    + $"-screen-fullscreen 0 -screen-width 1600 -screen-height 900 -logFile \"{ClientLogPath}\"",
                UseShellExecute = false,
            };
            info.Environment["SS3D_AUTOMATION_OUTPUT_DIR"] = OutputDir;
            // Isolate and pin the player's resolution. The shared PlayerPrefs can leave the client
            // fullscreen at the native 1920x1080 (changed by other runs), and the OsClick coordinates
            // are in the client's UI space: a window whose size differs from the render makes every
            // real input land off-target. XDG_CONFIG_HOME redirects only this process's prefs.
            string configRoot = Path.Combine(OutputDir, "config");
            string prefsDir = Path.Combine(configRoot, "unity3d", "CentCom Softworks", "SS3D");
            Directory.CreateDirectory(prefsDir);
            File.WriteAllText(Path.Combine(prefsDir, "prefs"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                + "<unity_prefs version_major=\"1\" version_minor=\"1\">\n"
                + "  <pref name=\"Screenmanager Resolution Width\" type=\"int\">1600</pref>\n"
                + "  <pref name=\"Screenmanager Resolution Height\" type=\"int\">900</pref>\n"
                + "  <pref name=\"Screenmanager Resolution Use Native\" type=\"int\">0</pref>\n"
                + "  <pref name=\"Screenmanager Fullscreen mode\" type=\"int\">3</pref>\n"
                + "  <pref name=\"Screenmanager Is Fullscreen mode\" type=\"int\">0</pref>\n"
                + "  <pref name=\"Screenmanager Window Position X\" type=\"int\">0</pref>\n"
                + "  <pref name=\"Screenmanager Window Position Y\" type=\"int\">0</pref>\n"
                + "</unity_prefs>\n");
            // PlayerSettings defaults to Borderless/native, which ignores the window size flags and
            // makes the render 1920x1080. Seed a windowed 1600x900 settings file in the isolated config.
            string settingsDir = Path.Combine(prefsDir, "Settings");
            Directory.CreateDirectory(settingsDir);
            File.WriteAllText(Path.Combine(settingsDir, "settings.json"),
                "{\"Version\":1,\"Display\":{\"ResolutionWidth\":1600,\"ResolutionHeight\":900,\"WindowMode\":2}}");
            info.Environment["XDG_CONFIG_HOME"] = configRoot;
            _client = System.Diagnostics.Process.Start(info);
            Timeline.Add($"{Elapsed():0.0}s launched client pid {_client.Id} on port {port}");
        }

        private static bool ClientAlive()
        {
            if (_client == null) return false;
            if (_client.HasExited && _index < _steps.Length - 1)
                throw new InvalidOperationException($"Remote client exited early (code {_client.ExitCode}). See {ClientLogPath}");
            return !_client.HasExited;
        }

        // The client writes its log while running; open shared to read it mid-run.
        private static string ClientLog()
        {
            if (!File.Exists(ClientLogPath)) return "";
            using FileStream stream = new(ClientLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        // TestSignal renders through Serilog, which quotes string properties.
        private static bool ClientSignal(string signal, string payload) =>
            ClientLog().Contains($"Test signal \"{signal}\" \"{payload}\"");

        private static Player RemotePlayer() => SubSystems.TryGet(out PlayerSubSystem players) ? players.GetPlayer(RemoteCkey) : null;

        private static bool RemoteReady() => RemotePlayer() != null && SubSystems.TryGet(out ReadyPlayersSubSystem ready) && ready.IsReady(RemotePlayer());

        private static bool RemoteSpawned() => RemotePlayer() != null && SubSystems.TryGet(out EntitySubSystem entities) && entities.IsPlayerSpawned(RemotePlayer());

        // The client emits OsClick "x y selector" / OsType "text" / OsKey "keysym"; perform each once, in order,
        // as real OS input from a virtual uinput device. Clicking focuses the window, so typing goes to it.
        private static void PerformOsInput()
        {
            if (_client == null || _client.HasExited) return;
            System.Text.RegularExpressions.MatchCollection requests = OsRequest.Matches(ClientLog());
            for (; _osInputsDone < requests.Count; _osInputsDone++)
            {
                string kind = requests[_osInputsDone].Groups[1].Value;
                string payload = requests[_osInputsDone].Groups[2].Value;
                _clientWindow ??= ClientWindow();
                string[] parts = payload.Split(' ');
                UinputInput(kind switch
                {
                    "OsClick" => $"click {_clientWindow} {parts[0]} {parts[1]}",
                    "OsType" => $"type {payload}",
                    _ => $"key {payload}",
                });
                Timeline.Add($"{Elapsed():0.0}s OS input {kind} {payload}");
            }
        }

        private static readonly System.Text.RegularExpressions.Regex OsRequest = new("Test signal \"(OsClick|OsType|OsKey)\" \"(.*)\"");

        private static string ClientWindow()
        {
            string[] windows = Xdotool($"search --onlyvisible --pid {_client.Id}").Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (windows.Length == 0) throw new InvalidOperationException($"No visible X11 window for client pid {_client.Id}");
            string window = windows[^1].Trim();
            // The client's frame can sit behind the editor; put it at the origin so the OsClick
            // coordinates are root coordinates too. uinput_input.py raises the frame per click.
            Xdotool($"windowmove {window} 0 0");
            return window;
        }

        // Window lookup only: xdotool's XTEST input would raise GNOME's remote-interaction portal dialog every run.
        private static string Xdotool(string arguments)
        {
            System.Diagnostics.ProcessStartInfo info = new("xdotool", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            using System.Diagnostics.Process process = System.Diagnostics.Process.Start(info);
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(10000) || process.ExitCode != 0)
                throw new InvalidOperationException($"xdotool {arguments} failed: {error}");
            return output;
        }

        // `Testing/ui/uinput_input.py serve`: one kernel-level virtual device for the whole run, no portal dialog.
        // Keep it alive: keys from a second, new device did not reach the window the first one clicked.
        // Needs write access to /dev/uinput and an X display (Unity under XWayland on DISPLAY=:0); moves the real pointer.
        private static void UinputInput(string command)
        {
            if (_input == null || _input.HasExited)
            {
                string script = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Testing", "ui", "uinput_input.py"));
                System.Diagnostics.ProcessStartInfo info = new("python3")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };
                info.ArgumentList.Add("-I");
                info.ArgumentList.Add(script);
                info.ArgumentList.Add("serve");
                _input = System.Diagnostics.Process.Start(info);
                string ready = _input.StandardOutput.ReadLine();
                if (ready != "ready") throw new InvalidOperationException($"uinput_input.py serve did not start: {ready ?? "exited (see editor log)"}");
            }
            _input.StandardInput.WriteLine(command);
            string reply = _input.StandardOutput.ReadLine();
            if (reply == null || !reply.StartsWith("ok ", StringComparison.Ordinal))
                throw new InvalidOperationException($"uinput_input.py {command}: {reply ?? "exited (see editor log)"}");
        }

        private static void StopClient()
        {
            if (_client != null && !_client.HasExited) _client.Kill();
            _client = null;
            if (_input != null)
            {
                // Closing stdin ends serve, which destroys the virtual device.
                if (!_input.HasExited) _input.StandardInput.Close();
                if (!_input.WaitForExit(3000)) _input.Kill();
                _input.Dispose();
                _input = null;
            }
        }

        private static double _nextRetry;

        private static bool RetryConnection()
        {
            if (!Online() && EditorApplication.timeSinceStartup >= _nextRetry)
            {
                _nextRetry = EditorApplication.timeSinceStartup + 2;
                InstanceFinder.ClientManager.StartConnection();
            }

            return true;
        }

        private static bool Online() => InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Started;

        private static Player Local() => Online() && SubSystems.TryGet(out PlayerSubSystem players)
            ? players.GetPlayer(InstanceFinder.ClientManager.Connection) : null;

        private static RoundSubSystem Rounds() => SubSystems.TryGet(out RoundSubSystem rounds) ? rounds : null;

        private static bool IsReady() => Local() != null && SubSystems.TryGet(out ReadyPlayersSubSystem ready) && ready.IsReady(Local());

        private static bool Spawned() => Online() && SubSystems.TryGet(out EntitySubSystem entities)
            && entities.IsPlayerSpawned(InstanceFinder.ClientManager.Connection);

        private static VisualElement Root() => UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
            .Select(d => d.rootVisualElement)
            .FirstOrDefault(r => r != null && r.Q("lobby-shell-surface") != null);

        private static RoundEndUiSubSystem RoundEndUi() => SubSystems.TryGet(out RoundEndUiSubSystem ui) ? ui : null;

        /// <summary>A named element of the round-end overlay (shared shell document, Overlay layer).</summary>
        private static VisualElement RoundEndPart(string name)
        {
            RoundEndUiSubSystem ui = RoundEndUi();
            return ui != null && ui.Overlay?.Root != null ? ui.Overlay.Root.Q(name) : null;
        }

        /// <summary>Displayed with every ancestor displayed; <see cref="Visible"/> checks the element alone.</summary>
        private static bool Shown(VisualElement element)
        {
            if (element?.panel == null) return false;
            for (VisualElement current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            return true;
        }

        private static VisualElement Swatch(string picker, int index)
        {
            List<VisualElement> swatches = Creator()?.Q(picker)?.Query(className: "char-creator__swatch").ToList();
            return swatches != null && index < swatches.Count ? swatches[index] : null;
        }

        private static bool SwatchSelectedAndLaidOut(string picker, int index)
        {
            VisualElement swatch = Swatch(picker, index);
            return swatch != null && swatch.worldBound.width > 0 && swatch.ClassListContains("char-creator__swatch--selected");
        }

        private static List<string> ChatLines() =>
            Shell()?.Query<Label>(className: "lobby-shell__chat-text").ToList().ConvertAll(l => l.text) ?? new List<string>();

        private static bool HostChatContains(string text) =>
            SubSystems.TryGet(out CommsSubSystem comms) && comms.OocLog.Any(m => m.Text == text);

        private static TextField ChatInput() => Shell()?.Q<TextField>(className: "lobby-shell__chat-input");

        private static bool MapEditorActive() => SubSystems.TryGet(out MapEditorSubSystem mapEditor) && mapEditor.IsActive;

        private static bool GameplayMapEnabled() => SubSystems.TryGet(out InputSubSystem input) && input.Inputs.Movement.Get().enabled;

        private static void Settle(double seconds) => _settleUntil = EditorApplication.timeSinceStartup + seconds;

        private static bool Settled() => EditorApplication.timeSinceStartup > _settleUntil;

        /// <summary>Synthetic press + release through the Input System (actions, not UITK events).</summary>
        private static void TapKey(Key key)
        {
            Keyboard keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }

        private static VisualElement Shell() => Root()?.Q("lobby-shell-surface");

        private static VisualElement Creator() => Root()?.Q("character-creator-surface");

        // The uGUI lobby (LobbyCanvas, renamed "Lobby" in Game.unity) and Character Customizer canvases were purged in Phase F.
        private static bool LegacyLobbyCanvasPresent() => UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Any(canvas => canvas.isRootCanvas && (canvas.name == "Lobby" || canvas.name == "Character Customizer"));

        private static bool Visible(VisualElement surface) => surface != null && surface.resolvedStyle.display != DisplayStyle.None;

        private static Button ReadyButton() => Shell().Q<Button>(className: "lobby-shell__btn--ready");

        private static Button AdminButton() => Shell().Q<Button>(className: "lobby-shell__btn--admin");

        private static string CountLabel() => Shell().Q<Label>(className: "lobby-shell__countdown")?.text;

        private static string CharacterName() => Shell().Q<Label>(className: "lobby-shell__character-name")?.text;

        private static JobSlotInfo Job(string name) =>
            SubSystems.TryGet(out RoleSubSystem roles) ? roles.Jobs.FirstOrDefault(job => job.Name == name) : default;

        private static string AssignedRole() =>
            SubSystems.TryGet(out RoleSubSystem roles) && roles.TryGetRole(Local(), out RoleData role) ? role.Name : null;

        private static VisualElement SecurityRow() => Shell()?.Query(className: "lobby-shell__job-row")
            .Where(row => row.Q<Label>(className: "lobby-shell__job-name")?.text == "Security").First();

        private static string SecurityRowText() =>
            string.Join(" ", SecurityRow()?.Query<Label>().ToList().Select(label => label.text) ?? Array.Empty<string>());

        private static Button SecurityChip(string text) =>
            SecurityRow()?.Query<Button>(className: "lobby-shell__priority-chip").Where(chip => chip.text == text).First();

        private static List<VisualElement> StyleTiles() =>
            Creator()?.Query(className: "char-creator__style-tile").ToList() ?? new List<VisualElement>();

        /// <summary>The saved lobby character reached the server and was applied to the spawned body.</summary>
        private static void VerifySpawnedAppearance()
        {
            Require(SubSystems.Get<EntitySubSystem>().TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out Entity entity),
                "Local spawned entity found");
            Require(entity.Mind.CharacterName == PilotCharacterName && entity.Mind.DisplayName == PilotCharacterName,
                "Spawned mind carries the saved character name");
            Require(entity is Human, "Spawned entity is a Human");

            Transform armature = entity.GetComponent<SS3D.Systems.Entities.Humanoid.Body.HumanoidRigReferences>().ArmatureRoot;
            Require(Mathf.Approximately(armature.localScale.y, Mathf.Lerp(0.88f, 1.12f, PilotHeight)), "Height scales the armature");
            Require(entity.transform.localScale == Vector3.one, "Networked root keeps unit scale");

            bool fat = entity.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Any(r => r.sharedMesh != null && r.sharedMesh.GetBlendShapeIndex(HumanoidMorphApplier.Fat) >= 0
                    && Mathf.Approximately(r.GetBlendShapeWeight(r.sharedMesh.GetBlendShapeIndex(HumanoidMorphApplier.Fat)), PilotFat * 100f));
            Require(fat, "Fat shape key applied to the body");

            Transform socket = Array.Find(entity.GetComponentsInChildren<Transform>(true), t => t.name == "HairSocket");
            Require(socket != null && socket.childCount == 1, "Saved hair style spawned on the head");
            VerifyFace(entity.gameObject);
        }

        private static void VerifyFace(GameObject root)
        {
            foreach ((string materialName, Color color, string property) in new (string, Color, string)[]
            {
                ("HumanSkin", CharacterCreatorMockData.SkinColors[5], "_Color"),
                ("HumanEye", CharacterCreatorMockData.EyeColors[1], "_BaseColor"),
            })
            {
                bool found = false;
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                    for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                        if (renderer.sharedMaterials[i] != null && renderer.sharedMaterials[i].name == materialName)
                        {
                            MaterialPropertyBlock block = new();
                            renderer.GetPropertyBlock(block, i);
                            Require(SameColor32(block.GetColor(property), color), materialName + " tint matches saved selection on " + renderer.name
                                + $" (got {block.GetColor(property)}, expected {color}, empty={block.isEmpty}, slot {i}/{renderer.sharedMaterials.Length}, path {renderer.transform.parent.name}/{renderer.name})");
                            found = true;
                        }
                Require(found, materialName + " renderers were checked");
            }
            Transform brows = Array.Find(root.GetComponentsInChildren<Transform>(true), t => t.name == "EyebrowSocket");
            Require(brows != null && brows.childCount == 1 && brows.GetChild(0).name.Contains("Bold"), "Selected eyebrow mesh is attached to the head");
            MaterialPropertyBlock browBlock = new();
            brows.GetComponentInChildren<Renderer>().GetPropertyBlock(browBlock);
            Color browExpected = ((LobbyAssetCatalog)HumanoidStyleSource.Current).GetHairColor(4);
            Require(SameColor32(browBlock.GetColor("_Color"), browExpected),
                $"Eyebrow color is independent (got {browBlock.GetColor("_Color")}, expected {browExpected}, renderer {brows.GetComponentInChildren<Renderer>().name})");
        }

        // Appearance colors replicate as Color32; compare at that precision.
        private static bool SameColor32(Color a, Color b)
        {
            Color32 x = a;
            Color32 y = b;
            return x.r == y.r && x.g == y.g && x.b == y.b && x.a == y.a;
        }

        private static Button FindButton(VisualElement surface, string text) =>
            surface?.Query<Button>().Where(b => b.text == text && b.worldBound.width > 0).First();

        private static bool BoothLive()
        {
            CharacterPreviewBooth booth = UnityEngine.Object.FindAnyObjectByType<CharacterPreviewBooth>();
            if (booth == null || booth.PreviewTexture == null) return false;
            bool camera = booth.GetComponentsInChildren<Camera>(true).Any(c => c.enabled);
            VisualElement image = Shell()?.Q(className: "lobby-shell__preview-image");
            return camera && image != null && image.resolvedStyle.backgroundImage.renderTexture == booth.PreviewTexture;
        }

        private static string Describe()
        {
            RoundSubSystem rounds = Rounds();
            return $"online={Online()} local={Local() != null} round={(rounds != null ? rounds.CurrentRoundState.ToString() : "-")} "
                + $"ready={IsReady()} spawned={Spawned()} shell={Visible(Shell())} creator={Visible(Creator())} "
                + $"button='{(Shell() != null ? ReadyButton()?.text : "-")}' count='{(Shell() != null ? CountLabel() : "-")}'";
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message + " | " + Describe());
            Checks.Add(message);
        }

        // Button.clicked ignores synthetic ClickEvents; buttons get NavigationSubmit, other elements ClickEvent.
        private static void Click(VisualElement element)
        {
            Require(element != null && element.enabledInHierarchy && element.worldBound.width > 0, "Clickable element is enabled and laid out");
            if (element is Button button)
            {
                button.Focus();
                using NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled();
                submit.target = button;
                button.SendEvent(submit);
                return;
            }

            using ClickEvent evt = ClickEvent.GetPooled();
            evt.target = element;
            element.SendEvent(evt);
        }

        private static void Capture(string name) =>
            ScreenCapture.CaptureScreenshot(Path.Combine(SessionState.GetString(OutputKey, ""), name + ".png"));

        [Serializable]
        private sealed class Report
        {
            public string status;
            public string failure;
            public double seconds;
            public string[] checks;
            public string[] timeline;
            public string[] errors;
            public string[] postconditions;
            public string scope;
        }

        // Own the fixture: valid and invalid copies of ministation with known spawn records.
        // Never overwrite a tester's station merely to make the lifecycle smoke pass.
        private static void CreateValidationStation(bool valid)
        {
            EnvelopePersistenceStore store = new();
            Require(store.TryLoad(PersistencePaths.StationTemplates + "/ministation", out PersistenceEnvelope envelope),
                "Loaded base station for temporary validation fixture");
            _validationStation ??= "validation-pilot-" + Guid.NewGuid().ToString("N");
            envelope.chunks.RemoveAll(chunk => chunk.contributorId == "spawn-points");
            List<SavedSpawnPointRecord> records = new();
            if (valid)
            {
                TileSubSystem tiles = SubSystems.Get<TileSubSystem>();
                SavedTileMap map = JsonUtility.FromJson<SavedTileMap>(envelope.chunks.Single(chunk => chunk.contributorId == "tilemap").payloadJson);
                Vector3[] floors = map.savedChunkList.SelectMany(chunk => chunk.savedTiles
                    .Where(tile => tile != null && tile.GetPlacedObjects().Any(placed =>
                        tiles.GetAsset(placed.tileObjectSOName) is TileObjectSo asset && asset.layer == TileLayer.Plenum))
                    .Select(tile => chunk.originPosition + new Vector3(tile.Location.x, 0, tile.Location.y)))
                    .Distinct().ToArray();
                Require(floors.Length > SubSystems.Get<RoleSubSystem>().Jobs.Count,
                    "Fixture has enough floor cells for every job and the operative marker");
                int index = 0;
                foreach (JobSlotInfo job in SubSystems.Get<RoleSubSystem>().Jobs)
                    records.Add(SavedSpawnPointRecord.FromRecord(SpawnPointRecord.ForJob(job.Name, floors[index++], Direction.North)));
                records.Add(SavedSpawnPointRecord.FromRecord(SpawnPointRecord.ForAntagonist(
                    AntagonistSpawnCategory.NuclearOperatives, floors[index], Direction.North)));
            }
            envelope.chunks.Add(new PersistenceChunk
            {
                contributorId = "spawn-points",
                payloadJson = JsonUtility.ToJson(new SavedSpawnPointChunkPayload { records = records.ToArray() }),
            });
            Require(store.TrySave(PersistencePaths.StationTemplates + "/" + _validationStation, envelope, true),
                "Saved temporary validation fixture");
        }

        private static void Finish(string failure, bool stopPlay)
        {
            _running = false;
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            StopClient();
            if (!string.IsNullOrEmpty(_alternateStation))
            {
                SS3D.Data.Management.LocalStorage.DeleteFile(SS3D.Data.Persistence.PersistencePaths.StationTemplates + "/" + _alternateStation);
                LocalStorage.DeleteFile(PersistencePaths.StationTemplates + "/" + _alternateStation + RoundSubSystem.AutosaveSuffix);
                _alternateStation = null;
            }
            if (!string.IsNullOrEmpty(_validationStation))
            {
                LocalStorage.DeleteFile(PersistencePaths.StationTemplates + "/" + _validationStation);
                LocalStorage.DeleteFile(PersistencePaths.StationTemplates + "/" + _validationStation + RoundSubSystem.AutosaveSuffix);
                _validationStation = null;
            }
            Report report = new()
            {
                status = failure == null && Errors.Count == 0 ? "passed" : "failed",
                failure = failure,
                seconds = Elapsed(),
                checks = Checks.ToArray(),
                timeline = Timeline.ToArray(),
                errors = Errors.ToArray(),
                scope = ReportScope(),
            };
            // Drivers wait for passed/failed; the final status follows the post-Play scene check.
            string result = report.status;
            report.status = "verifying";
            Write(report, OutputDir);
            report.status = result;
            _pending = report;
            if (stopPlay) EditorApplication.isPlaying = false;
        }

        private static string ReportScope()
        {
            if (SessionState.GetString(ModeKey, "") == InteractionOverlayMode)
                return "Focused runtime CPR armed/radial overlay geometry at panel scales 1.5 and 0.75, including screen-edge placement. Interaction dispatch and CPR health effects are outside this visual pilot.";
            if (SessionState.GetString(ModeKey, "") == RoundResetRegressionMode)
                return "Focused editor Host checks: Stop clears blood/bullet decals; restarting the same station restores oxygen in a fresh atmos simulation. Remote clients are not covered.";
            if (SessionState.GetString(ModeKey, "") == HealthRegressionMode)
                return "Focused editor Host checks: critical held-item drop and action refusal, recovery, world blackout with Escape menu, ghost FOV and respawn. No builds or remote multiplayer tests.";
            if (Sandbox) return SandboxScope;
            return Remote
                ? "Editor Host + built non-admin client process on loopback. The client's Ready click, chat typing and bug-report interaction are real OS input (uinput virtual device); other UI steps are synthetic events. Client screenshots and client.log in this folder. Does not cover remote machines."
                : "Single-editor Host session; synthetic UI events. Does not cover a remote client process, non-admin permissions or OS input.";
        }

        private static void Complete(Report report, string output, string[] problems)
        {
            report.postconditions = problems;
            if (problems.Length > 0)
            {
                report.status = "failed";
                report.failure ??= "Postcondition: " + string.Join("; ", problems);
            }
            Write(report, output);
            Debug.Log($"[SS3D Automation] Lobby lifecycle pilot {report.status}: {output}");
        }

        private static void Write(Report report, string output) =>
            File.WriteAllText(Path.Combine(output, "status.json"), JsonUtility.ToJson(report, true));
    }
}
