using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.Commands;
using SS3D.Systems.Tile.TileMapCreator;
using SS3D.Systems.Tile.Validation;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>
    /// Classic MiniStation's atmos room: the air pump lifts the chamber's air through manifolds into
    /// the Supply line, and scrubbers send what they take to the Waste line, which ends at a valve.
    /// </summary>
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Environment)]
    public class AtmosSupplyScenarios : ScenarioFixture
    {
        private const float TargetRoomKpa = 5f;

        private static readonly Vector2Int Intake = new(2, -10);
        private static readonly Vector2Int Source = new(3, -10);
        private static readonly Vector2Int AirPump = new(2, -7);
        private static readonly Vector2Int WasteValve = new(5, -7);
        private static readonly Vector2Int WasteOutlet = new(7, -8);
        private static readonly RectInt VentedRoom = new(16, 6, 6, 3);
        private static readonly Vector2Int RoomVent = new(17, 7);
        private static readonly Vector2Int RoomScrubber = new(20, 7);

        protected override string StationFixture => "Builds/Game/Data/StationTemplates/classic-ministation.json";

        protected override bool ChecksRoomAir => false;

        private static AtmosSubSystem Atmos => SubSystems.Get<AtmosSubSystem>();

        private static TileCoord Tile(Vector2Int cell) => new(SubSystems.Get<TileSubSystem>().CurrentMap.MapId, cell);

        private static T[] Devices<T>() where T : AtmosPortControllerBase =>
            Object.FindObjectsByType<T>(FindObjectsSortMode.None);

        private static T DeviceAt<T>(Vector2Int cell) where T : AtmosPortControllerBase =>
            Devices<T>().Single(device => device.OriginTile.Grid == cell);

        private static GasPipeNetworkId NetworkOf(AtmosPortControllerBase device)
        {
            Assert.IsTrue(device.TryGetConnectedNetwork(out GasPipeNetworkId network), $"{device.name} at {device.OriginTile.Grid} has a pipe network");
            return network;
        }

        private static VentController[] RoomVents() =>
            Devices<VentController>().Where(vent => vent.OriginTile.Grid != WasteOutlet).ToArray();

        private static GasPipeNetworkRecord Record(GasPipeNetworkId id)
        {
            Assert.IsTrue(Atmos.PipeRegistry.TryGetNetwork(id, out GasPipeNetworkRecord record));
            return record;
        }

        // Guards: the default map must start in every mode; a round refuses one that fails Map Check with no recovery.
        [UnityTest]
        public IEnumerator ClassicMiniStation_PassesMapCheckInEveryMode()
        {
            foreach (string mode in new[] { "Secret", "Extended", "Nuclear Emergency" })
            {
                MapValidationReport report = MapValidationService.Validate(SubSystems.Get<TileSubSystem>(), mode);
                Assert.IsTrue(report.CanStart, report.Describe("classic-ministation", mode, saved: false));
            }

            yield break;
        }

        // Guards: a loaded station starts with Supply filled, the chamber at its fixed mix, the intake parted from Supply by the air pump, and every room scrubber on one Waste line (AtmosGasMarkers, AtmosDevicePipeResolver, manifolds, inline ports) · Not cheaper because: needs the importer's real template loaded through the tilemap, with devices spawned and ticking
        [UnityTest]
        public IEnumerator ClassicMiniStation_StartsWithChamberAndLinesWired()
        {
            yield return WaitFor(() => Atmos.Simulation.GetCellPressure(Tile(Source)) > AtmosPipeConstants.ChamberPressureKpa - 1f,
                "the source holds the chamber at pressure", 10f);
            yield return WaitFor(() => Devices<VentController>().Concat<AtmosPortControllerBase>(Devices<ScrubberController>())
                .Concat(Devices<AtmosInlineDeviceBase>())
                .All(device => device.TryGetConnectedNetwork(out _)), "every device joins its networks", 10f);

            GasPipeNetworkId supply = NetworkOf(RoomVents()[0]);
            Assert.That(RoomVents().Select(NetworkOf), Is.All.EqualTo(supply), "All room vents draw on Supply");
            AtmosPumpController airPump = DeviceAt<AtmosPumpController>(AirPump);
            Assert.AreEqual(supply, NetworkOf(airPump), "The air pump's outlet is Supply");
            Assert.IsTrue(airPump.InletJoined && airPump.OutletJoined, "Both port caps show");
            Assert.AreEqual(AtmosPortConstants.PumpDefaultTargetKpa, airPump.TargetOutletPressureKpa, "A pump starts modest, for atmos to raise");
            Assert.AreNotEqual(supply, NetworkOf(DeviceAt<ScrubberController>(Intake)), "The pump parts the intake from Supply");

            DeviceAt<ScrubberController>(Intake).GetFilterStates(out bool intakeO2, out bool intakeN2, out _, out _);
            Assert.IsTrue(intakeO2 && intakeN2, "The intake carries air");
            DeviceAt<ScrubberController>(RoomScrubber).GetFilterStates(out bool roomO2, out bool roomN2, out bool roomCo2, out _);
            Assert.IsFalse(roomO2 || roomN2, "Room scrubbers leave air alone");
            Assert.IsTrue(roomCo2, "Room scrubbers take CO₂");

            GasPipeNetworkId[] waste = Devices<ScrubberController>().Where(scrubber => scrubber.OriginTile.Grid != Intake)
                .Select(NetworkOf).Distinct().ToArray();
            Assert.AreEqual(1, waste.Length, "Every room scrubber shares one Waste line");
            Assert.AreNotEqual(supply, waste[0], "Room scrubbers stay off Supply");

            TileSubSystem tiles = SubSystems.Get<TileSubSystem>();
            Assert.IsTrue(tiles.GasMarkers.TryGetAt(new Vector3(Source.x, 0f, Source.y), out GasMarkerRecord chamber), "The map saves its chamber as a marker");
            Assert.AreEqual(GasMarkerKind.Source, chamber.Kind);
            Assert.IsTrue(AtmosRoomFlood.Collect(tiles.QueryService, Tile(Source), new List<TileCoord>()), "Map Check takes the chamber for a sealed room");

            GasPipeNetworkRecord record = Record(supply);
            Assert.Greater(record.GetPressure(AtmosConstants.DefaultGasCount), AtmosConstants.StandardPressure,
                "The line starts above room pressure, so vents have air from the first moment");
            Assert.Greater(record.Moles[AtmosConstants.Nitrogen.Value], record.Moles[AtmosConstants.Oxygen.Value], "The line holds air");
        }

        // Guards: gas taken from the chamber by the intake crosses the manifolds and the air pump and reaches a depressurised room through its vent, with Supply's starting fill out of the way (AtmosGasMarkers, ScrubberController, GasPipeManifold, AtmosPumpController, VentController) · Not cheaper because: needs the turf sim, pipe network, powered devices and the shipped map's real run between them
        [UnityTest]
        public IEnumerator Chamber_RefillsADepressurisedRoomThroughItsVent()
        {
            ChangesRoom = true;
            ScrubberController intake = DeviceAt<ScrubberController>(Intake);
            VentController vent = DeviceAt<VentController>(RoomVent);
            AtmosPumpController pump = DeviceAt<AtmosPumpController>(AirPump);
            yield return WaitFor(() => intake.TryGetConnectedNetwork(out _) && vent.TryGetConnectedNetwork(out _)
                && pump.TryGetConnectedNetwork(out _), "the intake, pump and vent join their lines", 10f);

            foreach (ScrubberController scrubber in Devices<ScrubberController>().Where(scrubber => scrubber != intake))
                scrubber.ServerSetEnabled(false);
            foreach (VentController other in Devices<VentController>().Where(other => other != vent))
                other.ServerSetEnabled(false);

            GasPipeNetworkRecord line = Record(NetworkOf(vent));
            System.Array.Clear(line.Moles, 0, line.Moles.Length);
            DrainRoom();
            Assert.Less(RoomPressure(), 1f, "The room starts empty");

            yield return WaitFor(() => RoomPressure() >= TargetRoomKpa, "the vent refills the room from the chamber", 45f);
            Assert.Greater(Atmos.Simulation.GetCellPressure(Tile(Source)), AtmosPipeConstants.ChamberPressureKpa - 1f, "The chamber never runs down");
            Assert.That(line.Moles[AtmosConstants.Oxygen.Value] / line.GetTotalMoles(AtmosConstants.DefaultGasCount),
                Is.EqualTo(0.2f).Within(0.02f), "The intake carries the chamber's mix, not whichever gas it reaches first");
        }

        // Guards: a room scrubber carries CO₂ off the turf into the Waste line and not the distribution line (line attachment on a tile with both lines) · Not cheaper because: needs a powered scrubber spawned on the shipped map's pipes
        [UnityTest]
        public IEnumerator Scrubber_MovesCarbonDioxideToTheWasteLine()
        {
            ChangesRoom = true;
            ScrubberController scrubber = DeviceAt<ScrubberController>(RoomScrubber);
            yield return WaitFor(() => scrubber.TryGetConnectedNetwork(out _), "the scrubber joins its line", 10f);
            GasPipeNetworkId waste = NetworkOf(scrubber);
            GasPipeNetworkId supply = NetworkOf(DeviceAt<VentController>(RoomVent));

            Atmos.Simulation.DebugAddMoles(Tile(RoomScrubber), AtmosConstants.CarbonDioxide, 20f);
            yield return WaitFor(() => Atmos.GetPipeNetworkMoles(waste, AtmosConstants.CarbonDioxide) >= 1f, "CO₂ reaches the waste line", 15f);
            Assert.AreEqual(0f, Atmos.GetPipeNetworkMoles(supply, AtmosConstants.CarbonDioxide), "None of it reaches the distribution line");
        }

        // Guards: Waste stays shut behind its closed valve at round start, and opening the valve lets it out through the hull vent (AtmosValveController, inline ports, a vent powered off the grid outside any room) · Not cheaper because: needs the shipped map's run, a real space cell and the grid-fed vent
        [UnityTest]
        public IEnumerator WasteValve_HoldsTheLine_UntilOpened_ThenVentsItToSpace()
        {
            ChangesRoom = true;
            AtmosValveController valve = DeviceAt<AtmosValveController>(WasteValve);
            VentController outlet = DeviceAt<VentController>(WasteOutlet);
            ScrubberController scrubber = DeviceAt<ScrubberController>(RoomScrubber);
            yield return WaitFor(() => valve.TryGetConnectedNetwork(out _) && scrubber.TryGetConnectedNetwork(out _),
                "the valve and scrubber join their lines", 10f);

            Assert.IsFalse(valve.IsOpen, "The valve starts closed");
            GasPipeNetworkRecord waste = Record(NetworkOf(scrubber));
            foreach (ScrubberController other in Devices<ScrubberController>())
                other.ServerSetEnabled(false);

            waste.Moles[AtmosConstants.CarbonDioxide.Value] += 500f;
            float heldMoles = waste.GetTotalMoles(AtmosConstants.DefaultGasCount);
            yield return Hold(1f);
            Assert.AreEqual(heldMoles, waste.GetTotalMoles(AtmosConstants.DefaultGasCount), 0.01f, "A closed valve holds the line");

            valve.ServerSetEnabled(true);
            yield return WaitFor(() => outlet.IsPortFlowing, "the hull vent runs once the valve opens", 10f);

            // One vent drains the whole 60 m³ line.
            yield return WaitFor(() => waste.GetTotalMoles(AtmosConstants.DefaultGasCount) < heldMoles * 0.9f,
                "Waste drains out through the hull", 30f);
        }

        // Guards: panic takes every gas at full speed with the vents off, and normal puts a scrubber's speed and filters back (AirAlarmPresetDispatcher, ScrubberController) · Not cheaper because: [Server] setters need a spawned scrubber in a real area
        [UnityTest]
        public IEnumerator AirAlarmPresets_SiphonThenRestoreTheScrubber()
        {
            ChangesRoom = true;
            ScrubberController scrubber = DeviceAt<ScrubberController>(RoomScrubber);
            yield return WaitFor(() => scrubber.TryGetConnectedNetwork(out _), "the scrubber joins its line", 10f);

            scrubber.ServerApplyPanicSettings();
            scrubber.GetFilterStates(out bool o2, out bool n2, out bool co2, out bool toxins);
            Assert.IsTrue(o2 && n2 && co2 && toxins, "Panic takes every gas");
            Assert.AreEqual(AtmosPortConstants.ScrubberMaxFlowRate, scrubber.FlowRate);

            scrubber.ServerApplyNormalSettings();
            scrubber.GetFilterStates(out o2, out n2, out co2, out toxins);
            Assert.IsTrue(co2 && !o2 && !n2 && !toxins, "Normal goes back to CO₂");
            Assert.AreEqual(AtmosPortConstants.ScrubberNormalFlowRate, scrubber.FlowRate);
        }

        // Guards: a gas marker survives the real ServerRpc with its gas fractions, draws a pin under the Scripts layer, clears again and undoes (MapEditorSubSystem.RpcExecuteCommands, RpcUndo, MapEditorCommandDto, MapMarkerEditorView) · Not cheaper because: FishNet's generated serializer for the command and the host's editor session need a real spawn, which EditMode has not
        [UnityTest]
        public IEnumerator GasMarker_PlacedThroughTheEditor_DrawsAPin_AndClears()
        {
            ChangesRoom = true;
            MapEditorSubSystem editor = SubSystems.Get<MapEditorSubSystem>();
            GasMarkerRegistry markers = SubSystems.Get<TileSubSystem>().GasMarkers;
            var tile = new Vector3(VentedRoom.xMin, 0f, VentedRoom.yMin);
            GasMarkerRecord sent = GasMarkerRecord.Default(GasMarkerKind.RoomAir);
            sent.Position = tile;
            sent.GasFractions = AtmosGasMix.Pure(AtmosConstants.Plasma);

            SetEditorActive(editor, true);
            bool scriptsWereVisible = TileLayerVisibilityService.IsGroupVisible(TileLayerCategory.Scripts);
            TileLayerVisibilityService.SetGroupVisible(TileLayerCategory.Scripts, true);
            try
            {
                editor.SubmitCommands(new[]
                {
                    new MapEditorCommandDto
                    {
                        Kind = MapEditorCommandKind.PlaceGasMarker,
                        AssetName = MapEditorGasMarkerCatalog.Encode(sent.Kind),
                        Position = tile,
                        GasMarker = sent,
                    },
                });
                yield return WaitFor(() => markers.HasAt(tile), "the server takes the marker", 5f);

                markers.TryGetAt(tile, out GasMarkerRecord placed);
                Assert.AreEqual(sent.GasFractions, placed.GasFractions, "The mix crosses the RPC intact");
                yield return WaitFor(() => PinCount() == 1, "the marker draws one pin", 5f);

                editor.SubmitCommands(new[] { new MapEditorCommandDto { Kind = MapEditorCommandKind.ClearGasMarker, Position = tile } });
                yield return WaitFor(() => !markers.HasAt(tile), "the marker clears", 5f);
                yield return WaitFor(() => PinCount() == 0, "clearing removes the pin", 5f);

                // Undoing both returns to the save point, or the round's end autosaves the test's edits as a map.
                Undo(editor);
                yield return WaitFor(() => markers.HasAt(tile), "undo restores the marker", 5f);
                Undo(editor);
                yield return WaitFor(() => !markers.HasAt(tile), "undo removes the marker", 5f);
                Assert.IsFalse(editor.HasUnsavedChanges, "The editor is back at its save point");
            }
            finally
            {
                markers.TryClearAt(tile, out _);
                TileLayerVisibilityService.SetGroupVisible(TileLayerCategory.Scripts, scriptsWereVisible);
                SetEditorActive(editor, false);
            }
        }

        // The chamber and Supply fill draw pins too; this counts room air only.
        private static int PinCount() => Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Count(pin => pin.name.StartsWith("MarkerPin_Room air"));

        private static void Undo(MapEditorSubSystem editor) =>
            typeof(MapEditorSubSystem).GetMethod("RpcUndo", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(editor, new object[] { editor.LocalConnection });

        private static void SetEditorActive(MapEditorSubSystem editor, bool active) =>
            typeof(MapEditorSubSystem).GetMethod("SetActive", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(editor, new object[] { active });

        private static void DrainRoom()
        {
            AtmosSimulation simulation = Atmos.Simulation;
            foreach (Vector2Int cell in VentedRoom.allPositionsWithin)
            {
                simulation.TryRemoveMoles(Tile(cell), AtmosConstants.Oxygen, float.MaxValue, out _, out _);
                simulation.TryRemoveMoles(Tile(cell), AtmosConstants.Nitrogen, float.MaxValue, out _, out _);
            }
        }

        private static float RoomPressure()
        {
            float total = 0f;
            int cells = 0;
            foreach (Vector2Int cell in VentedRoom.allPositionsWithin)
            {
                total += Atmos.Simulation.GetCellPressure(Tile(cell));
                cells++;
            }

            return total / cells;
        }
    }
}
