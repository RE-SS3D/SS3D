using NUnit.Framework;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    public class BlastResolutionTests
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

        [Test]
        public void Cone_BreachesTargetAndDamagesBehindIt_WithoutSideOrRearDamage()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            for (int x = -3; x <= 4; x++)
                for (int z = -3; z <= 3; z++) PlaceFloor(context, x, z);
            foreach (Vector3 position in new[] { Vector3.zero, new Vector3(3, 0, 0), new Vector3(-2, 0, 0), new Vector3(0, 0, 2) })
                TileMapTestUtilities.PlaceAirtightWall(context, position);
            StructuralDamageService structural = new(context.Query, context.Construction);
            BlastResolutionService blast = new(context.Query, structural);
            TileCoord origin = context.Query.WorldToTile(Vector3.zero);
            blast.ResolveCone(origin, 200f, 40f, Vector2.right);
            Assert.False(structural.TryGetIntegrity(origin, out _, out _, out _), "The attached target is breached");
            Assert.True(structural.TryGetIntegrity(context.Query.WorldToTile(new Vector3(3, 0, 0)), out _, out float remaining, out float max));
            Assert.Less(remaining, max, "Cone reaches through the target");
            foreach (Vector3 position in new[] { new Vector3(-2, 0, 0), new Vector3(0, 0, 2) })
            {
                Assert.True(structural.TryGetIntegrity(context.Query.WorldToTile(position), out _, out remaining, out max));
                Assert.AreEqual(max, remaining, "Additional cone force never leaks to the rear or side");
            }
        }

        [Test]
        public void Cone_StopsAtSurvivingBarrier()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            for (int x = 0; x <= 3; x++) PlaceFloor(context, x, 0);
            TileMapTestUtilities.PlaceAirtightWall(context, new Vector3(1, 0, 0));
            TileMapTestUtilities.PlaceAirtightWall(context, new Vector3(2, 0, 0));
            StructuralDamageService structural = new(context.Query, context.Construction);
            BlastResolutionService blast = new(context.Query, structural);
            blast.ResolveCone(context.Query.WorldToTile(Vector3.zero), 100f, 40f, Vector2.right);
            Assert.True(structural.TryGetIntegrity(context.Query.WorldToTile(new Vector3(2, 0, 0)), out _, out float remaining, out float max));
            Assert.AreEqual(max, remaining, "Directional force still obeys structural occlusion");
        }

        [Test]
        public void Corridor_OpenPath_ReachesPastBend_AndHitsSideWall()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);

            // Open corridor along x; side walls at z=1 for x=1..3.
            for (int x = 0; x <= 5; x++)
            {
                PlaceFloor(context, x, 0);
            }

            for (int x = 1; x <= 3; x++)
            {
                PlaceFloor(context, x, 1);
                TileMapTestUtilities.PlaceAirtightWall(context, new Vector3(x, 0, 1));
            }

            var structural = new StructuralDamageService(context.Query, context.Construction);
            var blast = new BlastResolutionService(context.Query, structural);

            TileCoord epicenter = context.Query.WorldToTile(new Vector3(0, 0, 0));
            blast.Resolve(epicenter, yield: 100f, falloff: 20f);

            // Force at x=3 along corridor: 100 - 3*20 = 40 — far enough to matter.
            TileCoord sideWall = context.Query.WorldToTile(new Vector3(2, 0, 1));
            Assert.IsTrue(structural.TryGetIntegrity(sideWall, out StructuralIntegrityStage stage, out float remaining, out float max));
            Assert.Less(remaining, max, "Side wall along the corridor should take blast force.");
            Assert.AreNotEqual(StructuralIntegrityStage.Destroyed, stage);

            // Far end floor cell is open; a wall placed beyond the corridor end should not be required.
            TileCoord farFloor = context.Query.WorldToTile(new Vector3(4, 0, 0));
            Assert.IsTrue(context.Query.TryGetOccupancy(farFloor, out _), "Corridor path should remain queryable past the bend.");
        }

        [Test]
        public void ClosedDoor_BlocksUntilDestroyed_ThenCascadeContinues()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);

            PlaceFloor(context, 0, 0);
            PlaceFloor(context, 1, 0);
            PlaceFloor(context, 2, 0);
            PlaceFloor(context, 3, 0);
            PlaceDoor(context, new Vector3(1, 0, 0));
            TileMapTestUtilities.PlaceAirtightWall(context, new Vector3(3, 0, 0));

            var structural = new StructuralDamageService(context.Query, context.Construction);
            var blast = new BlastResolutionService(context.Query, structural);

            TileCoord door = context.Query.WorldToTile(new Vector3(1, 0, 0));
            TileCoord beyondWall = context.Query.WorldToTile(new Vector3(3, 0, 0));

            // Not enough to Destroy door (max 80): blocked hop spends 30.
            blast.Resolve(context.Query.WorldToTile(new Vector3(0, 0, 0)), yield: 40f, falloff: 10f);
            Assert.IsTrue(structural.TryGetIntegrity(door, out _, out float doorRemaining, out float doorMax));
            Assert.AreEqual(doorMax - 30f, doorRemaining, 0.01f);
            Assert.IsTrue(structural.TryGetIntegrity(beyondWall, out _, out float beyondRemaining, out float beyondMax));
            Assert.AreEqual(beyondMax, beyondRemaining, 0.01f, "Interior beyond a surviving door must not take blast force.");

            // Fresh map for cascade case.
            TearDown();
            SetUp();
            context = TileMapTestUtilities.CreateContext(_instantiated);
            PlaceFloor(context, 0, 0);
            PlaceFloor(context, 1, 0);
            PlaceFloor(context, 2, 0);
            PlaceFloor(context, 3, 0);
            PlaceDoor(context, new Vector3(1, 0, 0));
            TileMapTestUtilities.PlaceAirtightWall(context, new Vector3(3, 0, 0));

            structural = new StructuralDamageService(context.Query, context.Construction);
            blast = new BlastResolutionService(context.Query, structural);
            door = context.Query.WorldToTile(new Vector3(1, 0, 0));
            beyondWall = context.Query.WorldToTile(new Vector3(3, 0, 0));

            blast.Resolve(context.Query.WorldToTile(new Vector3(0, 0, 0)), yield: 120f, falloff: 10f);

            Assert.IsFalse(structural.TryGetIntegrity(door, out _, out _, out _), "Door should be Destroyed and cleared.");
            Assert.IsTrue(structural.TryGetIntegrity(beyondWall, out _, out float cascadedRemaining, out float cascadedMax));
            Assert.Less(cascadedRemaining, cascadedMax, "Cascade through Destroyed door should reach the wall beyond.");
        }

        [Test]
        public void ThinWall_CrackedNotDestroyed_BlocksFurtherTraversal()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);

            PlaceFloor(context, 0, 0);
            PlaceFloor(context, 1, 0);
            PlaceFloor(context, 2, 0);
            TileMapTestUtilities.PlaceAirtightWall(context, new Vector3(1, 0, 0));
            TileMapTestUtilities.PlaceAirtightWall(context, new Vector3(2, 0, 0));

            var structural = new StructuralDamageService(context.Query, context.Construction);
            var blast = new BlastResolutionService(context.Query, structural);

            // nextForce 65 → wall remaining 35 = Cracked (max 100); not Destroyed.
            blast.Resolve(context.Query.WorldToTile(new Vector3(0, 0, 0)), yield: 80f, falloff: 15f);

            TileCoord firstWall = context.Query.WorldToTile(new Vector3(1, 0, 0));
            TileCoord secondWall = context.Query.WorldToTile(new Vector3(2, 0, 0));

            Assert.IsTrue(structural.TryGetIntegrity(firstWall, out StructuralIntegrityStage stage, out float remaining, out _));
            Assert.AreEqual(StructuralIntegrityStage.Cracked, stage);
            Assert.AreEqual(35f, remaining, 0.01f);

            Assert.IsTrue(structural.TryGetIntegrity(secondWall, out _, out float beyondRemaining, out float beyondMax));
            Assert.AreEqual(beyondMax, beyondRemaining, 0.01f, "Cracked wall must still block blast hops.");
        }

        private static void PlaceFloor(TileMapTestUtilities.MapContext context, int x, int z)
        {
            Vector3 position = new Vector3(x, 0, z);
            TileMapTestUtilities.PlacePlenum(context, position);
        }

        private static void PlaceDoor(TileMapTestUtilities.MapContext context, Vector3 position)
        {
            TileObjectSo doorSo = TileMapTestUtilities.CreateTileSo(TileLayer.Turf, "TestDoor");
            doorSo.genericType = TileObjectGenericType.Door;
            bool placed = context.Map.PlaceTileObject(
                doorSo,
                position,
                Direction.North,
                skipBuildCheck: true,
                replaceExisting: true,
                skipAdjacency: true,
                out _);
            Assert.IsTrue(placed, $"Expected door placement at {position}.");
        }
    }
}
