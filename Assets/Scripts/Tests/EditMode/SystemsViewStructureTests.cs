using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Structure stream of the Map Editor systems views (Documents/architecture/2026-11_map-editor-systems-views.md Q2):
    /// a client is sent only what changed since what it was last sent, and nothing when nothing did.
    /// </summary>
    public sealed class SystemsViewStructureTests
    {
        private static SystemsViewKey Key(int x, int y, byte slot = 0) => new(new Vector2Int(x, y), slot);

        private static SystemsViewStructure Station(byte ventState = 1)
        {
            SystemsViewStructure structure = new(SystemsViewKind.Atmos);
            for (int x = 0; x < 4; x++)
                structure.Set(new SystemsViewSegment { Key = Key(x, 0), ViewId = 1 });
            structure.Set(new SystemsViewSegment { Key = Key(5, 0), ViewId = 2 });
            structure.Set(new SystemsViewNetwork { ViewId = 1, Label = "Supply 1" });
            structure.Set(new SystemsViewNetwork { ViewId = 2, Label = "Supply 2" });
            structure.Set(new SystemsViewBadge { Key = Key(0, 0, 7), Kind = 1, State = ventState, Facing = Direction.North, ViewId = 1 });
            structure.Set(new SystemsViewBadge { Key = Key(4, 0, 7), Kind = 2, State = 0, Facing = Direction.East, ViewId = 1, OtherViewId = 2 });
            structure.Set(new SystemsViewMarker { Key = Key(5, 0, 1), IssueId = "atmos-open-end" });
            return structure;
        }

        private static void AssertSame(SystemsViewStructure expected, SystemsViewStructure actual)
        {
            Assert.AreEqual(expected.View, actual.View);
            CollectionAssert.AreEquivalent(expected.Segments, actual.Segments);
            CollectionAssert.AreEquivalent(expected.Networks, actual.Networks);
            CollectionAssert.AreEquivalent(expected.Badges, actual.Badges);
            CollectionAssert.AreEquivalent(expected.Markers, actual.Markers);
        }

        [Test]
        public void FirstSend_IsAResetCarryingEverything()
        {
            SystemsViewDelta delta = SystemsViewStructure.Diff(null, Station());

            Assert.IsTrue(delta.Reset);
            Assert.IsFalse(delta.IsEmpty);
            Assert.AreEqual(5, delta.SegmentsSet.Length);
            Assert.AreEqual(2, delta.NetworksSet.Length);
            Assert.AreEqual(2, delta.BadgesSet.Length);
            Assert.AreEqual(1, delta.MarkersSet.Length);
            SystemsViewStructure client = new(SystemsViewKind.Off);
            client.Apply(delta);
            AssertSame(Station(), client);
        }

        [Test]
        public void NoChange_SendsNothing()
        {
            SystemsViewDelta delta = SystemsViewStructure.Diff(Station(), Station());

            Assert.IsTrue(delta.IsEmpty);
            Assert.IsNotNull(delta.SegmentsSet);
            Assert.IsNotNull(delta.MarkersRemoved);
        }

        [Test]
        public void BadgeChange_SendsOnlyThatBadge()
        {
            SystemsViewDelta delta = SystemsViewStructure.Diff(Station(ventState: 1), Station(ventState: 0));

            Assert.IsFalse(delta.Reset);
            Assert.AreEqual(1, delta.BadgesSet.Length);
            Assert.AreEqual(Key(0, 0, 7), delta.BadgesSet[0].Key);
            Assert.AreEqual(0, delta.BadgesSet[0].State);
            Assert.IsEmpty(delta.BadgesRemoved);
            Assert.IsEmpty(delta.SegmentsSet);
            Assert.IsEmpty(delta.SegmentsRemoved);
            Assert.IsEmpty(delta.NetworksSet);
            Assert.IsEmpty(delta.MarkersSet);
        }

        [Test]
        public void Removals_AreSentByKey_AndApplyMatchesTheNewState()
        {
            SystemsViewStructure before = Station();
            SystemsViewStructure after = Station();
            after.Segments.Remove(Key(5, 0));
            after.Networks.Remove(2);
            after.Markers.Clear();
            after.Set(new SystemsViewSegment { Key = Key(3, 0), ViewId = 3 });

            SystemsViewDelta delta = SystemsViewStructure.Diff(before, after);

            CollectionAssert.AreEqual(new[] { Key(5, 0) }, delta.SegmentsRemoved);
            CollectionAssert.AreEqual(new[] { 2 }, delta.NetworksRemoved);
            CollectionAssert.AreEqual(new[] { Key(5, 0, 1) }, delta.MarkersRemoved);
            Assert.AreEqual(1, delta.SegmentsSet.Length);
            before.Apply(delta);
            AssertSame(after, before);
        }

        [Test]
        public void SentForAnotherView_IsAReset()
        {
            SystemsViewStructure power = new(SystemsViewKind.Power);

            SystemsViewDelta delta = SystemsViewStructure.Diff(power, Station());

            Assert.IsTrue(delta.Reset);
            power.Apply(delta);
            AssertSame(Station(), power);
        }

        [Test]
        public void Publisher_SendsAResetOnSubscribe_ThenOnlyChanges()
        {
            FakeSource source = new(SystemsViewKind.Atmos) { Structure = Station(ventState: 1) };
            SystemsViewPublisher publisher = new();
            publisher.Register(source);
            publisher.Subscribe(7, SystemsViewKind.Atmos);
            List<(int Client, SystemsViewDelta Delta)> sent = new();

            publisher.Publish((client, delta) => sent.Add((client, delta)));
            Assert.AreEqual(1, sent.Count);
            Assert.AreEqual(7, sent[0].Client);
            Assert.IsTrue(sent[0].Delta.Reset);

            // No change: no build, nothing sent.
            sent.Clear();
            publisher.Publish((client, delta) => sent.Add((client, delta)));
            Assert.IsEmpty(sent);
            Assert.AreEqual(1, source.Builds);

            // A spurious version bump costs a build, not traffic.
            source.Version++;
            publisher.Publish((client, delta) => sent.Add((client, delta)));
            Assert.IsEmpty(sent);
            Assert.AreEqual(2, source.Builds);

            source.Structure = Station(ventState: 0);
            source.Version++;
            publisher.Publish((client, delta) => sent.Add((client, delta)));
            Assert.AreEqual(1, sent.Count);
            Assert.IsFalse(sent[0].Delta.Reset);
            Assert.AreEqual(1, sent[0].Delta.BadgesSet.Length);
            Assert.IsEmpty(sent[0].Delta.SegmentsSet);
        }

        [Test]
        public void Publisher_TracksWhatEachClientWasSent()
        {
            FakeSource source = new(SystemsViewKind.Atmos) { Structure = Station(ventState: 1) };
            SystemsViewPublisher publisher = new();
            publisher.Register(source);
            publisher.Subscribe(1, SystemsViewKind.Atmos);
            publisher.Publish((_, _) => { });

            // A late subscriber gets everything; the first only the change.
            source.Structure = Station(ventState: 0);
            source.Version++;
            publisher.Subscribe(2, SystemsViewKind.Atmos);
            Dictionary<int, SystemsViewDelta> sent = new();
            publisher.Publish((client, delta) => sent[client] = delta);

            Assert.IsFalse(sent[1].Reset);
            Assert.AreEqual(1, sent[1].BadgesSet.Length);
            Assert.IsTrue(sent[2].Reset);
            Assert.AreEqual(2, sent[2].BadgesSet.Length);
        }

        [Test]
        public void Publisher_Unsubscribed_SendsNothing_AndSwitchingViewResets()
        {
            SystemsViewPublisher publisher = new();
            publisher.Register(new FakeSource(SystemsViewKind.Atmos) { Structure = Station() });
            publisher.Subscribe(3, SystemsViewKind.Atmos);
            publisher.Publish((_, _) => { });

            // Power has no source yet: the client still gets an (empty) reset for it.
            publisher.Subscribe(3, SystemsViewKind.Power);
            Assert.AreEqual(SystemsViewKind.Power, publisher.ViewOf(3));
            List<SystemsViewDelta> sent = new();
            publisher.Publish((_, delta) => sent.Add(delta));
            Assert.AreEqual(1, sent.Count);
            Assert.IsTrue(sent[0].Reset);
            Assert.AreEqual(SystemsViewKind.Power, sent[0].View);
            Assert.IsEmpty(sent[0].SegmentsSet);

            sent.Clear();
            Assert.IsTrue(publisher.Unsubscribe(3));
            Assert.IsFalse(publisher.HasSubscribers);
            publisher.Publish((_, delta) => sent.Add(delta));
            Assert.IsEmpty(sent);

            publisher.Subscribe(3, SystemsViewKind.Off);
            Assert.AreEqual(SystemsViewKind.Off, publisher.ViewOf(3));
        }

        [Test]
        public void OverlayChunkMesh_HasAQuadPerSegment_InItsNetworkColour()
        {
            SystemsViewSegment[] segments =
            {
                new() { Key = Key(-1, -1), ViewId = 1 },
                new() { Key = Key(0, 0), ViewId = 2 },
                new() { Key = Key(17, 3), ViewId = 2 },
            };

            Dictionary<Vector2Int, List<SystemsViewSegment>> chunks = MapEditorSystemsOverlay.GroupByChunk(segments);

            CollectionAssert.AreEquivalent(new[] { new Vector2Int(-1, -1), Vector2Int.zero, new Vector2Int(1, 0) }, chunks.Keys);
            Mesh mesh = MapEditorSystemsOverlay.BuildChunkMesh(new Vector2Int(1, 0), chunks[new Vector2Int(1, 0)]);
            try
            {
                Assert.AreEqual(4, mesh.vertexCount);
                Color expected = SystemsViewPalette.ColorFor(2);
                Assert.AreEqual(expected.r, mesh.colors[0].r, 1e-4f);
                Assert.AreEqual(1f, mesh.bounds.center.x, 1e-4f);
                Assert.AreEqual(3f, mesh.bounds.center.z, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        private sealed class FakeSource : ISystemsViewSource
        {
            public FakeSource(SystemsViewKind view) => View = view;

            public SystemsViewKind View { get; }
            public int Version;
            public int Builds;
            public SystemsViewStructure Structure;

            public int StructureVersion => Version;

            public SystemsViewStructure BuildStructure()
            {
                Builds++;
                return Structure;
            }
        }
    }
}
