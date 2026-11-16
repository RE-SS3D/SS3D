using System.Linq;
using SS3D.Core;
using SS3D.Rendering.URP;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.SystemsView;
using SS3D.Systems.Electricity;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
using static SS3D.Editor.Automation.MapEditorSystemsPilot;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// The pause, step and Room air half of <see cref="MapEditorSystemsPilot"/> (Documents/architecture/systems/map-editor-systems-views.md, Tests):
    /// the Atmos view back on with Room air, the host admin pausing from the bar, a puff of nitrogen stepped in one tick so
    /// the pressure tint has something to show (with the fog, glow and wind still hidden), a trace while paused, a step that
    /// is exactly one atmos and one electricity tick, then Resume, Room air off and the view off. Runs after the Power steps;
    /// <see cref="Reset"/> resumes the simulations on an abort, so a run never leaves them paused.
    /// </summary>
    internal static class MapEditorSystemsAirPilot
    {
        private const float PuffMoles = 400f;
        private static Vector2Int _puff;
        private static int _ticks;
        private static long _powerTicks;

        /// <summary>Resumes what an aborted run paused.</summary>
        internal static void Reset()
        {
            if (SubSystems.TryGet(out MapEditorSubSystem editor) && FishNet.InstanceFinder.IsServer)
                editor.SimulationPause.ResumeAll();
        }

        private static AtmosSubSystem Atmos() => SubSystems.Get<AtmosSubSystem>();

        private static long PowerTicks() => ElectricitySubSystem.Channel.TickCount;

        private static bool ServerPaused() => MapEditorSystemsPilot.Editor().SimulationPause.IsPaused && AtmosSubSystem.Channel.Paused
            && ElectricitySubSystem.Channel.Paused;

        private static VisualElement Bar() => EditorRoot().Q("systems-sim");

        internal static Step[] Steps() => new Step[]
        {
            new Step
            {
                Name = "Atmos view back on, Room air on from the bar",
                Enter = () =>
                {
                    Submit(EditorRoot().Q<Button>("systems-view-atmos"));
                    Vector2Int tile = TraceTile;
                    MapEditorSystemsPilot.Editor().AutomationCenterOn(new Vector3(tile.x, 0f, tile.y));
                },
                Until = () => Client().Structure is { View: SystemsViewKind.Atmos } && Bar()?.resolvedStyle.display == DisplayStyle.Flex
                    && EditorRoot().Q<Button>("systems-room-air") is { } button && button.worldBound.width > 0,
                Timeout = 15,
                Verify = () =>
                {
                    Require(EditorRoot().Q("systems-sim-pause").resolvedStyle.display == DisplayStyle.Flex,
                        "The host admin's bar offers Pause");
                    Require(EditorRoot().Q("systems-sim-paused").resolvedStyle.display == DisplayStyle.None, "Nothing is paused yet");
                    Submit(EditorRoot().Q<Button>("systems-room-air"));
                },
            },
            new Step
            {
                Name = "Room air tints the floors",
                Until = () => Client().RoomAirRequested && Client().RoomAirOverlay.IsVisible && Client().RoomAirOverlay.TintedTiles > 0,
                Timeout = 10,
                Verify = () =>
                {
                    Require(AtmosRenderContext.IsRenderingSuppressed, "The editor still hides the fog, glow and wind");
                    Require(AtmosRenderContext.FullAtlasRequested, "The host's snapshot covers the whole station while Room air is on");
                    AddMeasurement($"room air: {Client().RoomAirOverlay.TintedTiles} tinted tiles");
                    Settle(0.6);
                },
            },
            new Step
            {
                Name = "Pause from the bar",
                Enter = () => Submit(EditorRoot().Q<Button>("systems-sim-pause")),
                Until = () => ServerPaused() && MapEditorSystemsPilot.Editor().AutomationViewModel.SystemsSimulationPaused,
                Timeout = 5,
                Verify = () =>
                {
                    Require(EditorRoot().Q("systems-sim-paused").resolvedStyle.display == DisplayStyle.Flex, "The bar says PAUSED");
                    Require(EditorRoot().Q<Button>("systems-sim-pause").text == "Resume", "Pause became Resume");
                    _ticks = Atmos().TickCount;
                    _powerTicks = PowerTicks();
                    Settle(1);
                },
            },
            new Step
            {
                Name = "Paused, nothing ticks; a puff of nitrogen and one Step",
                Until = Settled,
                Verify = () =>
                {
                    Require(Atmos().TickCount == _ticks && PowerTicks() == _powerTicks, "Paused, neither atmos nor electricity ticks");
                    _puff = PickPuffTile();
                    TileMap map = SubSystems.Get<TileSubSystem>().CurrentMap;
                    Atmos().DebugAddGas(new TileCoord(map.MapId, _puff), AtmosConstants.Nitrogen, PuffMoles);
                    Submit(EditorRoot().Q<Button>("systems-sim-step"));
                },
            },
            new Step
            {
                Name = "The step is one tick, and Room air shows the puff",
                Until = () => Atmos().TickCount > _ticks && Client().RoomAirOverlay.TryGetLevel(_puff, out int level)
                    && level > AtmosRoomAirPalette.LevelOf(130f),
                Timeout = 5,
                Verify = () =>
                {
                    Require(Atmos().TickCount == _ticks + 1, $"One Step is one atmos tick ({Atmos().TickCount - _ticks})");
                    Require(PowerTicks() == _powerTicks + 1, $"and one electricity tick ({PowerTicks() - _powerTicks})");
                    Require(ServerPaused(), "Still paused after the step");
                    MapEditorSystemsPilot.Editor().AutomationCenterOn(new Vector3(_puff.x, 0f, _puff.y));
                    Settle(1.2);
                },
            },
            new Step
            {
                Name = "Room air screenshot",
                Until = Settled,
                Verify = () =>
                {
                    Client().RoomAirOverlay.TryGetLevel(_puff, out int level);
                    AddMeasurement($"room air puff at {_puff}: step {level} of {AtmosRoomAirPalette.Levels}, "
                        + $"{Client().RoomAirOverlay.TintedTiles} tinted tiles");
                    Capture("30-atmos-room-air");
                },
            },
            new Step
            {
                Name = "Select traces the device while paused",
                Enter = () =>
                {
                    MapEditorSystemsPilot.Editor().AutomationSelectTool(MapEditorTool.Select);
                    MapEditorSystemsPilot.Editor().AutomationCenterOn(new Vector3(TraceTile.x, 0f, TraceTile.y));
                    Settle(1.2);
                },
                Until = Settled,
                Verify = () =>
                {
                    Point(TraceTile);
                    Point(TraceTile, InputUpdateType.Editor);
                    MapEditorSystemsPilot.Editor().AutomationSelectClick();
                    Require(Client().IsTracing, $"Select on {TraceTile} traced its network(s) while paused");
                },
            },
            new Step
            {
                Name = "The paused trace says so",
                Until = () => Client().Live is { Paused: true } live && live.Networks.Length == Client().TraceSides.Count
                    && EditorRoot().Q("systems-trace-paused") != null,
                Timeout = 5,
                Verify = () =>
                {
                    Settle(0.6);
                },
            },
            new Step
            {
                Name = "Paused trace screenshot",
                Until = Settled,
                Verify = () =>
                {
                    AddMeasurement("paused trace: " + string.Join(" | ", Client().Live.Value.Networks.Select(n => n.ValueOf("Pressure"))));
                    Capture("31-atmos-paused-trace");
                    _ticks = Atmos().TickCount;
                    Submit(EditorRoot().Q<Button>("systems-sim-step"));
                },
            },
            new Step
            {
                Name = "A step while tracing is one more tick, still paused",
                Until = () => Atmos().TickCount > _ticks,
                Timeout = 5,
                Verify = () =>
                {
                    Require(Atmos().TickCount == _ticks + 1, "A second Step is one more atmos tick");
                    Require(Client().Live is { Paused: true }, "The trace still reads Paused");
                    Submit(EditorRoot().Q<Button>("systems-sim-pause"));
                },
            },
            new Step
            {
                Name = "Resume lifts the pause",
                Until = () => !ServerPaused() && !MapEditorSystemsPilot.Editor().AutomationViewModel.SystemsSimulationPaused && Client().Live is { Paused: false },
                Timeout = 5,
                Verify = () =>
                {
                    Require(EditorRoot().Q("systems-sim-paused").resolvedStyle.display == DisplayStyle.None, "PAUSED is gone");
                    Submit(EditorRoot().Q<Button>("systems-trace-close"));
                    Submit(EditorRoot().Q<Button>("systems-room-air"));
                },
            },
            new Step
            {
                Name = "Room air off, then the view off",
                Until = () => !Client().RoomAirRequested && !Client().RoomAirOverlay.IsVisible,
                Timeout = 5,
                Verify = () =>
                {
                    Require(!AtmosRenderContext.FullAtlasRequested, "The host's snapshot is back to its area of interest");
                    Submit(EditorRoot().Q<Button>("systems-view-atmos"));
                },
            },
        };

        /// <summary>A tinted floor a few tiles from the traced device, so the puff and the device fit one frame.</summary>
        private static Vector2Int PickPuffTile()
        {
            Vector2Int centre = TraceTile;
            MapEditorRoomAirOverlay air = Client().RoomAirOverlay;
            int normal = AtmosRoomAirPalette.LevelOf(101.3f);
            for (int radius = 3; radius <= 10; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    foreach (int dy in new int[] { -radius, radius })
                    {
                        Vector2Int tile = centre + new Vector2Int(dx, dy);
                        if (air.TryGetLevel(tile, out int level) && Mathf.Abs(level - normal) <= 3)
                            return tile;
                    }
                }
            }

            Require(false, $"A tinted floor at normal pressure near {centre}");
            return centre;
        }
    }
}
