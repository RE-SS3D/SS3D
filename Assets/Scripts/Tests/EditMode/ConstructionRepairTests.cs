using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Construction;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    /// <summary>
    /// S3 of 2026-11_construction-mvp1 (structural plan Phase 6): a welder returns a Damaged or Cracked wall,
    /// window or reinforced wall to Intact, and a repaired Cracked wall is airtight again.
    /// </summary>
    public class ConstructionRepairTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void Cleanup()
        {
            foreach (GameObject go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        private static PlacedTileObject Wall(TileMapTestUtilities.MapContext context, Vector3 position, bool window = false)
        {
            TileMapTestUtilities.PlacePlenum(context, position);
            if (window) TileMapTestUtilities.PlaceWindow(context, position);
            else TileMapTestUtilities.PlaceAirtightWall(context, position);
            Assert.True(context.Query.TryGetOccupant(context.Query.WorldToTile(position), TileLayer.Turf, Direction.North, out ITileOccupant occupant));
            return (PlacedTileObject)occupant;
        }

        private static ConstructionRule Repair => ConstructionRecipes.Get(ConstructionStep.RepairStructure);

        [TestCase(false)]
        [TestCase(true)]
        public void Repair_MatchesOnlyDamagedOrCrackedStructures(bool window)
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            PlacedTileObject wall = Wall(context, new Vector3(2, 0, 2), window);

            wall.ServerSetIntegrity(100f, StructuralIntegrityStage.Intact);
            Assert.False(ConstructionObjectRules.Matches(Repair, wall), "Nothing to repair");
            wall.ServerSetIntegrity(70f, StructuralIntegrityStage.Damaged);
            Assert.True(ConstructionObjectRules.Matches(Repair, wall));
            wall.ServerSetIntegrity(30f, StructuralIntegrityStage.Cracked);
            Assert.True(ConstructionObjectRules.Matches(Repair, wall));
            Assert.AreEqual(ConstructionInput.Welder, Repair.Input);
        }

        [Test]
        public void Repair_ReachesReinforcedWalls()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            PlacedTileObject wall = Wall(context, new Vector3(2, 0, 2));
            wall.ServerSetIntegrity(30f, StructuralIntegrityStage.Cracked);
            Assert.False(Repair.RefusesReinforced, "D10: reinforced walls can be repaired");
            Assert.True(ConstructionObjectRules.Matches(Repair, wall));
        }

        [Test]
        public void RepairingACrackedWall_SealsItAndRestoresFullIntegrity()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            Vector3 position = new(2, 0, 2);
            PlacedTileObject wall = Wall(context, position);
            TileCoord coord = context.Query.WorldToTile(position);
            wall.ServerSetIntegrity(30f, StructuralIntegrityStage.Cracked);
            Assert.True(context.Query.TryGetOccupancy(coord, out TileOccupancy cracked));
            Assert.False(cracked.IsAirtight, "Precondition: a Cracked wall leaks");

            List<Vector3> notified = new();
            ConstructionApply.Repair(wall, notified.Add);

            Assert.AreEqual(StructuralIntegrityStage.Intact, wall.IntegrityStage);
            Assert.AreEqual(StructuralIntegrityRules.ResolveMaxIntegrity(wall.tileObjectSO), wall.IntegrityRemaining, 0.01f);
            Assert.AreEqual(1, notified.Count, "Atmos is told the cell changed");
            Assert.True(context.Query.TryGetOccupancy(coord, out TileOccupancy repaired));
            Assert.True(repaired.IsAirtight);
            Assert.True(repaired.HasWall);
        }

        [Test]
        public void RepairingADamagedWall_DoesNotNotifyAtmos()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            PlacedTileObject wall = Wall(context, new Vector3(2, 0, 2));
            wall.ServerSetIntegrity(70f, StructuralIntegrityStage.Damaged);
            List<Vector3> notified = new();
            ConstructionApply.Repair(wall, notified.Add);
            Assert.AreEqual(StructuralIntegrityStage.Intact, wall.IntegrityStage);
            Assert.IsEmpty(notified, "A Damaged wall never leaked");
        }

        [Test]
        public void DamagedWall_ExamineSuggestsTheWelder()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_objects);
            PlacedTileObject wall = Wall(context, new Vector3(2, 0, 2));
            wall.ServerSetIntegrity(70f, StructuralIntegrityStage.Damaged);
            Assert.True(ConstructionExamineText.TryGetNextStep(wall, out string key, out _));
            Assert.AreEqual(SS3D.Systems.Examine.ExamineConstructionKeys.RepairWelder, key);
        }
    }
}
