using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FishNet;
using SS3D.Core;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Atmospherics.SystemsView;
using SS3D.Systems.Entities;
using SS3D.Systems.Examine;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// Rendered check of the Map Editor's Atmos view (Documents/architecture/2026-11_map-editor-systems-views.md Q9): a
    /// host round on a station with pipes, the editor open with the view on, screenshots of the overview with its legend,
    /// a close-up of device badges and a hover readout, a device switched off reaching the client's badge, and a Select
    /// tracing a two-network device (panel, label, the rest dimmed). It also
    /// logs the first-open structure size against the send budget. <c>Testing/ui/validate.sh --steps visual</c> runs it
    /// after the lobby pilot; screenshots land in <c>artifacts/ui/systems-view-*</c>.
    /// </summary>
    [InitializeOnLoad]
    public static class MapEditorSystemsPilot
    {
        private const string ActiveKey = "SS3D.SystemsViewPilot.Active";
        private const string OutputKey = "SS3D.SystemsViewPilot.Output";
        private const string SceneKey = "SS3D.SystemsViewPilot.PreviousStartScene";
        private const string SizeKey = "SS3D.SystemsViewPilot.PreviousGameViewSize";
        private const string LastTabKey = "SS3D.SystemsViewPilot.PreviousLastTab";
        private const string NoLastTab = "\u0000none";
        /// <summary>A station template to run on instead of the shipped classic-ministation (e.g. a MetaStation copy for budget numbers).</summary>
        public const string StationKey = "SS3D.SystemsViewPilot.Station";
        private const string DefaultStation = "classic-ministation";
        private const double Seconds = 240;
        // A big station override (MetaStation loads for minutes in the editor) gets this long for loading.
        private static double LoadSeconds => Station == DefaultStation ? 120 : 900;
        // The start step may wait two loads plus a minute (Timeout = 2 * LoadSeconds + 60); the default budget already
        // covers that at 120 s per load, so a longer load adds its excess over 120 s for each of the two loads.
        private static double ExtraLoadSeconds => 2 * (LoadSeconds - 120);

        private static readonly List<string> Errors = new();
        private static readonly List<string> Checks = new();
        private static readonly List<string> Measurements = new();
        private static Step[] _steps;
        private static int _index;
        private static bool _entered;
        private static double _stepDeadline;
        private static double _deadline;
        private static double _settleUntil;
        private static bool _running;
        private static Report _pending;
        private static Vector2Int _focus;
        private static Vector2Int _hoverTile;
        private static VentController _switchedVent;
        private static Vector2Int _traceTile;

        private sealed class Step
        {
            public string Name;
            public Action Enter;
            public Func<bool> Until;
            public Action Verify;
            public double Timeout = 30;
        }

        static MapEditorSystemsPilot()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        // Guards: the Map Editor's Atmos view draws coloured runs, badges, legend and hover on a real station (map-editor-systems-views Q9) · Not cheaper because: needs the rendered overlay and UI Toolkit chrome in a live host round, reviewed by eye
        [MenuItem("SS3D/Testing/Run Map Editor Systems Pilot")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before starting the systems view pilot.");
            if (EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Fix compilation errors before starting the systems view pilot.");
            if (SessionState.GetBool(ActiveKey, false)) RestoreEditorSettings();
            string output = Path.GetFullPath("artifacts/ui/systems-view-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(output);
            SessionState.SetString(OutputKey, output);
            try { AutomationPreflight.Require("map editor systems pilot"); }
            catch (InvalidOperationException exception)
            {
                Write(new Report { status = "failed", failure = exception.Message }, output);
                throw;
            }

            SessionState.SetString(SceneKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetString(LastTabKey, PlayerPrefs.GetString(MapEditorLastTab.PrefsKey, NoLastTab));
            SessionState.SetInt(SizeKey, AutomationGameView.Select1080p());
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
                Measurements.Clear();
                _steps = BuildSteps();
                _index = 0;
                _entered = false;
                _switchedVent = null;
                _running = true;
                _deadline = EditorApplication.timeSinceStartup + Seconds + ExtraLoadSeconds;
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
                string output = SessionState.GetString(OutputKey, "");
                _pending = null;
                AutomationPreflight.AfterPlay(problems => Complete(report, output, problems));
            }
        }

        private static void RestoreEditorSettings()
        {
            string previous = SessionState.GetString(SceneKey, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous)
                ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
            AutomationGameView.Restore(SessionState.GetInt(SizeKey, 0));
            // The pilot switches tabs, which the editor remembers; give the author back theirs.
            string lastTab = SessionState.GetString(LastTabKey, NoLastTab);
            if (lastTab == NoLastTab) PlayerPrefs.DeleteKey(MapEditorLastTab.PrefsKey);
            else PlayerPrefs.SetString(MapEditorLastTab.PrefsKey, lastTab);
            PlayerPrefs.Save();
            SessionState.SetBool(ActiveKey, false);
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Errors.Add($"[step {_index}] {message}\n{stack}");
        }

        private static void Tick()
        {
            if (!_running) return;
            try
            {
                if (EditorApplication.timeSinceStartup > _deadline)
                    throw new TimeoutException($"Systems view pilot exceeded its time at step '{_steps[_index].Name}'.");
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
                _entered = false;
                if (++_index >= _steps.Length) Finish(null, true);
            }
            catch (Exception exception) { Finish(exception.ToString(), true); }
        }

        private static string Station => SessionState.GetString(StationKey, DefaultStation);

        private static Step[] BuildSteps() => new Step[]
        {
            new Step
            {
                Name = "Lobby ready on host boot",
                Timeout = 180,
                Until = () => Online() && Rounds() != null && Settings() != null && Local() != null
                    && Rounds().CurrentRoundState == RoundState.Stopped
                    && ReadyButton() is { } ready && ready.enabledInHierarchy && ready.worldBound.width > 0,
                Verify = () =>
                {
                    ServerSettings settings = Settings().Current;
                    settings.Map = Station;
                    Settings().ApplyServerSettingsForTesting(settings);
                    Settle(1.5);
                },
            },
            new Step
            {
                Name = "Ready up",
                Until = () => Settled() && ReadyButton() is { } ready && ready.enabledInHierarchy && ready.worldBound.width > 0,
                Verify = () => Submit(ReadyButton()),
            },
            new Step
            {
                Name = "Start a round on the station",
                Until = () => SubSystems.TryGet(out ReadyPlayersSubSystem ready) && ready.IsReady(Local()),
                Verify = () => Require(Rounds().ServerRoundAdminAction(RoundAdminAction.Start, null), "Admin start accepted"),
            },
            new Step
            {
                Name = "Round runs with the host spawned and atmos ready",
                Timeout = 2 * LoadSeconds + 60,
                // Skips the countdown.
                Until = () =>
                {
                    // The round system re-registers while a station loads.
                    if (Rounds() == null)
                        return false;
                    if (Rounds().CurrentRoundState == RoundState.WarmingUp)
                        Rounds().ServerRoundAdminAction(RoundAdminAction.ForceStart, null);
                    else if (Station != DefaultStation && Rounds().CurrentRoundState is RoundState.Stopped or RoundState.Ended
                        && Rounds().CurrentStationLoadState == StationLoadState.Unplayable
                        && SubSystems.TryGet(out AtmosSubSystem loaded) && loaded.IsReady && Editor() != null)
                    {
                        // A measurement copy that can't start a round (no job spawns): its map is loaded and atmos runs, so
                        // measure the server side and stop. A second load ("Edit map anyway") of a station this big stalls
                        // the editor long enough for the host's own client to time out.
                        MeasureAndFrame();
                        _index = _steps.Length - 1;
                        _steps[_index] = new Step { Name = "Measured a station that can't start a round" };
                        _entered = true;
                        return false;
                    }
                    return Rounds().CurrentRoundState == RoundState.Ongoing && Spawned()
                        && SubSystems.TryGet(out AtmosSubSystem atmos) && atmos.IsReady;
                },
                Verify = () =>
                {
                    Require(Rounds().StationName == Station, $"Round runs on {Station}");
                    Settle(3);
                },
            },
            new Step { Name = "Body finishes starting", Until = Settled },
            new Step
            {
                Name = "Open the map editor on Upper › Floors, with no view on",
                Enter = () => TapKey(Key.B),
                Until = () => Editor() != null && Editor().IsActive,
                Verify = () =>
                {
                    // The editor reopens on the last tab (PlayerPrefs), and Lower › Gas Pipes turns Atmos on by itself;
                    // the toolbar press below would then turn it off (Q13). Start every run from the same tab.
                    Editor().AutomationOpenSubTab(MapEditorSubcategory.Floors);
                    Require(Editor().CurrentSubcategory == MapEditorSubcategory.Floors, "The editor starts on Upper › Floors");
                    Settle(1);
                },
            },
            new Step
            {
                Name = "Measure the first-open structure and frame its densest chunk",
                Until = Settled,
                Verify = MeasureAndFrame,
            },
            new Step
            {
                Name = "Turn the Atmos view on from the toolbar",
                Enter = () =>
                {
                    Require(Client().Requested == SystemsViewKind.Off, "No view is on before the toolbar press");
                    Submit(EditorRoot().Q<Button>("systems-view-atmos"));
                },
                Until = () => Client().Structure is { } structure && structure.View == SystemsViewKind.Atmos
                    && structure.Segments.Count > 0 && Client().Overlay.ChunkCount > 0,
                Verify = () => Settle(1.5),
            },
            new Step
            {
                Name = "Overview: coloured runs, badges and the legend",
                Until = Settled,
                Verify = () =>
                {
                    SystemsViewStructure structure = Client().Structure;
                    Require(structure.Badges.Count > 0, $"Client has device badges ({structure.Badges.Count})");
                    Require(structure.Networks.Values.All(n => !string.IsNullOrEmpty(n.Label)), "Every network has a name");
                    VisualElement legend = EditorRoot().Q("systems-legend");
                    Require(legend != null && legend.resolvedStyle.display == DisplayStyle.Flex && legend.worldBound.height > 40f,
                        "The legend is open with the view on");
                    Require(legend.Query<Label>().ToList().Any(l => l.text.StartsWith("Vent:")), "The legend explains the vent mark");
                    Measurements.Add($"client: {structure.Segments.Count} segments, {structure.Networks.Count} networks, "
                        + $"{structure.Badges.Count} badges in {Client().Overlay.ChunkCount} overlay chunks");
                    Capture("10-atmos-overview");
                },
            },
            new Step
            {
                Name = "Close-up on devices, hovering one",
                Enter = () =>
                {
                    _hoverTile = PickHoverTile(Client().Structure);
                    Editor().AutomationCenterOn(new Vector3(_hoverTile.x, 0f, _hoverTile.y));
                    Editor().AutomationZoomStep(13f);
                    Settle(0.6);
                },
                Until = Settled,
                Verify = () =>
                {
                    Point(_hoverTile);
                    Settle(0.8);
                },
            },
            new Step
            {
                Name = "Hover names the line and the device",
                Until = () => Settled() && !string.IsNullOrEmpty(CursorLabel()),
                Timeout = 10,
                Verify = () =>
                {
                    string expected = Client().HoverText(_hoverTile);
                    // Under the placing label when the tab has an entry held (MapEditorSystemsViewClient.CursorLabel).
                    Require(!string.IsNullOrEmpty(expected) && CursorLabel().EndsWith(expected),
                        $"The cursor badge reads the hovered tile: '{CursorLabel()}'");
                    Measurements.Add($"hover {_hoverTile}: {CursorLabel().Replace('\n', '/')}");
                    Capture("11-atmos-hover-closeup");
                },
            },
            new Step
            {
                Name = "On Lower › Gas Pipes, placing from the palette, hover still names the line",
                Enter = () =>
                {
                    Require(Editor().TryGetSubcategoryPrototypeAssetName(MapEditorSubcategory.GasPipes, out string pipe),
                        "Gas Pipes has something to place");
                    Editor().AutomationPickPaletteEntry(new MapEditorCatalogEntry { AssetName = pipe }, SubSystems.Get<TileSubSystem>().GetAsset(pipe));
                    _hoverTile = PickRunTile(Client().Structure);
                    Point(_hoverTile);
                    Settle(0.8);
                },
                Until = () => Settled() && CursorLabel()?.Contains('\n') == true,
                Timeout = 10,
                Verify = () =>
                {
                    Require(Editor().CurrentSubcategory == MapEditorSubcategory.GasPipes, "Picking it opened Lower › Gas Pipes");
                    string hover = Client().HoverText(_hoverTile);
                    Require(!string.IsNullOrEmpty(hover) && CursorLabel().EndsWith(hover),
                        $"The cursor badge shows the placing label and the hovered line: '{CursorLabel()}'");
                    Measurements.Add($"placing hover {_hoverTile}: {CursorLabel().Replace('\n', '/')}");
                    Capture("12-atmos-gaspipes-placing-hover");
                },
            },
            new Step
            {
                Name = "Switching a vent off reaches the client's badge",
                Enter = () =>
                {
                    _switchedVent = SubSystems.Get<AtmosSubSystem>().PortRegistry.Ports.OfType<VentController>().First(v => v.IsEnabled);
                    _switchedVent.ServerSetEnabled(false);
                },
                Until = () => VentBadge() is { } badge && AtmosSystemsViewCodes.SwitchOf(badge.State) == AtmosSystemsViewCodes.Off,
                Timeout = 5,
                Verify = () =>
                {
                    Require(true, "A vent switched off shows Off on the client within the poll");
                    _switchedVent.ServerSetEnabled(true);
                },
            },
            new Step
            {
                Name = "Select on a two-network device traces both sides",
                Enter = () =>
                {
                    _traceTile = PickHoverTile(Client().Structure);
                    Editor().AutomationSelectTool(MapEditorTool.Select);
                    Editor().AutomationCenterOn(new Vector3(_traceTile.x, 0f, _traceTile.y));
                    Settle(1.2);
                },
                Until = Settled,
            },
            new Step
            {
                Name = "The click traces through the Select tool",
                // Pointed once the camera has stopped moving, so the screen point still lands on the device.
                Enter = () =>
                {
                    Point(_traceTile);
                    Settle(0.3);
                },
                Until = Settled,
                Verify = () =>
                {
                    // HandleSelectClick is what a press of Place runs with the Select tool (Q11): the pick, then the trace.
                    // Called from the editor's update, which reads the Input System's editor state buffer: point there too.
                    Point(_traceTile, InputUpdateType.Editor);
                    Editor().AutomationSelectClick();
                    // The pick is described only for a failure: it raycasts and reads the map again.
                    if (!Client().IsTracing)
                        Require(false, $"Select on {_traceTile} traced its network(s) ({DescribePick()})");
                    Checks.Add($"Select on {_traceTile} traced its network(s)");
                },
            },
            new Step
            {
                Name = "Trace panel, label and the rest dimmed",
                Until = () => Client().Live is { } live && live.Networks.Length == Client().TraceSides.Count,
                Timeout = 5,
                Verify = () =>
                {
                    VisualElement panel = EditorRoot().Q("systems-trace");
                    Require(panel != null && panel.resolvedStyle.display == DisplayStyle.Flex, "The trace panel is open");
                    Require(panel.Query<Label>().ToList().Any(l => l.text == "Pressure"), "It shows the traced network's pressure");
                    Require(panel.Q<Button>("systems-trace-locate-0") != null || Client().Live.Value.Networks.All(n => n.Devices.Length == 0),
                        "Each attached device has a Locate");
                    VisualElement label = EditorRoot().Q("systems-trace-label");
                    Require(label != null && label.resolvedStyle.display == DisplayStyle.Flex, "The network's name label is in the world");
                    Require(Client().Overlay.Focus.Count == Client().TraceSides.Count, "The overlay dims everything but the traced networks");
                    Measurements.Add($"trace {_traceTile}: " + string.Join(" | ", Client().TraceSides.Select(side => Client().TraceLabel(side)
                        + " " + Client().Live.Value.Networks.First(n => n.ViewId == side.ViewId).ValueOf("Pressure"))));
                    Capture("13-atmos-trace");
                    Settle(0.5);
                },
            },
            new Step
            {
                Name = "Clear ends the trace",
                Until = Settled,
                Verify = () =>
                {
                    Submit(EditorRoot().Q<Button>("systems-trace-close"));
                    Require(!Client().IsTracing && Client().Overlay.Focus.Count == 0, "Clear ends the trace and undims the overlay");
                },
            },
            new Step
            {
                Name = "Turning the view off clears the overlay",
                Enter = () => Submit(EditorRoot().Q<Button>("systems-view-atmos")),
                Until = () => Client().Structure == null,
                Verify = () => Require(EditorRoot().Q("systems-legend").resolvedStyle.display == DisplayStyle.None,
                    "The legend hides with the view"),
            },
        };

        /// <summary>What Select's pick sees under the pointer, for a trace that didn't start.</summary>
        private static string DescribePick()
        {
            bool picked = MapEditorCursorPick.TryPick(Editor().PickCamera, SubSystems.Get<TileSubSystem>().CurrentMap,
                Editor().AutomationViewModel, out PlacedTileObject placed, out PlacedItemObject item);
            Vector3 pointed = TileHelper.GetPointedPosition(true, Editor().PickCamera);
            string badges = string.Join(",", Client().Structure.Badges.Keys.Where(k => k.Tile == _traceTile).Select(k => ((TileLayer)k.Slot).ToString()));
            return $"picked={picked} placed={(placed != null ? placed.NameString + "@" + placed.WorldOrigin + "/" + placed.Layer : "-")} "
                + $"item={(item != null ? item.NameString : "-")} pointed={pointed} tool={Editor().CurrentTool} badges here: {badges} "
                + $"camera={Editor().PickCamera.transform.position} tileScreen={Editor().PickCamera.WorldToScreenPoint(new Vector3(_traceTile.x, 0.2f, _traceTile.y))} "
                + $"mouse={Mouse.current?.position.ReadValue()} devices={string.Join(",", InputSystem.devices.OfType<Mouse>().Select(m => m.name + m.position.ReadValue()))}";
        }

        private static SystemsViewBadge? VentBadge()
        {
            if (_switchedVent == null || Client().Structure == null || !_switchedVent.TryGetComponent(out PlacedTileObject placed))
                return null;
            return Client().Structure.Badges.TryGetValue(new SystemsViewKey(placed.WorldOrigin, (byte)placed.Layer), out SystemsViewBadge badge)
                ? badge : null;
        }

        /// <summary>
        /// Server-side numbers for the effort doc's traffic risk: what a first open sends, how long a build takes cold
        /// (a fresh source walks every network) and warm, then frames the camera on the chunk with the most runs.
        /// </summary>
        private static void MeasureAndFrame()
        {
            AtmosSubSystem atmos = SubSystems.Get<AtmosSubSystem>();
            Require(atmos.SystemsViewSource != null, "The atmos source exists on the server");
            AtmosSystemsViewSource fresh = new(() => SubSystems.Get<TileSubSystem>().CurrentMap, () => atmos.PipeRegistry, atmos.PortRegistry);
            Stopwatch cold = Stopwatch.StartNew();
            SystemsViewStructure structure = fresh.BuildStructure();
            cold.Stop();
            int walked = fresh.LastWalkedNetworks;
            Stopwatch warm = Stopwatch.StartNew();
            fresh.BuildStructure();
            warm.Stop();
            int bytes = SystemsViewWire.SizeOf(SystemsViewStructure.Diff(null, structure));
            int budget = Editor().SystemsViews.MaxBytesPerSend;
            Measurements.Add($"{Station}: {structure.Segments.Count} segments, {structure.Networks.Count} networks, "
                + $"{structure.Badges.Count} badges, {atmos.PipeRegistry.NetworkCount} registry networks");
            Measurements.Add($"first-open delta {bytes} B against a {budget} B send budget: {(bytes + budget - 1) / budget} send(s)");
            Measurements.Add($"build: cold {cold.Elapsed.TotalMilliseconds:0.0} ms (walks {walked} networks), "
                + $"warm {warm.Elapsed.TotalMilliseconds:0.00} ms");
            // A device removed: only the pipes beside it are re-linked (it used to walk every network).
            AtmosPortRegistry ports = new();
            foreach (IAtmosPortDevice port in atmos.PortRegistry.Ports)
                ports.Register(port);
            AtmosSystemsViewSource probe = new(() => SubSystems.Get<TileSubSystem>().CurrentMap, () => atmos.PipeRegistry, ports);
            _ = probe.StructureVersion;
            probe.BuildStructure();
            VentController removed = ports.Ports.OfType<VentController>().FirstOrDefault();
            if (removed != null)
            {
                ports.Unregister(removed);
                _ = probe.StructureVersion;
                Stopwatch device = Stopwatch.StartNew();
                probe.BuildStructure();
                device.Stop();
                Measurements.Add($"build after a device is removed: {device.Elapsed.TotalMilliseconds:0.00} ms "
                    + $"(walks {probe.LastWalkedNetworks} of {atmos.PipeRegistry.NetworkCount} networks, re-links {probe.LastRelinkedRuns} pipes)");
            }

            Debug.Log("[systems-view-budget] " + string.Join(" | ", Measurements));
            Require(structure.Segments.Count > 0, $"{Station} has pipe runs for the view");

            _focus = structure.Segments.Keys
                .GroupBy(key => MapEditorSystemsOverlay.ChunkOf(key.Tile))
                .OrderByDescending(group => group.Count() + 4 * structure.Badges.Keys.Count(b => MapEditorSystemsOverlay.ChunkOf(b.Tile) == group.Key))
                .First().Key * TileConstants.ChunkSize + new Vector2Int(TileConstants.ChunkSize / 2, TileConstants.ChunkSize / 2);
            Editor().AutomationCenterOn(new Vector3(_focus.x, 0f, _focus.y));
        }

        /// <summary>A two-network device near the framed chunk if there is one, so the close-up shows both colours; else any badge.</summary>
        private static Vector2Int PickHoverTile(SystemsViewStructure structure)
        {
            IEnumerable<SystemsViewBadge> badges = structure.Badges.Values
                .OrderBy(b => (b.Key.Tile - _focus).sqrMagnitude);
            SystemsViewBadge pick = badges.FirstOrDefault(b => AtmosSystemsViewCodes.IsTwoNetwork(b.Kind));
            if (pick.Kind == 0) pick = badges.First();
            return pick.Key.Tile;
        }

        /// <summary>A tile with one run and no badge near the framed chunk, so the hover reads a plain pipe.</summary>
        private static Vector2Int PickRunTile(SystemsViewStructure structure) => structure.Segments.Keys
            .Select(key => key.Tile)
            .Where(tile => structure.Segments.Keys.Count(key => key.Tile == tile) == 1
                && !structure.Badges.Keys.Any(key => key.Tile == tile))
            .OrderBy(tile => (tile - _hoverTile).sqrMagnitude)
            .First();

        /// <summary>Moves the Input System pointer over a tile, as a hand on the mouse would.</summary>
        private static void Point(Vector2Int tile, InputUpdateType buffer = default)
        {
            Vector3 screen = Editor().PickCamera.WorldToScreenPoint(new Vector3(tile.x, 0.2f, tile.y));
            Mouse mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            InputState.Change(mouse.position, new Vector2(screen.x, screen.y), buffer);
        }

        private static bool Online() => InstanceFinder.ClientManager != null && InstanceFinder.ClientManager.Started;

        private static Player Local() => Online() && SubSystems.TryGet(out PlayerSubSystem players)
            ? players.GetPlayer(InstanceFinder.ClientManager.Connection) : null;

        private static RoundSubSystem Rounds() => SubSystems.TryGet(out RoundSubSystem rounds) ? rounds : null;

        private static ServerSettingsService Settings() => SubSystems.TryGet(out ServerSettingsService settings) ? settings : null;

        private static bool Spawned() => Online() && SubSystems.TryGet(out EntitySubSystem entities)
            && entities.IsPlayerSpawned(InstanceFinder.ClientManager.Connection);

        private static MapEditorSubSystem Editor() => SubSystems.TryGet(out MapEditorSubSystem editor) ? editor : null;

        private static MapEditorSystemsViewClient Client() => Editor().SystemsViewClient;

        private static string CursorLabel() => SubSystems.TryGet(out ExamineSubSystem examine) ? examine.CursorLabelOverride : null;

        private static VisualElement EditorRoot() => UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
            .Select(d => d.rootVisualElement)
            .First(r => r != null && r.Q("systems-view-atmos") != null);

        private static Button ReadyButton() => UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
            .Select(d => d.rootVisualElement?.Q("lobby-shell-surface"))
            .FirstOrDefault(shell => shell != null && shell.resolvedStyle.display != DisplayStyle.None)
            ?.Q<Button>(className: "lobby-shell__btn--ready");

        private static void Settle(double seconds) => _settleUntil = EditorApplication.timeSinceStartup + seconds;

        private static bool Settled() => EditorApplication.timeSinceStartup > _settleUntil;

        private static void TapKey(Key key)
        {
            Keyboard keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }

        // Button.clicked ignores synthetic ClickEvents; buttons get NavigationSubmit.
        private static void Submit(Button button)
        {
            Require(button != null && button.enabledInHierarchy && button.worldBound.width > 0, "Enabled, laid-out button: " + button?.name);
            button.Focus();
            using NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message + " | " + Describe());
            Checks.Add(message);
        }

        private static string Describe()
        {
            RoundSubSystem rounds = Rounds();
            MapEditorSubSystem editor = Editor();
            string view = editor != null ? $"editor={editor.IsActive} view={editor.SystemsViewClient.Requested} "
                + $"segments={editor.SystemsViewClient.Structure?.Segments.Count}" : "editor=-";
            return $"online={Online()} round={(rounds != null ? rounds.CurrentRoundState.ToString() : "-")} spawned={Spawned()} {view}";
        }

        private static void Capture(string name) =>
            ScreenCapture.CaptureScreenshot(Path.Combine(SessionState.GetString(OutputKey, ""), name + ".png"));

        [Serializable]
        private sealed class Report
        {
            public string status;
            public string failure;
            public string station;
            public string[] checks;
            public string[] measurements;
            public string[] errors;
            public string[] postconditions;
            public string scope = "Single-editor host round; the view is driven through its toolbar button, the hover through a "
                + "synthetic Input System pointer and the trace through the Select tool's click handler. Screenshots need visual review; "
                + "no pixel baseline. Does not cover a remote client (mppm:systems-view does).";
        }

        private static void Finish(string failure, bool stopPlay)
        {
            _running = false;
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            Report report = new()
            {
                status = failure == null && Errors.Count == 0 ? "passed" : "failed",
                failure = failure,
                station = Station,
                checks = Checks.ToArray(),
                measurements = Measurements.ToArray(),
                errors = Errors.ToArray(),
            };
            string result = report.status;
            report.status = "verifying";
            Write(report, SessionState.GetString(OutputKey, ""));
            report.status = result;
            _pending = report;
            if (stopPlay) EditorApplication.isPlaying = false;
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
            Debug.Log($"[SS3D Automation] Systems view pilot {report.status}: {output}");
        }

        private static void Write(Report report, string output) =>
            File.WriteAllText(Path.Combine(output, "status.json"), JsonUtility.ToJson(report, true));
    }
}
