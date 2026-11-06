using System.Collections;
using System.Reflection;
using FishNet.Connection;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Tile;
using SS3D.UI.MachineInterface;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>
    /// Filters, mixers and meters from their real prefabs, placed on pipes in the test room, with
    /// each panel opened through the real snapshot RPC.
    /// </summary>
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Atmos)]
    public class AtmosDeviceScenarios : ScenarioFixture
    {
        private const string SurfacePipe = "AtmosPipesSurface";
        private const string SupplyPipe = "AtmosPipesSupply";
        private const float FillKpa = 300f;

        private static AtmosSubSystem Atmos => SubSystems.Get<AtmosSubSystem>();

        private static TileSubSystem Tiles => SubSystems.Get<TileSubSystem>();

        private static MachineInterfaceSubSystem Panels => SubSystems.Get<MachineInterfaceSubSystem>();

        // Guards: the machine filter's prefab wires its port lines and side, moves selected gas out of the side, and its panel opens through the generated snapshot RPC (AtmosFilterController, AtmosThreeWayDeviceBase, FilterInterfaceController, MachineUiAssetCatalog) · Not cheaper because: serialized prefab wiring and FishNet's TargetRpc only exist on a spawned object
        [UnityTest]
        public IEnumerator Filter_DividesALine_AndOpensItsPanel()
        {
            ChangesRoom = true;
            Vector3 centre = RoomTile();
            PlacedTileObject inletPipe = Place(SurfacePipe, centre + Vector3.back);
            PlacedTileObject outletPipe = Place(SurfacePipe, centre + Vector3.forward);
            PlacedTileObject sidePipe = Place(SurfacePipe, centre + Vector3.left);
            AtmosFilterController filter = PlaceDevice<AtmosFilterController>("AtmosFilterL2", centre);
            FilterInterfaceController panel = filter.GetComponent<FilterInterfaceController>();

            yield return WaitFor(() => filter.TryGetConnectedNetwork(out _), "the filter joins its three lines", 5f);
            filter.ServerToggleGas(AtmosConstants.Plasma);
            Assert.IsTrue(Atmos.PipeRegistry.TryFillNetwork(
                GasPipeSegmentKey.From(inletPipe),
                AtmosGasMix.Pure(AtmosConstants.Plasma),
                FillKpa));

            yield return WaitFor(() => Plasma(sidePipe) > 0f, "plasma leaves by the side port", 5f);
            Assert.AreEqual(0f, Plasma(outletPipe), "Selected gas doesn't carry straight on");

            yield return OpenPanel(panel, MachineInterfaceIds.Filter);
        }

        // Guards: a mixer plate finds the underfloor line at its ports, takes both inlets at its ratio, and its panel opens (AtmosMixerController, TryResolveThreeWayLine, MixerInterfaceController) · Not cheaper because: the plate's line choice runs against a live tilemap and networks built from a real prefab
        [UnityTest]
        public IEnumerator MixerPlate_CombinesTwoUnderfloorLines_AndOpensItsPanel()
        {
            ChangesRoom = true;
            Vector3 centre = RoomTile();
            PlacedTileObject inlet1 = Place(SupplyPipe, centre + Vector3.back);
            PlacedTileObject outlet = Place(SupplyPipe, centre + Vector3.forward);
            PlacedTileObject inlet2 = Place(SupplyPipe, centre + Vector3.left);
            AtmosMixerController mixer = PlaceDevice<AtmosMixerController>("AtmosMixerL", centre);

            yield return WaitFor(() => mixer.TryGetConnectedNetwork(out _), "the plate finds its line", 5f);
            Atmos.PipeRegistry.TryFillNetwork(GasPipeSegmentKey.From(inlet1), AtmosGasMix.Pure(AtmosConstants.Oxygen), FillKpa);
            Atmos.PipeRegistry.TryFillNetwork(GasPipeSegmentKey.From(inlet2), AtmosGasMix.Pure(AtmosConstants.Nitrogen), FillKpa);

            GasPipeNetworkRecord Line() => Network(outlet);
            yield return WaitFor(
                () => Line().GetPressure(AtmosConstants.DefaultGasCount) >= AtmosPortConstants.PumpDefaultTargetKpa - 1f,
                "the outlet reaches its target",
                5f);

            GasPipeNetworkRecord line = Line();
            Assert.That(
                line.Moles[AtmosConstants.Oxygen.Value],
                Is.EqualTo(line.Moles[AtmosConstants.Nitrogen.Value]).Within(0.01f),
                "An even ratio takes as much from each inlet");

            yield return OpenPanel(mixer.GetComponent<MixerInterfaceController>(), MachineInterfaceIds.Mixer);
        }

        // Guards: a meter on a pipe finds its line, needs no power, and its read-only panel opens for anyone (AtmosMeterController, MeterInterfaceController) · Not cheaper because: the panel's snapshot goes through FishNet's generated TargetRpc on a spawned object
        [UnityTest]
        public IEnumerator Meter_ReadsItsLine_AndOpensItsPanel()
        {
            ChangesRoom = true;
            Vector3 centre = RoomTile();
            PlacedTileObject pipe = Place(SupplyPipe, centre);
            AtmosMeterController meter = Place("AtmosMeter", centre).GetComponent<AtmosMeterController>();

            yield return WaitFor(() => meter.TryGetLine(out _), "the meter finds its line", 5f);
            Atmos.PipeRegistry.TryFillNetwork(GasPipeSegmentKey.From(pipe), AtmosGasMix.Air(), FillKpa);

            yield return WaitFor(() => HistoryNewest(meter) > FillKpa - 1f, "a sample lands in the history", 5f);
            yield return OpenPanel(meter.GetComponent<MeterInterfaceController>(), MachineInterfaceIds.Meter);
        }

        private static float HistoryNewest(AtmosMeterController meter)
        {
            var history = new float[AtmosMeterController.HistoryLength];
            meter.CopyHistory(history);
            return history[history.Length - 1];
        }

        private static float Plasma(PlacedTileObject pipe) => Network(pipe).Moles[AtmosConstants.Plasma.Value];

        private static GasPipeNetworkRecord Network(PlacedTileObject pipe)
        {
            Assert.IsTrue(Atmos.PipeRegistry.TryGetNetworkForSegment(GasPipeSegmentKey.From(pipe), out _, out GasPipeNetworkRecord record));
            return record;
        }

        private Vector3 RoomTile()
        {
            Vector3 body = Body.transform.position;
            return new Vector3(Mathf.Round(body.x) + 2f, 0f, Mathf.Round(body.z) + 2f);
        }

        private static PlacedTileObject Place(string asset, Vector3 position)
        {
            var tile = (TileObjectSo)Tiles.GetAsset(asset);
            Assert.IsTrue(
                Tiles.CurrentMap.PlaceTileObject(tile, position, Direction.North, true, false, false, out GameObject placed),
                $"{asset} places at {position}");
            return placed.GetComponent<PlacedTileObject>();
        }

        // Power comes from an APC this room doesn't have; the wiring under test is everything else.
        private static T PlaceDevice<T>(string asset, Vector3 position) where T : AtmosPortControllerBase
        {
            T device = Place(asset, position).GetComponent<T>();
            typeof(AtmosPortControllerBase)
                .GetField("_powerConsumer", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(device, null);
            return device;
        }

        private IEnumerator OpenPanel(MachineInterfaceBehaviour panel, string interfaceId)
        {
            typeof(MachineInterfaceBehaviour)
                .GetMethod("OpenInterfaceForViewer", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(panel, new object[] { (NetworkConnection)Host.Owner });

            yield return WaitFor(() => Panels.IsOpen, $"{interfaceId} opens on the client", 3f);
            yield return Hold(0.5f);
            Panels.Close();
            yield return WaitFor(() => !Panels.IsOpen, $"{interfaceId} closes", 3f);
        }
    }
}
