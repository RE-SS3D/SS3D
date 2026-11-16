using NUnit.Framework;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    public class StructuralDamageTests
    {
        private List<GameObject> _instantiated;

        [SetUp]
        public void SetUp() => _instantiated = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _instantiated)
                Object.DestroyImmediate(go);
            _instantiated.Clear();
        }

        [TestCase(TileObjectSpecificType.Steel)]
        [TestCase(TileObjectSpecificType.Wood)]
        public void Table_DamageAndDestructionPreserveFloorAndPlenum(TileObjectSpecificType material)
        {
            var context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 position = new Vector3(8, 0, 8);
            TileMapTestUtilities.PlacePlenum(context, position);
            var floor = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "TestFloor");
            Assert.IsTrue(context.Map.PlaceTileObject(floor, position, Direction.North, true, false, false, out _));
            var table = TileMapTestUtilities.CreateTileSo(TileLayer.FurnitureBase, "TableSteel");
            table.genericType = TileObjectGenericType.Table;
            table.specificType = material;
            Assert.IsTrue(context.Map.PlaceTileObject(table, position, Direction.North, true, false, false, out _));
            TileCoord coord = context.Query.WorldToTile(position);
            var service = new StructuralDamageService(context.Query, context.Construction);

            Assert.IsTrue(service.TryApplyStructuralDamage(coord, 30, StructuralDamageSource.Melee));
            Assert.IsTrue(service.TryGetIntegrity(coord, out var stage, out _, out _));
            Assert.AreEqual(StructuralIntegrityStage.Damaged, stage);
            Assert.IsTrue(service.TryApplyStructuralDamage(coord, 35, StructuralDamageSource.Ranged));
            Assert.IsTrue(service.TryGetIntegrity(coord, out stage, out _, out _));
            Assert.AreEqual(StructuralIntegrityStage.Cracked, stage);
            Assert.IsTrue(service.TryApplyStructuralDamage(coord, 35, StructuralDamageSource.Blast));
            Assert.IsFalse(context.Query.TryGetOccupant(coord, TileLayer.FurnitureBase, Direction.North, out _));
            Assert.IsTrue(context.Query.TryGetOccupant(coord, TileLayer.Turf, Direction.North, out _));
            Assert.IsTrue(context.Query.TryGetOccupant(coord, TileLayer.Plenum, Direction.North, out _));
        }

        [TestCase(TileObjectSpecificType.Wood, true)]
        [TestCase(TileObjectSpecificType.Glass, true)]
        [TestCase(TileObjectSpecificType.Poker, false)]
        [TestCase(TileObjectSpecificType.Steel, true)]
        public void TableDamage_OnlySupportsSelectedMaterials(TileObjectSpecificType material, bool supported)
        {
            var so = ScriptableObject.CreateInstance<TileObjectSo>();
            try
            {
                so.layer = TileLayer.FurnitureBase;
                so.genericType = TileObjectGenericType.Table;
                so.specificType = material;
                Assert.AreEqual(supported, StructuralIntegrityRules.IsStructural(so));
            }
            finally { Object.DestroyImmediate(so); }
        }

        [Test]
        public void StageFromRemaining_UsesProvisionalFractions()
        {
            const float max = 100f;
            Assert.AreEqual(StructuralIntegrityStage.Intact, StructuralIntegrityRules.StageFromRemaining(100f, max));
            Assert.AreEqual(StructuralIntegrityStage.Intact, StructuralIntegrityRules.StageFromRemaining(71f, max));
            Assert.AreEqual(StructuralIntegrityStage.Damaged, StructuralIntegrityRules.StageFromRemaining(70f, max));
            Assert.AreEqual(StructuralIntegrityStage.Damaged, StructuralIntegrityRules.StageFromRemaining(36f, max));
            Assert.AreEqual(StructuralIntegrityStage.Cracked, StructuralIntegrityRules.StageFromRemaining(35f, max));
            Assert.AreEqual(StructuralIntegrityStage.Destroyed, StructuralIntegrityRules.StageFromRemaining(0f, max));
        }

        [Test]
        public void ApplyDamage_TransitionsToDamagedThenCracked()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 wallPos = new Vector3(1, 0, 1);
            TileMapTestUtilities.PlacePlenum(context, wallPos);
            TileMapTestUtilities.PlaceAirtightWall(context, wallPos);

            var service = new StructuralDamageService(context.Query, context.Construction);
            TileCoord coord = context.Query.WorldToTile(wallPos);

            Assert.IsTrue(service.TryApplyStructuralDamage(coord, 30f, StructuralDamageSource.Console));
            Assert.IsTrue(service.TryGetIntegrity(coord, out StructuralIntegrityStage stage, out float remaining, out float max));
            Assert.AreEqual(StructuralIntegrityStage.Damaged, stage);
            Assert.AreEqual(70f, remaining, 0.01f);
            Assert.AreEqual(100f, max, 0.01f);

            Assert.IsTrue(service.TryApplyStructuralDamage(coord, 35f, StructuralDamageSource.Console));
            Assert.IsTrue(service.TryGetIntegrity(coord, out stage, out remaining, out _));
            Assert.AreEqual(StructuralIntegrityStage.Cracked, stage);
            Assert.AreEqual(35f, remaining, 0.01f);
        }

        [Test]
        public void CrackedWall_IsNotAirtight_ButStillHasWall()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 wallPos = new Vector3(2, 0, 2);
            TileMapTestUtilities.PlacePlenum(context, wallPos);
            TileMapTestUtilities.PlaceAirtightWall(context, wallPos);

            TileCoord coord = context.Query.WorldToTile(wallPos);
            Assert.IsTrue(context.Query.TryGetOccupant(coord, TileLayer.Turf, Direction.North, out ITileOccupant occupant));
            var placed = (PlacedTileObject)occupant;
            placed.ServerSetIntegrity(35f, StructuralIntegrityStage.Cracked);

            Assert.IsTrue(context.Query.TryGetOccupancy(coord, out TileOccupancy occupancy));
            Assert.IsTrue(occupancy.HasWall);
            Assert.IsFalse(occupancy.IsAirtight);
        }

        [Test]
        public void Destroyed_ClearsTurfWall()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 wallPos = new Vector3(3, 0, 3);
            TileMapTestUtilities.PlacePlenum(context, wallPos);
            TileMapTestUtilities.PlaceAirtightWall(context, wallPos);

            var service = new StructuralDamageService(context.Query, context.Construction);
            TileCoord coord = context.Query.WorldToTile(wallPos);

            Assert.IsTrue(service.TryApplyStructuralDamage(coord, 100f, StructuralDamageSource.Console));
            Assert.IsFalse(service.TryGetIntegrity(coord, out _, out _, out _));
            Assert.IsFalse(context.Query.TryGetOccupant(coord, TileLayer.Turf, Direction.North, out _));
            Assert.IsTrue(context.Query.TryGetOccupancy(coord, out TileOccupancy occupancy));
            Assert.IsFalse(occupancy.HasWall);
        }

        [Test]
        public void NonStructuralTurf_ReturnsFalse()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 floorPos = new Vector3(4, 0, 4);
            TileMapTestUtilities.PlacePlenum(context, floorPos);

            TileObjectSo floorSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "TestFloor");
            floorSo.genericType = TileObjectGenericType.Floor;
            Assert.IsTrue(context.Map.PlaceTileObject(
                floorSo, floorPos, Direction.North, skipBuildCheck: true, replaceExisting: false, skipAdjacency: false, out _));

            var service = new StructuralDamageService(context.Query, context.Construction);
            TileCoord coord = context.Query.WorldToTile(floorPos);
            Assert.IsFalse(service.TryApplyStructuralDamage(coord, 50f, StructuralDamageSource.Console));
        }

        [Test]
        public void Window_UsesLowerDefaultMaxIntegrity()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            Vector3 pos = new Vector3(5, 0, 5);
            TileMapTestUtilities.PlacePlenum(context, pos);
            TileMapTestUtilities.PlaceWindow(context, pos);

            var service = new StructuralDamageService(context.Query, context.Construction);
            TileCoord coord = context.Query.WorldToTile(pos);
            Assert.IsTrue(service.TryGetIntegrity(coord, out _, out float remaining, out float max));
            Assert.AreEqual(StructuralIntegrityRules.DefaultWindowMaxIntegrity, max, 0.01f);
            Assert.AreEqual(max, remaining, 0.01f);
        }
    }
}
