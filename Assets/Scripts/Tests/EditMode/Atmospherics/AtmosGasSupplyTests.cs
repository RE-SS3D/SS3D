using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.GasMarkers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    /// <summary>
    /// The gas a map's storage chambers and starting line fills provide.
    /// </summary>
    public class AtmosGasSupplyTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects)
                Object.DestroyImmediate(go);
            _objects.Clear();
        }

        [Test]
        public void HoldCell_RestoresTheMixEveryTick_WhileGasLeavesThroughABreach()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, 4, 4, out int map);
            var held = new TileCoord(map, 1, 1);
            float[] moles = MixMoles(AtmosGasMix.Pure(AtmosConstants.Oxygen), 200f);

            for (int tick = 0; tick < 20; tick++)
            {
                sim.HoldCell(held, moles, AtmosConstants.StandardTemperature);
                sim.Tick(AtmosConstants.TickInterval);
            }

            sim.HoldCell(held, moles, AtmosConstants.StandardTemperature);
            Assert.That(sim.DebugGetMoles(held, AtmosConstants.Oxygen), Is.EqualTo(200f).Within(0.01f));
            Assert.That(sim.DebugGetMoles(held, AtmosConstants.Nitrogen), Is.EqualTo(0f).Within(0.01f));
            Assert.Greater(sim.DebugGetMoles(new TileCoord(map, 2, 1), AtmosConstants.Oxygen), AtmosConstants.StationOxygenMoles * 1.5f,
                "The held cell pours into its neighbours");
        }

        [Test]
        public void HoldCell_AlreadyAtTheMix_LetsTheCellSleep()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, 4, 4, out int map);
            var held = new TileCoord(map, 1, 1);
            for (int tick = 0; tick < 5; tick++)
                sim.Tick(AtmosConstants.TickInterval);
            Assert.AreEqual(0, sim.ActiveCellCount, "Station air settles");

            float[] moles = MixMoles(AtmosGasMix.Air(), sim.GetCellMoles(CellIndex(sim, held)));
            sim.HoldCell(held, moles, AtmosConstants.StandardTemperature);
            sim.Tick(AtmosConstants.TickInterval);

            Assert.AreEqual(0, sim.ActiveCellCount, "Holding the air a cell already has wakes nothing");
        }

        [Test]
        public void FillMoles_PureMix_ClearsOtherGases()
        {
            var moles = new[] { 5f, 5f, 5f, 5f };
            AtmosGasMix.FillMoles(AtmosGasMix.Pure(AtmosConstants.Oxygen), 10f, moles);
            Assert.AreEqual(new[] { 10f, 0f, 0f, 0f }, moles);
        }

        [Test]
        public void FillMoles_Air_MatchesStationAir()
        {
            float[] moles = MixMoles(AtmosGasMix.Air(), AtmosConstants.StationOxygenMoles + AtmosConstants.StationNitrogenMoles);
            Assert.That(moles[AtmosConstants.Oxygen.Value], Is.EqualTo(AtmosConstants.StationOxygenMoles).Within(0.01f));
            Assert.That(moles[AtmosConstants.Nitrogen.Value], Is.EqualTo(AtmosConstants.StationNitrogenMoles).Within(0.01f));
        }

        [Test]
        public void FillMoles_ScalesAnUnnormalisedMix_AndVacuumHoldsNothing()
        {
            float[] moles = MixMoles(new[] { 1f, 3f, 0f, 0f }, 8f);
            Assert.That(moles[1], Is.EqualTo(6f).Within(0.001f));
            Assert.AreEqual(new float[4], MixMoles(AtmosGasMix.Vacuum(), 8f));
        }

        [Test]
        public void Matches_ComparesProportions_NotAmounts()
        {
            Assert.IsTrue(AtmosGasMix.Matches(new[] { 2f, 8f }, new[] { 1f, 4f, 0f, 0f }));
            Assert.IsFalse(AtmosGasMix.Matches(AtmosGasMix.Air(), AtmosGasMix.Pure(AtmosConstants.Oxygen)));
            Assert.IsTrue(AtmosGasMix.Matches(AtmosGasMix.Vacuum(), null));
        }

        [Test]
        public void Source_HoldsItsMix_UntilTheMarkerIsCleared()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, 4, 4, out int map);
            var markers = new GasMarkerRegistry();
            using var runtime = new AtmosGasMarkers(markers, context.Map, context.Query, AtmosConstants.DefaultGasCount);
            var held = new TileCoord(map, 1, 1);
            Assert.IsTrue(markers.TryPlace(Marker(GasMarkerKind.Source, held, AtmosGasMix.Pure(AtmosConstants.Plasma), 500f)));
            float expected = AtmosPipeThermo.GetMolesForPressure(500f, AtmosConstants.StandardTemperature, AtmosConstants.CellVolume);

            for (int tick = 0; tick < 5; tick++)
            {
                sim.Tick(AtmosConstants.TickInterval);
                runtime.HoldSources(sim);
            }

            Assert.That(sim.DebugGetMoles(held, AtmosConstants.Plasma), Is.EqualTo(expected).Within(0.01f));
            Assert.That(sim.DebugGetMoles(held, AtmosConstants.Oxygen), Is.EqualTo(0f).Within(0.01f));

            Assert.IsTrue(markers.TryClearAt(new Vector3(1f, 0f, 1f), out _));
            sim.Tick(AtmosConstants.TickInterval);
            runtime.HoldSources(sim);
            Assert.Less(sim.DebugGetMoles(held, AtmosConstants.Plasma), expected, "A cleared marker stops holding its tile");
        }

        [Test]
        public void RoomAir_Vacuum_EmptiesTheRoom_AndStopsAtItsWalls()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, 3, 3, out int map);
            var markers = new GasMarkerRegistry();
            using var runtime = new AtmosGasMarkers(markers, context.Map, context.Query, AtmosConstants.DefaultGasCount);
            Assert.IsTrue(markers.TryPlace(Marker(GasMarkerKind.RoomAir, new TileCoord(map, 2, 2), AtmosGasMix.Vacuum(), 101f)));

            runtime.FillRooms(sim);

            for (int x = 1; x <= 3; x++)
            {
                for (int y = 1; y <= 3; y++)
                    Assert.AreEqual(0f, sim.DebugGetMoles(new TileCoord(map, x, y), AtmosConstants.Oxygen), $"Cell {x},{y}");
            }
        }

        [Test]
        public void RoomFlood_SealedRoom_CollectsItsFloor()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            TileMapTestUtilities.BuildWalledRoom(context, 0, 0, 3, 2);
            var cells = new List<TileCoord>();

            Assert.IsTrue(AtmosRoomFlood.Collect(context.Query, new TileCoord(context.Map.MapId, 1, 1), cells));
            Assert.AreEqual(6, cells.Count);
        }

        [Test]
        public void RoomFlood_FloorMeetingSpace_IsNotSealed()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            TileMapTestUtilities.PlacePlenum(context, new Vector3(1f, 0f, 1f));
            var cells = new List<TileCoord>();

            Assert.IsFalse(AtmosRoomFlood.Collect(context.Query, new TileCoord(context.Map.MapId, 1, 1), cells));
        }

        [Test]
        public void RoomFlood_StopsAtADoor_EvenAnOpenOne()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            TileMapTestUtilities.BuildWalledRoom(context, 0, 0, 2, 1);
            TileObjectSo door = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "TestDoor");
            door.genericType = TileObjectGenericType.Door;
            Assert.IsTrue(context.Map.PlaceTileObject(door, new Vector3(2f, 0f, 1f), Direction.North, skipBuildCheck: true,
                replaceExisting: true, skipAdjacency: false, out _));
            var cells = new List<TileCoord>();

            Assert.IsTrue(AtmosRoomFlood.Collect(context.Query, new TileCoord(context.Map.MapId, 1, 1), cells));
            Assert.AreEqual(1, cells.Count);
        }

        private static GasMarkerRecord Marker(GasMarkerKind kind, TileCoord coord, float[] fractions, float pressure) =>
            new()
            {
                Kind = kind,
                Position = new Vector3(coord.Grid.x, 0f, coord.Grid.y),
                PressureKpa = pressure,
                GasFractions = fractions,
            };

        private static float[] MixMoles(float[] fractions, float total)
        {
            var moles = new float[AtmosConstants.DefaultGasCount];
            AtmosGasMix.FillMoles(fractions, total, moles);
            return moles;
        }

        private static int CellIndex(AtmosSimulation sim, TileCoord coord)
        {
            Assert.IsTrue(sim.TryGetCellIndex(coord, out int index));
            return index;
        }
    }
}
