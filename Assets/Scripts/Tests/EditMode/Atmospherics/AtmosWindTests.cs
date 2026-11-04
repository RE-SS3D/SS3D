using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Visualization;
using SS3D.Systems.Tile;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    public class AtmosWindTests
    {
        private readonly List<GameObject> _objects = new();
        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        [Test]
        public void WallBlocksWind_DestructionOpensFlow_RepairStopsIt()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using var sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, 3, out int map);
            var inside = new TileCoord(map, 3, 2);
            Assert.That(AtmosWind.Sample(sim, inside), Is.EqualTo(Vector3.zero));
            Vector3 breach = new(4, 0, 2);
            context.Construction.TryClearTile(breach, TileLayer.Turf, Direction.North);
            sim.QueueCellRefresh(new TileCoord(map, 4, 2));
            for (int tick = 0; tick < 3; tick++) sim.Tick(AtmosConstants.TickInterval);
            Vector3 wind = AtmosWind.Sample(sim, inside);
            Assert.That(wind.x, Is.GreaterThan(AtmosWind.MinPressure));
            Assert.That(Mathf.Abs(wind.z), Is.LessThan(wind.x));
            TileMapTestUtilities.PlaceAirtightWall(context, breach);
            sim.UpdateCell(new TileCoord(map, 4, 2));
            Assert.That(AtmosWind.Sample(sim, inside).x, Is.LessThanOrEqualTo(0f));
        }

        [Test]
        public void EvacuatedCellStopsPulling_UnknownCellHasNoWind()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            TileMapTestUtilities.PlacePlenum(context, new Vector3(2, 0, 2));
            using var sim = new AtmosSimulation(context.Query, context.Map.MapId, AtmosConstants.DefaultGasCount);
            sim.CreateChunk(new TileChunkRef { MapId = context.Map.MapId, ChunkKey = Vector2Int.zero, Origin = Vector3.zero });
            var cell = new TileCoord(context.Map.MapId, 2, 2);
            // Close three sides so pressure has one directed outlet.
            TileMapTestUtilities.PlaceAirtightWall(context, new Vector3(1, 0, 2));
            TileMapTestUtilities.PlaceAirtightWall(context, new Vector3(2, 0, 1));
            TileMapTestUtilities.PlaceAirtightWall(context, new Vector3(2, 0, 3));
            sim.UpdateCell(new TileCoord(context.Map.MapId, 1, 2));
            sim.UpdateCell(new TileCoord(context.Map.MapId, 2, 1));
            sim.UpdateCell(new TileCoord(context.Map.MapId, 2, 3));
            Assert.That(AtmosWind.Sample(sim, cell).x, Is.GreaterThan(AtmosWind.KnockdownPressure));
            Assert.That(AtmosWind.Sample(sim, new TileCoord(context.Map.MapId, 900, 900)), Is.EqualTo(Vector3.zero));
            for (int tick = 0; tick < 200; tick++) sim.Tick(AtmosConstants.TickInterval);
            Assert.That(AtmosWind.Sample(sim, cell).magnitude, Is.LessThan(AtmosWind.MinPressure));
        }


        [Test]
        public void WindVelocity_IsBoundedAndScalesWithPressure()
        {
            Assert.That(AtmosWind.Velocity(Vector3.right * 10000f).magnitude,
                Is.EqualTo(AtmosWind.MaxSpeed).Within(0.001f));
            Assert.That(AtmosWind.Velocity(Vector3.zero), Is.EqualTo(Vector3.zero));
            Assert.That(AtmosWind.Velocity(Vector3.right * 2f).x, Is.LessThan(AtmosWind.Velocity(Vector3.right * 4f).x));
        }

        [Test]
        public void SampleAt_MatchesTileCentre_AndBlendsAcrossTheEdge()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using var sim = OpenBreach(context, out int map);
            Vector3 near = AtmosWind.Sample(sim, new TileCoord(map, 3, 2));
            Vector3 far = AtmosWind.Sample(sim, new TileCoord(map, 2, 2));
            Assume.That(near.magnitude, Is.Not.EqualTo(far.magnitude).Within(0.01f));

            AssertClose(AtmosWind.SampleAt(sim, new Vector3(3, 0, 2)), near);
            AssertClose(AtmosWind.SampleAt(sim, new Vector3(2.5f, 0, 2)), (near + far) * 0.5f);
            AssertClose(AtmosWind.SampleAt(sim, new Vector3(2.49f, 0, 2)), AtmosWind.SampleAt(sim, new Vector3(2.51f, 0, 2)),
                (near - far).magnitude * 0.05f);
        }

        [Test]
        public void SampleAt_DoesNotBlendThroughAWall()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using var sim = OpenBreach(context, out int map);
            // z = 3 is the top row and z = 4 the wall, so leaning into the wall must not dilute the wind.
            Vector3 centre = AtmosWind.SampleAt(sim, new Vector3(3, 0, 3));
            Assume.That(centre.magnitude, Is.GreaterThan(AtmosWind.MinPressure));
            AssertClose(AtmosWind.SampleAt(sim, new Vector3(3, 0, 3.45f)), centre);
            Assert.That(AtmosWind.SampleAt(sim, new Vector3(3, 0, 4)), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void Follow_IsFrameRateIndependent()
        {
            Vector3 once = AtmosWind.Follow(Vector3.zero, Vector3.right, 0.1f);
            Vector3 twice = AtmosWind.Follow(AtmosWind.Follow(Vector3.zero, Vector3.right, 0.05f), Vector3.right, 0.05f);
            AssertClose(once, twice);
        }

        [Test]
        public void Drag_PushesSlowBodies_NeverBrakesFastOnes()
        {
            Vector3 wind = Vector3.right * 4f;
            Assert.That(AtmosWind.DragAcceleration(wind, Vector3.zero).x, Is.GreaterThan(0f));
            Assert.That(AtmosWind.DragAcceleration(wind, Vector3.right * 9f), Is.EqualTo(Vector3.zero));
            Assert.That(AtmosWind.DragAcceleration(Vector3.right * 1000f, Vector3.zero).magnitude,
                Is.EqualTo(AtmosWind.MaxAcceleration).Within(0.001f));
            Assert.That(AtmosWind.DragAcceleration(Vector3.zero, Vector3.right), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void FlowEncoding_PointsDownhill_AndKeepsStrength()
        {
            Vector2 gentle = AtmosCellVisualEncoding.EncodeFlow(99f, 101f, 100f, 100f);
            Vector2 violent = AtmosCellVisualEncoding.EncodeFlow(0f, 100f, 50f, 50f);
            Assert.That(gentle.x, Is.GreaterThan(0f), "wind blows from high pressure to low");
            Assert.That(gentle.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(gentle.magnitude, Is.LessThan(violent.magnitude));
            Assert.That(violent.magnitude, Is.EqualTo(1f).Within(0.001f));
            Assert.That(AtmosCellVisualEncoding.EncodeFlow(100f, 100f, 100f, 100f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SampleFlow_InterpolatesBetweenTileCentres_AndIsStillOutsideTheAtlas()
        {
            float[] packed = { 0.5f, 0.5f, 1f, 0.5f, 0.5f, 0.5f, 1f, 0.5f };
            var bounds = new Vector4(10f, 20f, 2f, 2f);
            Assert.That(AtmosCellVisualEncoding.SampleFlow(packed, bounds, 10.5f, 20f).x, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(AtmosCellVisualEncoding.SampleFlow(packed, bounds, 11f, 21f).x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(AtmosCellVisualEncoding.SampleFlow(packed, bounds, 9f, 20f), Is.EqualTo(Vector2.zero));
            Assert.That(AtmosCellVisualEncoding.SampleFlow(null, bounds, 10f, 20f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void HostAtlasFlow_PointsAtTheBreach_InBothRefreshModes()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using var sim = OpenBreach(context, out int _);
            using var uploader = new AtmosGpuUploader();

            uploader.Refresh(sim);
            AssertFlowsEast(uploader);

            // The AOI path used to read neighbours before they were filled.
            uploader.Refresh(sim, 0, 0, 15, 15);
            AssertFlowsEast(uploader);
        }

        [Test]
        public void ClientPatchFlow_MatchesHostDirection_NearAWall()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using var sim = OpenBreach(context, out int _);
            var builder = new AtmosChunkPatchBuilder();
            AtmosChunkPatch patch = builder.Build(sim, 0, Vector2Int.zero);
            int local = 2 * AtmosConstants.ChunkSize + 3;
            Assert.That(patch.Flow[local * 2], Is.GreaterThan(0.5f), "east toward the breach");
        }

        private static void AssertFlowsEast(AtmosGpuUploader uploader)
        {
            var snapshot = uploader.BuildSnapshot(default);
            Vector2 flow = AtmosCellVisualEncoding.SampleFlow(snapshot.FlowData, snapshot.AtlasBounds, 3f, 2f);
            Assert.That(flow.x, Is.GreaterThan(0.1f));
            Assert.That(Mathf.Abs(flow.y), Is.LessThan(flow.x));
        }

        private static AtmosSimulation OpenBreach(TileMapTestUtilities.MapContext context, out int map)
        {
            AtmosSimulation sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, 3, out map);
            context.Construction.TryClearTile(new Vector3(4, 0, 2), TileLayer.Turf, Direction.North);
            sim.QueueCellRefresh(new TileCoord(map, 4, 2));
            for (int tick = 0; tick < 3; tick++) sim.Tick(AtmosConstants.TickInterval);
            return sim;
        }

        private static void AssertClose(Vector3 actual, Vector3 expected, float tolerance = 0.001f) =>
            Assert.That(Vector3.Distance(actual, expected), Is.LessThan(tolerance), $"{actual} vs {expected}");
    }
}
