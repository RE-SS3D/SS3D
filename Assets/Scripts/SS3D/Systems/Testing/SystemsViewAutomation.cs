using FishNet;
using SS3D.Core;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Atmospherics.SystemsView;
using SS3D.Systems.Electricity;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.Authorizers;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.MapEditor.UI;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Testing
{
    /// <summary>
    /// Automation steps for mppm:systems-view (Documents/architecture/systems/map-editor-systems-views.md, Tests):
    /// a pure client opens the map editor, turns the Atmos view on, receives structure, traces a valve's two
    /// networks and sees their pressures meet once the host opens it. Both sides pick the same valve: the one with a
    /// different network on each side, lowest tile first (Classic MiniStation has one, on Waste, open at round start,
    /// so the host shuts it first).
    /// </summary>
    public static class SystemsViewAutomation
    {
        // The pressures the host gives each side of the valve, far enough apart that only the valve closes the gap.
        private const float BackKpa = 400f;
        private const float FrontKpa = 5f;

        /// <summary>The script lines this class runs (<see cref="Run"/>), host side then client side.</summary>
        public static readonly System.Collections.Generic.HashSet<string> Opcodes = new()
        {
            "editor_for_clients", "prime_valve", "wait_systems_trace", "open_primed_valve", "restore_editor_access",
            "open_map_editor", "systems_view", "trace_valve", "assert_trace_pressure_change", "close_map_editor",
            "systems_simulation", "systems_steps", "assert_simulation_running", "assert_pause_refused", "assert_trace_paused_step",
        };

        /// <summary>
        /// Runs one script line. Host: <c>editor_for_clients</c> lets clients open the editor (as on <c>-sandbox</c>)
        /// until <c>restore_editor_access</c>; <c>prime_valve</c> fills the chosen valve's sides apart;
        /// <c>wait_systems_trace</c> waits for a client to trace a two-sided device; <c>open_primed_valve</c>. Client:
        /// <c>open_map_editor</c> / <c>close_map_editor</c>; <c>systems_view atmos</c>; <c>trace_valve</c>;
        /// <c>assert_trace_pressure_change</c>.
        /// </summary>
        public static IEnumerator Run(object sender, string opcode, string[] args, float timeoutSeconds)
        {
            switch (opcode)
            {
                case "editor_for_clients":
                    OpenEditorToClients();
                    TestSignal.Emit(sender, "EditorOpenToClients");
                    break;
                case "restore_editor_access":
                    RestoreEditorAccess();
                    break;
                case "prime_valve":
                    TestSignal.Emit(sender, "ValvePrimed", PrimeValve());
                    break;
                case "wait_systems_trace":
                    yield return WaitForTrace(timeoutSeconds);
                    TestSignal.Emit(sender, "SystemsTraceSeen");
                    break;
                case "open_primed_valve":
                    OpenPrimedValve();
                    TestSignal.Emit(sender, "ValveOpened");
                    break;
                case "open_map_editor":
                    yield return OpenMapEditor(timeoutSeconds);
                    TestSignal.Emit(sender, "MapEditorOpened");
                    break;
                case "close_map_editor":
                    yield return CloseMapEditor(timeoutSeconds);
                    break;
                case "systems_view":
                    yield return ShowView(args[0], timeoutSeconds);
                    TestSignal.Emit(sender, "SystemsViewStructure", args[0]);
                    break;
                case "trace_valve":
                    yield return TraceValve(timeoutSeconds);
                    TestSignal.Emit(sender, "ValveTraced");
                    break;
                case "assert_trace_pressure_change":
                    string seen = null;
                    yield return AssertTracePressureChange(timeoutSeconds, text => seen = text);
                    TestSignal.Emit(sender, "TracePressureChanged", seen);
                    break;
                case "systems_simulation":
                    yield return RequestSimulation(Enum.Parse<SystemsViewSimulationAction>(args[0], ignoreCase: true), timeoutSeconds);
                    TestSignal.Emit(sender, "SystemsSimulation", args[0]);
                    break;
                case "systems_steps":
                    string stepped = null;
                    yield return StepSimulation(int.Parse(args[0], CultureInfo.InvariantCulture), timeoutSeconds, text => stepped = text);
                    TestSignal.Emit(sender, "SystemsStepped", stepped);
                    break;
                case "assert_simulation_running":
                    yield return AssertSimulationRunning(timeoutSeconds);
                    TestSignal.Emit(sender, "SystemsSimulationRunning");
                    break;
                case "assert_pause_refused":
                    yield return AssertPauseRefused(timeoutSeconds);
                    TestSignal.Emit(sender, "SystemsPauseRefused");
                    break;
                case "assert_trace_paused_step":
                    string steppedSeen = null;
                    yield return AssertTracePausedStep(timeoutSeconds, text => steppedSeen = text);
                    TestSignal.Emit(sender, "TracePausedStepped", steppedSeen);
                    break;
                default:
                    throw new InvalidOperationException($"SystemsViewAutomation has no step '{opcode}'");
            }
        }

        private static AtmosValveController _primed;
        private static bool _editorOpenedToClients;
        private static string _savedLastTab;
        private static bool _hadLastTab;

        /// <summary>Host: every non-Restricted player may open the editor, as on <c>-sandbox</c>, until <see cref="RestoreEditorAccess"/>.</summary>
        public static void OpenEditorToClients()
        {
            MapEditorPermissions.SetAuthorizer(new OpenBuildMapEditorAuthorizer());
            _editorOpenedToClients = true;
        }

        /// <summary>Host: back to the server's own rule (domain reload on leaving Play Mode resets it anyway).</summary>
        public static void RestoreEditorAccess()
        {
            if (_editorOpenedToClients)
                MapEditorPermissions.SetAuthorizer(null);
            _editorOpenedToClients = false;
        }

        /// <summary>
        /// The script ended, passed or not (<c>AutomationSubSystem.Finish</c>): undo what a failed or timed-out step
        /// left behind, the editor opened to clients and the primed valve.
        /// </summary>
        public static void Reset()
        {
            RestoreEditorAccess();
            _primed = null;
            // A run never leaves the server's simulations paused (map-editor-systems-views, Pause and step).
            if (InstanceFinder.IsServer && SubSystems.TryGet(out MapEditorSubSystem editor))
                editor.SimulationPause.ResumeAll();
        }

        /// <summary>
        /// Host, with its editor open: Pause or Resume through the editor's own request (the admin-gated ServerRpc), then
        /// waits until the server's atmos and electricity channels say so.
        /// </summary>
        public static IEnumerator RequestSimulation(SystemsViewSimulationAction action, float timeoutSeconds)
        {
            MapEditorSubSystem editor = SubSystems.Get<MapEditorSubSystem>();
            editor.RequestSystemsSimulation(action);
            bool paused = action == SystemsViewSimulationAction.Pause;
            yield return Wait(() => editor.SimulationPause.IsPaused == paused && ServerChannelsPaused() == paused,
                timeoutSeconds, $"systems_simulation {action}");
        }

        /// <summary>Host: <paramref name="count"/> Steps, one a frame; each is exactly one atmos and one electricity tick.</summary>
        public static IEnumerator StepSimulation(int count, float timeoutSeconds, Action<string> report)
        {
            MapEditorSubSystem editor = SubSystems.Get<MapEditorSubSystem>();
            AtmosSubSystem atmos = SubSystems.Get<AtmosSubSystem>();
            if (!editor.SimulationPause.IsPaused)
                throw new InvalidOperationException("systems_steps: pause first");

            int atmosFrom = atmos.TickCount;
            long powerFrom = ElectricitySubSystem.Channel.TickCount;
            for (int i = 0; i < count; i++)
            {
                int expected = atmosFrom + i + 1;
                editor.RequestSystemsSimulation(SystemsViewSimulationAction.Step);
                yield return Wait(() => atmos.TickCount >= expected, timeoutSeconds, $"step {i + 1}");
            }

            // A few frames more: a paused channel must not tick by itself.
            for (int i = 0; i < 10; i++)
                yield return null;
            int atmosTicks = atmos.TickCount - atmosFrom;
            long powerTicks = ElectricitySubSystem.Channel.TickCount - powerFrom;
            if (atmosTicks != count || powerTicks != count)
                throw new InvalidOperationException($"systems_steps {count}: atmos ran {atmosTicks} ticks, electricity {powerTicks}");
            report($"{count} steps: {atmosTicks} atmos and {powerTicks} electricity ticks");
        }

        /// <summary>Host: nothing holds the simulations any more (the pausing editor closed), and atmos ticks again.</summary>
        public static IEnumerator AssertSimulationRunning(float timeoutSeconds)
        {
            MapEditorSubSystem editor = SubSystems.Get<MapEditorSubSystem>();
            AtmosSubSystem atmos = SubSystems.Get<AtmosSubSystem>();
            yield return Wait(() => !editor.SimulationPause.IsPaused && !ServerChannelsPaused(), timeoutSeconds, "the simulations to resume");
            int from = atmos.TickCount;
            yield return Wait(() => atmos.TickCount > from + 2, timeoutSeconds, "atmos to tick by itself again");
        }

        private static bool ServerChannelsPaused() => AtmosSubSystem.Channel.Paused && ElectricitySubSystem.Channel.Paused;

        /// <summary>
        /// Client, not an admin: its editor offers no Pause or Step, and the request sent anyway is refused by the server,
        /// which says so in a toast.
        /// </summary>
        public static IEnumerator AssertPauseRefused(float timeoutSeconds)
        {
            MapEditorSubSystem editor = SubSystems.Get<MapEditorSubSystem>();
            MapEditorViewModel vm = editor.AutomationViewModel;
            if (vm.MaySystemsSimulationControl)
                throw new InvalidOperationException("assert_pause_refused: the server offered this client the pause");
            VisualElement pause = Find("systems-sim-pause");
            if (pause != null && pause.resolvedStyle.display != DisplayStyle.None)
                throw new InvalidOperationException("assert_pause_refused: the client's bar shows Pause");

            editor.RequestSystemsSimulation(SystemsViewSimulationAction.Pause);
            yield return Wait(() => vm.ToastMessage == "Only admins can pause or step the simulation.", timeoutSeconds,
                "the server to refuse the client's pause");
            if (vm.SystemsSimulationPaused)
                throw new InvalidOperationException("assert_pause_refused: the client reads the simulation as paused");
        }

        /// <summary>
        /// Client, tracing the valve: the host admin pauses (the live numbers say Paused and the panel shows it), re-primes the
        /// valve and opens it while paused; the gap holds while nothing steps, then closes some once the host steps, still
        /// paused; then the live numbers stop saying Paused when the host's editor closes. Returns what it saw.
        /// </summary>
        public static IEnumerator AssertTracePausedStep(float timeoutSeconds, Action<string> report)
        {
            MapEditorSystemsViewClient client = SubSystems.Get<MapEditorSubSystem>().SystemsViewClient;
            Vector2Int tile = client.TraceOrigin;
            yield return Wait(() => client.Live is { Paused: true } && Find("systems-trace-paused") != null, timeoutSeconds,
                "the trace to read Paused, in the live numbers and the panel");

            float held = 0f;
            yield return Wait(() => IsOpen(client.Structure, tile) && Gap(client, out _, out _) is { } gap && gap > 100f && (held = gap) > 0f,
                timeoutSeconds, "the host to re-prime the valve and open it while paused");
            float start = Time.time;
            yield return Wait(() => Time.time - start > 1.5f, timeoutSeconds, "a moment of nothing stepping");
            if (Gap(client, out _, out _) is not { } still || Mathf.Abs(still - held) > 0.05f || client.Live is not { Paused: true })
                throw new InvalidOperationException($"assert_trace_paused_step: the gap moved with nothing stepping ({held:0.0} kPa)");

            float stepped = 0f;
            yield return Wait(() => client.Live is { Paused: true } && Gap(client, out _, out _) is { } gap && gap < held - 0.5f && (stepped = gap) >= 0f,
                timeoutSeconds, $"a step to change the traced pressures while paused (gap {held:0.0} kPa)");
            yield return Wait(() => client.Live is { Paused: false }, timeoutSeconds, "the pause to lift when the host's editor closes");
            report($"paused gap {held:0.0} kPa, after steps {stepped:0.0} kPa, then resumed");
        }

        private static VisualElement Find(string name)
        {
            foreach (UIDocument document in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                VisualElement element = document.rootVisualElement?.Q(name);
                if (element != null)
                    return element;
            }

            return null;
        }

        /// <summary>Server: shuts the chosen valve and fills its back network high and its front low. Returns what it did.</summary>
        public static string PrimeValve()
        {
            AtmosSubSystem atmos = SubSystems.Get<AtmosSubSystem>();
            TileMap map = SubSystems.Get<TileSubSystem>().CurrentMap;
            GasPipeNetworkRegistry registry = atmos.PipeRegistry;
            foreach (AtmosValveController valve in atmos.PortRegistry.Ports.OfType<AtmosValveController>()
                         .OrderBy(valve => valve.OriginTile.Grid.x).ThenBy(valve => valve.OriginTile.Grid.y))
            {
                Direction facing = valve.GetComponent<PlacedTileObject>().Direction;
                if (!AtmosDevicePipeResolver.TryResolveInlineNetworks(map, registry, valve.OriginTile, facing, out GasPipeNetworkId back, out GasPipeNetworkId front)
                    || back.Equals(front) || !registry.TryGetNetwork(back, out GasPipeNetworkRecord backRecord)
                    || !registry.TryGetNetwork(front, out GasPipeNetworkRecord frontRecord))
                    continue;

                valve.ServerSetEnabled(false);
                Fill(backRecord, BackKpa);
                Fill(frontRecord, FrontKpa);
                _primed = valve;
                return $"{valve.OriginTile.Grid} back {BackKpa} kPa, front {FrontKpa} kPa";
            }

            throw new InvalidOperationException("prime_valve: no valve with a different pipe network on each side");
        }

        private static void Fill(GasPipeNetworkRecord record, float kpa)
        {
            float[] nitrogen = new float[record.Moles.Length];
            nitrogen[AtmosConstants.Nitrogen.Value] = 1f;
            AtmosGasMix.FillMoles(nitrogen, AtmosPipeThermo.GetMolesForPressure(kpa, record.Temperature, record.Volume), record.Moles);
        }

        /// <summary>Server: a client traces two networks or more (a two-port device).</summary>
        public static IEnumerator WaitForTrace(float timeoutSeconds)
        {
            MapEditorSubSystem editor = SubSystems.Get<MapEditorSubSystem>();
            yield return Wait(() => InstanceFinder.ServerManager.Clients.Keys.Any(id => editor.SystemsViews.TracedBy(id).Count >= 2),
                timeoutSeconds, "wait_systems_trace");
        }

        public static void OpenPrimedValve()
        {
            if (_primed == null)
                throw new InvalidOperationException("open_primed_valve: run prime_valve first");
            _primed.ServerSetEnabled(true);
        }

        /// <summary>
        /// Client: opens the editor the way its key does (the server grants it). The editor remembers its tab in
        /// PlayerPrefs on open; that is put back at once, so the run leaves the machine's prefs as they were.
        /// </summary>
        public static IEnumerator OpenMapEditor(float timeoutSeconds)
        {
            MapEditorSubSystem editor = SubSystems.Get<MapEditorSubSystem>();
            _hadLastTab = PlayerPrefs.HasKey(MapEditorLastTab.PrefsKey);
            _savedLastTab = PlayerPrefs.GetString(MapEditorLastTab.PrefsKey, null);
            editor.AutomationToggleOpen();
            yield return Wait(() => editor.IsActive, timeoutSeconds, "open_map_editor");
            RestoreLastTab();
        }

        public static IEnumerator CloseMapEditor(float timeoutSeconds)
        {
            MapEditorSubSystem editor = SubSystems.Get<MapEditorSubSystem>();
            if (editor.IsActive)
                editor.AutomationToggleOpen();
            yield return Wait(() => !editor.IsActive, timeoutSeconds, "close_map_editor");
            RestoreLastTab();
        }

        private static void RestoreLastTab()
        {
            if (_hadLastTab)
                PlayerPrefs.SetString(MapEditorLastTab.PrefsKey, _savedLastTab);
            else
                PlayerPrefs.DeleteKey(MapEditorLastTab.PrefsKey);
            PlayerPrefs.Save();
        }

        /// <summary>Client: pins a view from the toolbar's handler and waits for the server's structure.</summary>
        public static IEnumerator ShowView(string view, float timeoutSeconds)
        {
            SystemsViewKind kind = Enum.Parse<SystemsViewKind>(view, ignoreCase: true);
            MapEditorSubSystem editor = SubSystems.Get<MapEditorSubSystem>();
            editor.AutomationSelectSystemsView(kind);
            yield return Wait(() => editor.SystemsViewClient.Structure is { } structure && structure.View == kind
                && structure.Segments.Count > 0 && structure.Badges.Count > 0, timeoutSeconds, $"systems_view {view}");
        }

        /// <summary>Client: Select's trace on the chosen valve, framed by the camera; waits for both sides' numbers.</summary>
        public static IEnumerator TraceValve(float timeoutSeconds)
        {
            MapEditorSubSystem editor = SubSystems.Get<MapEditorSubSystem>();
            MapEditorSystemsViewClient client = editor.SystemsViewClient;
            SystemsViewBadge valve = ChosenValve(client.Structure)
                ?? throw new InvalidOperationException("trace_valve: the structure has no valve with a different network on each side");
            if (!client.BeginTrace(valve.Key.Tile, (TileLayer)valve.Key.Slot))
                throw new InvalidOperationException($"trace_valve: Select on the valve at {valve.Key.Tile} traced nothing");

            editor.AutomationCenterOn(new Vector3(valve.Key.Tile.x, 0f, valve.Key.Tile.y));
            yield return Wait(() => Gap(client, out _, out _) != null, timeoutSeconds, "trace_valve live numbers");
            if (client.TraceSides.Count != 2 || client.TraceSides[0].Side != "Back" || client.TraceSides[1].Side != "Front")
                throw new InvalidOperationException("trace_valve: the valve's two sides aren't labelled Back and Front");
        }

        /// <summary>
        /// Client: the valve's badge shows it shut, then open; after that the two traced pressures close at least half the
        /// gap they had while it was shut, in the live numbers and in the panel. Returns what it saw.
        /// </summary>
        public static IEnumerator AssertTracePressureChange(float timeoutSeconds, Action<string> report)
        {
            MapEditorSystemsViewClient client = SubSystems.Get<MapEditorSubSystem>().SystemsViewClient;
            Vector2Int tile = client.TraceOrigin;
            float closedGap = 0f;
            string closedReading = null;
            yield return Wait(() =>
            {
                if (IsOpen(client.Structure, tile))
                    return closedReading != null;
                // Shut, and the host's fill read since: the gap to close.
                if (Gap(client, out float back, out float front) is { } gap)
                {
                    closedGap = gap;
                    closedReading = $"{back:0.0}/{front:0.0} kPa";
                }

                return false;
            }, timeoutSeconds, "the traced valve's badge to show it open");

            string openReading = null;
            yield return Wait(() =>
            {
                if (Gap(client, out float back, out float front) is not { } gap || gap > closedGap * 0.5f)
                    return false;
                openReading = $"{back:0.0}/{front:0.0} kPa";
                return true;
            }, timeoutSeconds, $"the traced pressures to meet once the valve opened (shut: {closedReading})");

            string shown = client.Live.Value.Networks[0].ValueOf("Pressure");
            yield return Wait(() => PanelShows(shown), timeoutSeconds, $"the trace panel to show {shown}");
            report($"valve {tile}: shut {closedReading}, open {openReading}");
        }

        private static SystemsViewBadge? ChosenValve(SystemsViewStructure structure) => structure?.Badges.Values
            .Where(b => b.Kind == AtmosSystemsViewCodes.KindValve && b.ViewId > 0 && b.OtherViewId > 0 && b.ViewId != b.OtherViewId)
            .OrderBy(b => b.Key.Tile.x).ThenBy(b => b.Key.Tile.y)
            .Select(b => (SystemsViewBadge?)b).FirstOrDefault();

        private static bool IsOpen(SystemsViewStructure structure, Vector2Int tile) => structure != null && structure.Badges.Values.Any(b =>
            b.Key.Tile == tile && b.Kind == AtmosSystemsViewCodes.KindValve && AtmosSystemsViewCodes.SwitchOf(b.State) != AtmosSystemsViewCodes.Off);

        /// <summary>|back - front| from the latest live numbers of a two-sided trace; null until both have arrived.</summary>
        private static float? Gap(MapEditorSystemsViewClient client, out float back, out float front)
        {
            back = front = 0f;
            if (client.Live is not { } live || live.Networks.Length < 2 || client.TraceSides.Count < 2
                || !TryPressure(live, client.TraceSides[0].ViewId, out back) || !TryPressure(live, client.TraceSides[1].ViewId, out front))
                return null;
            return Mathf.Abs(back - front);
        }

        private static bool TryPressure(SystemsViewLive live, int viewId, out float kpa)
        {
            kpa = 0f;
            string value = live.TryFind(viewId, out SystemsViewLiveNetwork network) ? network.ValueOf("Pressure") : null;
            return value != null && value.EndsWith(" kPa", StringComparison.Ordinal)
                && float.TryParse(value[..^4], NumberStyles.Float, CultureInfo.InvariantCulture, out kpa);
        }

        private static bool PanelShows(string text)
        {
            foreach (UIDocument document in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                VisualElement panel = document.rootVisualElement?.Q("systems-trace");
                if (panel != null && panel.resolvedStyle.display == DisplayStyle.Flex
                    && panel.Query<Label>().ToList().Any(label => label.text == text))
                    return true;
            }

            return false;
        }

        private static IEnumerator Wait(Func<bool> condition, float timeoutSeconds, string label)
        {
            float start = Time.time;
            while (!condition())
            {
                if (Time.time - start > timeoutSeconds)
                    throw new TimeoutException($"Timed out after {timeoutSeconds}s waiting for {label}");
                yield return null;
            }
        }
    }
}
