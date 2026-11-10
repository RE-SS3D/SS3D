using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests
{
    public class BlastPushTests
    {
        private readonly List<GameObject> _instantiated = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _instantiated) Object.DestroyImmediate(go);
            _instantiated.Clear();
        }

        [Test]
        public void Velocity_PointsAwayFromEpicenter_AndStaysPlanar()
        {
            Vector3 velocity = BlastPush.Velocity(new Vector3(1f, 0f, 1f), new Vector3(1f, 2f, 4f), Vector3.right, 100f);
            Assert.AreEqual(0f, velocity.x, 0.001f);
            Assert.AreEqual(0f, velocity.y, 0.001f);
            Assert.AreEqual(100f * BlastPush.SpeedPerForce, velocity.z, 0.001f);
        }

        [Test]
        public void Velocity_ScalesWithForce_UpToTheCap()
        {
            float weak = BlastPush.Velocity(Vector3.zero, Vector3.right, Vector3.right, 45f).magnitude;
            float strong = BlastPush.Velocity(Vector3.zero, Vector3.right, Vector3.right, 135f).magnitude;
            Assert.AreEqual(strong, weak * 3f, 0.001f);
            Assert.AreEqual(BlastPush.MaxSpeed, BlastPush.Velocity(Vector3.zero, Vector3.right, Vector3.right, 1000f).magnitude, 0.001f);
        }

        [Test]
        public void Velocity_OnTheEpicenter_UsesTheFallback()
        {
            Vector3 velocity = BlastPush.Velocity(Vector3.one, Vector3.one, Vector3.back + Vector3.up, 100f);
            Assert.AreEqual(100f * BlastPush.SpeedPerForce, -velocity.z, 0.001f, "A body on the epicenter still goes somewhere");
            Assert.AreEqual(0f, velocity.y, 0.001f);
        }

        [Test]
        public void KnocksDown_OnlyCloseToTheEpicenter()
        {
            Assert.IsTrue(BlastPush.KnocksDown(135f), "One hop from a C4");
            Assert.IsTrue(BlastPush.KnocksDown(90f), "A minibomb's own tile");
            Assert.IsFalse(BlastPush.KnocksDown(45f), "A minibomb's next tile");
        }

        [Test]
        public void ShapedCharge_PushesOnce_WithBothPassesSummed()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            for (int x = 0; x <= 3; x++) TileMapTestUtilities.PlacePlenum(context, new Vector3(x, 0, 0));
            var calls = new List<(TileCoord Epicenter, Dictionary<TileCoord, float> Forces)>();
            var structural = new StructuralDamageService(context.Query, context.Construction);
            var blast = new BlastResolutionService(context.Query, structural, null, null,
                (epicenter, forces) => calls.Add((epicenter, new Dictionary<TileCoord, float>(forces))));
            TileCoord origin = context.Query.WorldToTile(Vector3.zero);

            blast.ResolveShaped(origin, 100f, 40f, 80f, 20f, Vector2.right);

            Assert.AreEqual(1, calls.Count, "Two passes are one detonation");
            Assert.AreEqual(origin, calls[0].Epicenter);
            Assert.AreEqual(60f + 60f, calls[0].Forces[context.Query.WorldToTile(Vector3.right)], 0.001f);
        }

        [Test]
        public void CrewDamage_RunsOncePerTile_WithBothPassesSummed()
        {
            // Tech-debt P2.5: crew brute used to run on every BFS visit, so a shaped charge's second pass hit again.
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            for (int x = 0; x <= 3; x++) TileMapTestUtilities.PlacePlenum(context, new Vector3(x, 0, 0));
            var hits = new Dictionary<TileCoord, List<float>>();
            var structural = new StructuralDamageService(context.Query, context.Construction);
            var blast = new BlastResolutionService(context.Query, structural, (coord, force) =>
            {
                if (!hits.TryGetValue(coord, out List<float> forces)) hits[coord] = forces = new List<float>();
                forces.Add(force);
            });
            TileCoord origin = context.Query.WorldToTile(Vector3.zero);

            blast.ResolveShaped(origin, 100f, 40f, 80f, 20f, Vector2.right);

            TileCoord next = context.Query.WorldToTile(Vector3.right);
            Assert.AreEqual(1, hits[next].Count, "one hit per tile per detonation");
            Assert.AreEqual(60f + 60f, hits[next][0], 0.001f);
            foreach (KeyValuePair<TileCoord, List<float>> tile in hits)
                Assert.AreEqual(1, tile.Value.Count, $"{tile.Key} was hit more than once");
        }

        [Test]
        public void UselessYield_PushesNobody()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            int calls = 0;
            var structural = new StructuralDamageService(context.Query, context.Construction);
            var blast = new BlastResolutionService(context.Query, structural, null, null, (_, _) => calls++);
            blast.Resolve(context.Query.WorldToTile(Vector3.zero), 0.5f, 1f);
            Assert.AreEqual(0, calls);
        }
    }
}
