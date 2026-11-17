using NUnit.Framework;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;

namespace EditorTests
{
    public class BlastFlameAndDebrisTests
    {
        private static readonly TileCoord Epicenter = new(0, 10, -4);

        [Test]
        public void FlameTiles_RoundTripOffsetsAndRelativeStrength()
        {
            var forces = new Dictionary<TileCoord, float>
            {
                [Epicenter] = 200f,
                [new TileCoord(0, 12, -4)] = 100f,
                [new TileCoord(0, 7, -9)] = 50f,
            };

            List<BlastFlameTiles.Flame> flames = BlastFlameTiles.Unpack(BlastFlameTiles.Pack(Epicenter, forces));

            Assert.AreEqual(3, flames.Count);
            Assert.AreEqual(Vector2Int.zero, flames[0].Offset);
            Assert.AreEqual(1f, flames[0].Strength, 0.01f);
            Assert.AreEqual(new Vector2Int(2, 0), flames[1].Offset);
            Assert.AreEqual(0.5f, flames[1].Strength, 0.01f);
            Assert.AreEqual(new Vector2Int(-3, -5), flames[2].Offset, "Negative offsets survive the signed bytes");
            Assert.AreEqual(8 * BlastFlameTiles.HopSeconds, flames[2].Delay, 0.0001f, "Catches one hop per tile of grid distance");
        }

        [Test]
        public void FlameTiles_DropOtherMapsSpentForceAndOutOfRangeTiles()
        {
            var forces = new Dictionary<TileCoord, float>
            {
                [Epicenter] = 200f,
                [new TileCoord(1, 11, -4)] = 150f,
                [new TileCoord(0, 11, -4)] = 0.5f,
                [new TileCoord(0, 10 + 200, -4)] = 150f,
            };

            Assert.AreEqual(1, BlastFlameTiles.Unpack(BlastFlameTiles.Pack(Epicenter, forces)).Count);
            Assert.IsEmpty(BlastFlameTiles.Pack(Epicenter, null));
            Assert.IsEmpty(BlastFlameTiles.Unpack(null));
        }

        [Test]
        public void FlameTiles_CapKeepsTheStrongestTiles()
        {
            var forces = new Dictionary<TileCoord, float>();
            for (int x = -40; x <= 40; x++)
            for (int y = -40; y <= 40; y++)
                forces[new TileCoord(0, Epicenter.Grid.x + x, Epicenter.Grid.y + y)] = 500f - (Mathf.Abs(x) + Mathf.Abs(y)) * 5f;

            List<BlastFlameTiles.Flame> flames = BlastFlameTiles.Unpack(BlastFlameTiles.Pack(Epicenter, forces));

            Assert.AreEqual(BlastFlameTiles.MaxTiles, flames.Count);
            // 2k² + 2k + 1 tiles lie within k hops: 1013 at k = 22.
            Assert.That(flames.TrueForAll(f => Mathf.Abs(f.Offset.x) + Mathf.Abs(f.Offset.y) <= 22));
        }

        [Test]
        public void DebrisLaunch_GoesOutwardAndUpFasterCloserIn()
        {
            Vector3 epicenter = new(10f, 0f, -4f);

            BlastDebrisPush.Launch(epicenter, 10f, epicenter + new Vector3(2f, 1f, 0f), out Vector3 near, out float nearSpeed);
            BlastDebrisPush.Launch(epicenter, 10f, epicenter + new Vector3(0f, 1f, -8f), out Vector3 far, out float farSpeed);

            Assert.Greater(near.x, 0.5f, "East of the epicenter flies east");
            Assert.Greater(near.y, 0f, "and arcs up");
            Assert.Less(far.z, -0.5f, "South of the epicenter flies south");
            Assert.Greater(nearSpeed, farSpeed);
            Assert.That(farSpeed, Is.InRange(BlastDebrisPush.MinSpeed, BlastDebrisPush.MaxSpeed));
        }

        [Test]
        public void DebrisLaunch_OnTheEpicenterGoesStraightUp()
        {
            BlastDebrisPush.Launch(Vector3.zero, 6f, new Vector3(0.1f, 0.5f, 0f), out Vector3 direction, out float speed);

            Assert.AreEqual(Vector3.up, direction);
            Assert.AreEqual(BlastDebrisPush.MaxSpeed, speed, 0.5f);
        }

        [Test]
        public void DebrisPush_OnlyAppliesInsideAFreshBlastsReach()
        {
            BlastDebrisPush.Clear();
            try
            {
                Assert.IsFalse(BlastDebrisPush.TryGetPush(Vector3.zero, out _, out _), "No blast, no push");

                BlastDebrisPush.Begin(Vector3.zero, 5f);
                Assert.IsTrue(BlastDebrisPush.TryGetPush(new Vector3(3f, 1f, 0f), out Vector3 direction, out _));
                Assert.Greater(direction.x, 0f);
                Assert.IsFalse(BlastDebrisPush.TryGetPush(new Vector3(20f, 0f, 0f), out _, out _), "Beyond its reach");
            }
            finally
            {
                BlastDebrisPush.Clear();
            }
        }
    }
}
