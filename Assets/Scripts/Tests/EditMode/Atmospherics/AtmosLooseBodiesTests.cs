using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Tile;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    public class AtmosLooseBodiesTests
    {
        private const int RoomSize = 10;

        private readonly List<GameObject> _objects = new();
        private readonly List<Rigidbody> _bodies = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Rigidbody body in _bodies) AtmosLooseBodies.Unregister(body);
            _bodies.Clear();
            foreach (GameObject go in _objects) if (go != null) Object.DestroyImmediate(go);
            _objects.Clear();
        }

        [Test]
        public void LooseBodyAcceleration_ScalesWithMass_AndNeverBrakes()
        {
            Vector3 wind = new(4f, 0f, 0f);
            Vector3 oneKilo = AtmosWind.LooseBodyAcceleration(wind, Vector3.zero, 1f);
            Assert.That(AtmosWind.LooseBodyAcceleration(wind, Vector3.zero, 2f).x, Is.EqualTo(oneKilo.x / 2f).Within(0.001f));
            Assert.That(AtmosWind.LooseBodyAcceleration(wind, Vector3.zero, 0.01f).magnitude, Is.EqualTo(AtmosWind.MaxAcceleration).Within(0.001f));
            Assert.That(AtmosWind.LooseBodyAcceleration(wind, new Vector3(6f, 0f, 0f), 1f), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void CapKeepsTheBodiesInTheStrongestWind()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = Breached(context, out int hole);
            Rigidbody nearest = AddBody(new Vector3(RoomSize, 0.2f, hole));
            for (int x = 1; x <= RoomSize; x++)
            {
                for (int z = 1; z <= RoomSize; z++)
                {
                    if (x != RoomSize || z != hole) AddBody(new Vector3(x, 0.2f, z));
                }
            }

            var loose = new AtmosLooseBodies();
            Tick(sim, loose, 10);

            Assert.That(loose.PushedCount, Is.EqualTo(AtmosLooseBodies.MaxPushed));
            Assert.IsTrue(loose.IsPushed(nearest), "The body beside the hole is in the strongest wind");
        }

        [Test]
        public void StillAirAndHeldBodiesAreLeftAlone()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out _);
            Rigidbody still = AddBody(new Vector3(5f, 0.2f, 5f));
            var loose = new AtmosLooseBodies();
            Tick(sim, loose, 10);
            Assert.IsFalse(loose.IsPushed(still), "A sealed room has no wind");

            sim.Dispose();
            var breachedContext = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation breached = Breached(breachedContext, out int hole);
            Rigidbody free = AddBody(new Vector3(RoomSize, 0.2f, hole));
            Rigidbody held = AddBody(new Vector3(RoomSize, 0.2f, hole));
            held.isKinematic = true;
            var windy = new AtmosLooseBodies();
            Tick(breached, windy, 10);
            Assert.IsTrue(windy.IsPushed(free));
            Assert.IsFalse(windy.IsPushed(held), "Kinematic bodies are held or stored");
        }

        [Test]
        public void BodyWithoutGravitySlowsToRest_AndGravityGivesItsDampingBack()
        {
            var context = TileMapTestUtilities.CreateContext(_objects);
            using AtmosSimulation sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out _);
            Rigidbody body = AddBody(new Vector3(-20f, 0f, -20f));
            body.linearDamping = 0.1f;
            body.useGravity = false;
            var loose = new AtmosLooseBodies();

            loose.Tick(sim);

            Assert.That(loose.AdriftCount, Is.EqualTo(1));
            Assert.That(body.linearDamping, Is.EqualTo(AtmosLooseBodies.DriftDamping));

            body.useGravity = true;
            loose.Tick(sim);

            Assert.That(loose.AdriftCount, Is.Zero);
            Assert.That(body.linearDamping, Is.EqualTo(0.1f));
        }

        private static AtmosSimulation Breached(TileMapTestUtilities.MapContext context, out int hole)
        {
            AtmosSimulation sim = AtmosTestFixtures.CreateSealedRoomSimulation(context, RoomSize, out int map);
            hole = RoomSize / 2;
            context.Construction.TryClearTile(new Vector3(RoomSize + 1, 0, hole), TileLayer.Turf, Direction.North);
            sim.QueueCellRefresh(new TileCoord(map, RoomSize + 1, hole));
            return sim;
        }

        private static void Tick(AtmosSimulation sim, AtmosLooseBodies loose, int ticks)
        {
            for (int tick = 0; tick < ticks; tick++)
            {
                sim.Tick(AtmosConstants.TickInterval);
                loose.Tick(sim);
            }
        }

        private Rigidbody AddBody(Vector3 position)
        {
            var go = new GameObject("loose body");
            _objects.Add(go);
            go.transform.position = position;
            var body = go.AddComponent<Rigidbody>();
            AtmosLooseBodies.Register(body);
            _bodies.Add(body);
            return body;
        }
    }
}
