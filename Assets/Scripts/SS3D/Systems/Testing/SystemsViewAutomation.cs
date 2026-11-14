using FishNet;
using SS3D.Core;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Atmospherics.SystemsView;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.Authorizers;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Testing
{
    /// <summary>
    /// Automation steps for mppm:systems-view (Documents/architecture/2026-11_map-editor-systems-views.md Tests, Phase 2):
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
