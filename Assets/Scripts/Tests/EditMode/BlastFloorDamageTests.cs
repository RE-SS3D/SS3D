using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    public class BlastFloorDamageTests
    {
        private const float C4Yield = 180f;
        private const float C4Falloff = 45f;

        private readonly List<GameObject> _objects = new();
        [TearDown]
        public void Cleanup()
        {
            foreach (GameObject go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }
        private static PlacedTileObject Floor(TileMapTestUtilities.MapContext context, Vector3 position)
        {
            TileMapTestUtilities.PlacePlenum(context, position);
            TileObjectSo so = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "TestFloor");
            Assert.True(context.Map.PlaceTileObject(so, position, Direction.North, true, false, false, out GameObject floor));
            return floor.GetComponent<PlacedTileObject>();
        }

        [Test]
        public void WeakBlast_DamagesBothLayers_WithoutVenting()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            PlacedTileObject floor = Floor(context, Vector3.zero);
            StructuralDamageService service = new(context.Query, context.Construction);
            service.ApplyBlastFloorDamage(context.Query.WorldToTile(Vector3.zero), 30f);
            Assert.That(floor.IntegrityRemaining, Is.EqualTo(30f));
            Assert.That(floor.IntegrityStage, Is.EqualTo(StructuralIntegrityStage.Damaged));
            Assert.True(context.Query.TryGetOccupant(context.Query.WorldToTile(Vector3.zero), TileLayer.Plenum, Direction.North, out ITileOccupant plenum));
            Assert.That(((PlacedTileObject)plenum).IntegrityRemaining, Is.EqualTo(170f));
            Assert.True(context.Query.TryGetOccupancy(context.Query.WorldToTile(Vector3.zero), out TileOccupancy occupancy));
            Assert.True(occupancy.HasPlenum);
        }

        [Test]
        public void MediumBlast_StripsFloorButPreservesHullAndWiring()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            Floor(context, Vector3.zero);
            TileObjectSo cable = TileMapTestUtilities.CreateTileSo(TileLayer.Wire, "TestCable");
            context.Map.PlaceTileObject(cable, Vector3.zero, Direction.North, true, false, false, out _);
            TileCoord tile = context.Query.WorldToTile(Vector3.zero);
            new StructuralDamageService(context.Query, context.Construction).ApplyBlastFloorDamage(tile, 90f);
            Assert.False(context.Query.TryGetOccupant(tile, TileLayer.Turf, Direction.North, out _));
            Assert.True(context.Query.TryGetOccupant(tile, TileLayer.Plenum, Direction.North, out _));
            Assert.True(context.Query.TryGetOccupant(tile, TileLayer.Wire, Direction.North, out _));
        }

        [Test]
        public void StrongBlast_BreachesHull_AndNotifiesInfrastructureRemoval()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            Floor(context, Vector3.zero);
            foreach (TileLayer layer in new[] { TileLayer.Wire, TileLayer.PipeMiddle, TileLayer.Disposal })
                context.Map.PlaceTileObject(TileMapTestUtilities.CreateTileSo(layer), Vector3.zero, Direction.North, true, false, false, out _);
            TileMapTestUtilities.RecordingMutationObserver observer = new();
            context.Map.RegisterMutationObserver(observer);
            TileCoord tile = context.Query.WorldToTile(Vector3.zero);
            new StructuralDamageService(context.Query, context.Construction).ApplyBlastFloorDamage(tile, 200f);
            Assert.True(context.Query.TryGetOccupancy(tile, out TileOccupancy occupancy));
            Assert.False(occupancy.HasPlenum);
            foreach (TileLayer layer in new[] { TileLayer.Turf, TileLayer.Plenum, TileLayer.Wire, TileLayer.PipeMiddle, TileLayer.Disposal })
                Assert.False(context.Query.TryGetOccupant(tile, layer, Direction.North, out _), layer.ToString());
            Assert.That(observer.ClearedCount, Is.EqualTo(5), "Every directly or indirectly removed layer notifies observers");
        }

        [Test]
        public void FloorRemoval_IsDeferredUntilWaveHasSpread()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            for (int x = 0; x <= 4; x++) Floor(context, new Vector3(x, 0, 0));
            StructuralDamageService service = new(context.Query, context.Construction);
            BlastResolutionService blast = new(context.Query, service, applyFloorDamage: service.ApplyBlastFloorDamage);
            blast.Resolve(context.Query.WorldToTile(Vector3.zero), 260f, 50f);
            Assert.False(context.Query.TryGetOccupant(context.Query.WorldToTile(new Vector3(1,0,0)), TileLayer.Plenum, Direction.North, out _));
            Assert.False(context.Query.TryGetOccupant(context.Query.WorldToTile(new Vector3(4,0,0)), TileLayer.Turf, Direction.North, out _), "The wave reaches the outer floor despite opening holes behind it");
            Assert.True(context.Query.TryGetOccupant(context.Query.WorldToTile(new Vector3(4,0,0)), TileLayer.Plenum, Direction.North, out _));
        }

        [Test]
        public void ShapedBlast_FloorHoleExtendsFurtherThroughTheTarget()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            for (int x = -3; x <= 3; x++) Floor(context, new Vector3(x, 0, 0));
            StructuralDamageService service = new(context.Query, context.Construction);
            BlastResolutionService blast = new(context.Query, service, applyFloorDamage: service.ApplyBlastFloorDamage);
            blast.ResolveShaped(context.Query.WorldToTile(Vector3.zero), 240f, 60f, 180f, 40f, Vector2.right);
            Assert.False(context.Query.TryGetOccupant(context.Query.WorldToTile(new Vector3(2,0,0)), TileLayer.Plenum, Direction.North, out _), "Extra cone reaches beyond the first holes");
            Assert.True(context.Query.TryGetOccupant(context.Query.WorldToTile(new Vector3(-2,0,0)), TileLayer.Plenum, Direction.North, out _), "Outside the cone the same distance retains plating");
            Assert.True(context.Query.TryGetOccupant(context.Query.WorldToTile(new Vector3(-1,0,0)), TileLayer.Plenum, Direction.North, out _), "The room side of the charge keeps its hull");
        }

        [Test]
        public void BareDamagedPlating_HoldsAir_UntilItsHullBreaks()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation simulation = EditorTests.Atmospherics.AtmosTestFixtures.CreateSealedRoomSimulation(context, 4, out int map);
            context.Map.RegisterMutationObserver(new AtmosTileObserver(simulation));
            Vector3 position = new(2,0,2);
            context.Map.PlaceTileObject(TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "TestFloor"), position, Direction.North, true, false, false, out _);
            StructuralDamageService service = new(context.Query, context.Construction);
            TileCoord tile = new(map, 2, 2);
            float before = simulation.GetTotalMoles();
            service.ApplyBlastFloorDamage(tile, 90f);
            for (int tick = 0; tick < 20; tick++) simulation.Tick(AtmosConstants.TickInterval);
            Assert.That(simulation.GetTotalMoles(), Is.EqualTo(before).Within(0.1f), "Removing the surface and damaging plating does not vent the room");
            service.ApplyBlastFloorDamage(tile, 110f);
            for (int tick = 0; tick < 200; tick++) simulation.Tick(AtmosConstants.TickInterval);
            Assert.That(simulation.GetTotalMoles(), Is.LessThan(before * 0.8f), "Breaking plating opens a real vacuum sink through mutation observers");
        }

        [Test]
        public void SingleC4_StripsFloorsButLeavesFreshHullSealed()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            for (int x = 0; x <= 3; x++) Floor(context, new Vector3(x, 0, 0));
            StructuralDamageService service = new(context.Query, context.Construction);
            BlastResolutionService blast = new(context.Query, service, applyFloorDamage: service.ApplyBlastFloorDamage);
            TileCoord epicenter = context.Query.WorldToTile(Vector3.zero);
            blast.Resolve(epicenter, C4Yield, C4Falloff);
            Assert.True(context.Query.TryGetOccupant(epicenter, TileLayer.Plenum, Direction.North, out ITileOccupant plenum));
            Assert.That(((PlacedTileObject)plenum).IntegrityStage, Is.EqualTo(StructuralIntegrityStage.Cracked));
            Assert.False(context.Query.TryGetOccupant(epicenter, TileLayer.Turf, Direction.North, out _));
            Assert.False(context.Query.TryGetOccupant(context.Query.WorldToTile(new Vector3(2,0,0)), TileLayer.Turf, Direction.North, out _), "Floor tiles come loose over a wider area");
            Assert.True(context.Query.TryGetOccupant(context.Query.WorldToTile(new Vector3(3,0,0)), TileLayer.Turf, Direction.North, out _));
        }

        [Test]
        public void SecondC4_BreachesAlreadyCrackedHull()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            Floor(context, Vector3.zero);
            StructuralDamageService service = new(context.Query, context.Construction);
            BlastResolutionService blast = new(context.Query, service, applyFloorDamage: service.ApplyBlastFloorDamage);
            TileCoord tile = context.Query.WorldToTile(Vector3.zero);
            blast.Resolve(tile, C4Yield, C4Falloff);
            blast.Resolve(tile, C4Yield, C4Falloff);
            Assert.False(context.Query.TryGetOccupant(tile, TileLayer.Plenum, Direction.North, out _));
        }
    }
}
