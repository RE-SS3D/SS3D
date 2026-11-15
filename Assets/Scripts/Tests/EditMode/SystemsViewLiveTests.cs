using System.Collections.Generic;
using System.Linq;
using FishNet.Serializing;
using NUnit.Framework;
using SS3D.Systems.Atmospherics.SystemsView;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using static SS3D.Systems.Atmospherics.SystemsView.AtmosSystemsViewCodes;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// The live stream of the Map Editor systems views (Documents/architecture/2026-11_map-editor-systems-views.md Q2,
    /// Phase 2): a trace per connection, numbers only to the client tracing them and only when they change, a network
    /// that goes leaving the trace, and the trace ending with the subscription.
    /// </summary>
    public sealed class SystemsViewLiveTests
    {
        private FakeSource _source;
        private SystemsViewPublisher _publisher;
        private List<(int Client, SystemsViewLive Live)> _sent;

        [SetUp]
        public void SetUp()
        {
            _source = new FakeSource();
            _source.Set(1, "1.0 kPa");
            _source.Set(2, "2.0 kPa");
            _source.Set(3, "3.0 kPa");
            _publisher = new SystemsViewPublisher();
            _publisher.Register(_source);
            _publisher.Subscribe(1, SystemsViewKind.Atmos);
            _publisher.Subscribe(2, SystemsViewKind.Atmos);
            _sent = new List<(int, SystemsViewLive)>();
        }

        // As the hub does: the structure first, then the live numbers.
        private void Poll()
        {
            _publisher.Publish((_, _) => { });
            _publisher.PublishLive((client, live) => _sent.Add((client, live)));
        }

        private static string Pressure(SystemsViewLive live, int viewId) =>
            live.Networks.Single(network => network.ViewId == viewId).ValueOf("Pressure");

        [Test]
        public void Live_GoesOnlyToTheTracingClient_AndOnlyWhenItChanges()
        {
            Assert.IsTrue(_publisher.Trace(1, SystemsViewKind.Atmos, 7, new[] { 2 }));
            Poll();
            Assert.AreEqual(1, _sent.Count, "The first read goes out");
            Assert.AreEqual(1, _sent[0].Client, "Only to the client tracing");
            Assert.AreEqual(7, _sent[0].Live.TraceId, "Under the client's own trace id");
            Assert.AreEqual(SystemsViewKind.Atmos, _sent[0].Live.View);
            Assert.AreEqual("2.0 kPa", Pressure(_sent[0].Live, 2));
            Assert.IsFalse(_sent[0].Live.Paused);

            int reads = _source.LiveReads;
            Poll();
            Assert.AreEqual(1, _sent.Count, "No change: nothing sent");
            Assert.Greater(_source.LiveReads, reads, "but it was read again");

            _source.Set(3, "3.5 kPa");
            Poll();
            Assert.AreEqual(1, _sent.Count, "A change on a network nobody traces sends nothing");

            _source.Set(2, "2.5 kPa");
            Poll();
            Assert.AreEqual(2, _sent.Count);
            Assert.AreEqual(1, _sent[1].Client);
            Assert.AreEqual("2.5 kPa", Pressure(_sent[1].Live, 2));

            // Pausing holds the numbers still on purpose, so the panel is told (Phase 5's pause sets it).
            _source.SimulationPaused = true;
            Poll();
            Assert.AreEqual(3, _sent.Count);
            Assert.IsTrue(_sent[2].Live.Paused);
            Poll();
            Assert.AreEqual(3, _sent.Count, "Still paused, still the same: nothing");
        }

        [Test]
        public void TraceState_IsPerConnection()
        {
            _source.Set(4, "4.0 kPa");
            Assert.IsTrue(_publisher.Trace(1, SystemsViewKind.Atmos, 1, new[] { 3, 3, 0, -1, 1, 2, 4, 5 }));
            Assert.IsTrue(_publisher.Trace(2, SystemsViewKind.Atmos, 9, new[] { 2 }));
            CollectionAssert.AreEqual(new[] { 3, 1, 2, 4 }, _publisher.TracedBy(1),
                "Ids under 1 and repeats are dropped, at most a manifold's four lines are kept, in the client's order");
            CollectionAssert.AreEqual(new[] { 2 }, _publisher.TracedBy(2));
            Assert.IsFalse(_publisher.Trace(1, SystemsViewKind.Power, 2, new[] { 1 }), "A trace must be on the view the client watches");
            Assert.IsFalse(_publisher.Trace(3, SystemsViewKind.Atmos, 1, new[] { 1 }), "A client that watches nothing can't trace");
            CollectionAssert.AreEqual(new[] { 3, 1, 2, 4 }, _publisher.TracedBy(1), "A refused trace leaves the old one");

            Poll();
            Assert.AreEqual(2, _sent.Count);
            SystemsViewLive first = _sent.Single(s => s.Client == 1).Live;
            CollectionAssert.AreEqual(new[] { 3, 1, 2, 4 }, first.Networks.Select(n => n.ViewId), "Every traced network, in order");
            Assert.AreEqual(9, _sent.Single(s => s.Client == 2).Live.TraceId);

            // Client 2 moves its trace: only it gets the new one, under its new id.
            _publisher.Trace(2, SystemsViewKind.Atmos, 10, new[] { 1 });
            Poll();
            Assert.AreEqual(3, _sent.Count);
            Assert.AreEqual(2, _sent[2].Client);
            Assert.AreEqual(10, _sent[2].Live.TraceId);
            Assert.AreEqual("1.0 kPa", Pressure(_sent[2].Live, 1));

            // No ids ends a trace; nothing more goes to that client.
            _publisher.Trace(2, SystemsViewKind.Atmos, 11, System.Array.Empty<int>());
            Assert.IsEmpty(_publisher.TracedBy(2));
            _source.Set(1, "1.5 kPa");
            Poll();
            Assert.IsTrue(_sent.Skip(3).All(s => s.Client == 1), "Only the client still tracing hears of the change");
        }

        [Test]
        public void Unsubscribing_OrSwitchingView_EndsTheTrace()
        {
            _publisher.Trace(1, SystemsViewKind.Atmos, 1, new[] { 1 });
            Assert.IsTrue(_publisher.HasTraces);
            Poll();
            Assert.AreEqual(1, _sent.Count);

            Assert.IsTrue(_publisher.Unsubscribe(1), "The editor closed");
            Assert.IsEmpty(_publisher.TracedBy(1));
            Assert.IsFalse(_publisher.HasTraces);
            _source.Set(1, "9.0 kPa");
            Poll();
            Assert.AreEqual(1, _sent.Count, "Nothing to a client that left");

            // Opening again starts with no trace.
            _publisher.Subscribe(1, SystemsViewKind.Atmos);
            Assert.IsEmpty(_publisher.TracedBy(1));
            Poll();
            Assert.AreEqual(1, _sent.Count);

            _publisher.Trace(1, SystemsViewKind.Atmos, 2, new[] { 1 });
            _publisher.Subscribe(1, SystemsViewKind.Atmos);
            CollectionAssert.AreEqual(new[] { 1 }, _publisher.TracedBy(1), "Asking for the same view again keeps the trace");
            _publisher.Subscribe(1, SystemsViewKind.Power);
            Assert.IsEmpty(_publisher.TracedBy(1), "Another view starts without one");
            _publisher.Subscribe(1, SystemsViewKind.Off);
            Assert.IsFalse(_publisher.HasTraces);
        }

        [Test]
        public void ANetworkThatGoes_LeavesTheTrace_AndTheLastOneGoingEndsIt()
        {
            _publisher.Trace(1, SystemsViewKind.Atmos, 4, new[] { 1, 2 });
            Poll();
            Assert.AreEqual(2, _sent[0].Live.Networks.Length);

            // An edit removed network 2 (its view id is gone from the source).
            _source.Remove(2);
            Poll();
            Assert.AreEqual(2, _sent.Count);
            CollectionAssert.AreEqual(new[] { 1 }, _sent[1].Live.Networks.Select(n => n.ViewId));
            CollectionAssert.AreEqual(new[] { 1 }, _publisher.TracedBy(1));

            // Then network 1: one last, empty message tells the client the trace is gone, then nothing.
            _source.Remove(1);
            Poll();
            Assert.AreEqual(3, _sent.Count);
            Assert.IsEmpty(_sent[2].Live.Networks);
            Assert.AreEqual(4, _sent[2].Live.TraceId);
            Assert.IsFalse(_publisher.HasTraces);
            Poll();
            Assert.AreEqual(3, _sent.Count);
        }

        [Test]
        public void Wire_RoundTripsALiveMessage()
        {
            SystemsViewLive live = new()
            {
                View = SystemsViewKind.Atmos,
                TraceId = 12,
                Paused = true,
                Networks = new[]
                {
                    new SystemsViewLiveNetwork
                    {
                        ViewId = 3,
                        Rows = new[] { new SystemsViewLiveRow("Pressure", "101.3 kPa"), new SystemsViewLiveRow("O2", "21 %") },
                        Devices = new[] { new SystemsViewLiveDevice { Key = new SystemsViewKey(new Vector2Int(-4, 9), 7), Label = "Vent", Detail = "on" } },
                    },
                    new SystemsViewLiveNetwork { ViewId = 5, Rows = new SystemsViewLiveRow[0], Devices = new SystemsViewLiveDevice[0] },
                },
            };

            PooledWriter writer = WriterPool.Retrieve();
            try
            {
                writer.WriteSystemsViewLive(live);
                PooledReader reader = ReaderPool.Retrieve(writer.GetArraySegment(), null);
                SystemsViewLive read = reader.ReadSystemsViewLive();
                reader.Store();
                Assert.IsTrue(read.Equals(live), "Every field survives the wire");
                Assert.AreEqual("21 %", read.Networks[0].ValueOf("O2"));
            }
            finally
            {
                writer.Store();
            }
        }

        [Test]
        public void Client_TracesBothSidesOfAPump_AndIgnoresLiveForAnOlderTrace()
        {
            MapEditorSystemsViewClient client = new();
            try
            {
                client.Update(SystemsViewKind.Atmos);
                SystemsViewStructure structure = new(SystemsViewKind.Atmos);
                structure.Set(new SystemsViewSegment { Key = new SystemsViewKey(new Vector2Int(0, 0), Surface), ViewId = 1 });
                structure.Set(new SystemsViewSegment { Key = new SystemsViewKey(new Vector2Int(2, 0), Surface), ViewId = 2 });
                structure.Set(new SystemsViewNetwork { ViewId = 1, Label = "Surface 1" });
                structure.Set(new SystemsViewNetwork { ViewId = 2, Label = "Surface 2" });
                structure.Set(new SystemsViewBadge
                {
                    Key = new SystemsViewKey(new Vector2Int(1, 0), (byte)TileLayer.PipeSurface),
                    Kind = KindPump, State = On, Facing = Direction.East, ViewId = 1, OtherViewId = 2,
                });
                Assert.IsTrue(client.Receive(SystemsViewStructure.Diff(null, structure)));

                Assert.IsTrue(client.BeginTrace(new Vector2Int(1, 0), TileLayer.PipeSurface), "Select on the pump traces it");
                CollectionAssert.AreEqual(new[] { new SystemsViewTraceSide(1, "Inlet"), new SystemsViewTraceSide(2, "Outlet") }, client.TraceSides);
                Assert.AreEqual("Surface 1 · Inlet", client.TraceLabel(client.TraceSides[0]));
                CollectionAssert.AreEquivalent(new[] { 1, 2 }, client.Overlay.Focus, "Everything else dims");
                Assert.IsTrue(client.TakeTraceRequest(out int traceId, out int[] ids));
                CollectionAssert.AreEqual(new[] { 1, 2 }, ids);
                Assert.IsFalse(client.TakeTraceRequest(out _, out _), "Asked once");

                SystemsViewLive Live(int id, params int[] networks) => new()
                {
                    View = SystemsViewKind.Atmos,
                    TraceId = id,
                    Networks = networks.Select(n => new SystemsViewLiveNetwork
                    {
                        ViewId = n, Rows = new[] { new SystemsViewLiveRow("Pressure", "1.0 kPa") }, Devices = new SystemsViewLiveDevice[0],
                    }).ToArray(),
                };

                Assert.IsFalse(client.ReceiveLive(Live(traceId - 1, 1, 2)), "A reply to an older trace is ignored");
                Assert.IsTrue(client.ReceiveLive(Live(traceId, 1, 2)));
                Assert.AreEqual(2, client.Live.Value.Networks.Length);

                // The server dropped the outlet's network (it went in an edit): that side leaves the trace.
                Assert.IsTrue(client.ReceiveLive(Live(traceId, 1)));
                CollectionAssert.AreEqual(new[] { new SystemsViewTraceSide(1, "Inlet") }, client.TraceSides);

                // A Select on nothing in the view ends the trace, and the server is told.
                Assert.IsFalse(client.BeginTrace(new Vector2Int(9, 9), TileLayer.PipeSurface));
                Assert.IsFalse(client.IsTracing);
                Assert.IsEmpty(client.Overlay.Focus);
                Assert.IsTrue(client.TakeTraceRequest(out int endId, out int[] none));
                Assert.Greater(endId, traceId);
                Assert.IsEmpty(none);

                // Switching the view ends a trace with no request: the server starts the new view without one.
                client.BeginTrace(new Vector2Int(0, 0), TileLayer.PipeSurface);
                client.TakeTraceRequest(out _, out _);
                client.Update(SystemsViewKind.Off);
                Assert.IsFalse(client.IsTracing);
                Assert.IsFalse(client.TakeTraceRequest(out _, out _));
            }
            finally
            {
                DestroyOverlay();
            }
        }

        [Test]
        public void AManifoldOnFourLines_TracesAndPublishesAllFour()
        {
            SystemsViewStructure structure = new(SystemsViewKind.Atmos);
            for (byte line = Supply; line <= Surface; line++)
                structure.Set(new SystemsViewSegment { Key = new SystemsViewKey(new Vector2Int(4, 4), (byte)(ManifoldSlotBase + line)), ViewId = 10 + line });
            List<SystemsViewTraceSide> targets = new();
            AtmosSystemsViewStyle.Instance.TraceTargets(structure, new Vector2Int(4, 4), TileLayer.PipeManifold, targets);
            CollectionAssert.AreEqual(new[] { "Supply line", "Mixed line", "Waste line", "Surface line" }, targets.Select(t => t.Side));
            Assert.LessOrEqual(targets.Count, SystemsViewPublisher.MaxTracedNetworks, "The cap covers every target a style gives");

            foreach (SystemsViewTraceSide target in targets)
                _source.Set(target.ViewId, "1.0 kPa");
            _publisher.Trace(1, SystemsViewKind.Atmos, 1, targets.Select(t => t.ViewId).ToArray());
            Poll();
            CollectionAssert.AreEqual(new[] { 10, 11, 12, 13 }, _sent.Single().Live.Networks.Select(n => n.ViewId), "None of the four lines is dropped");
        }

        [Test]
        public void SelectOnFurnitureOverAPipe_TracesThePipe_OnlyOnThePointedTile()
        {
            MapEditorSystemsViewClient client = new();
            try
            {
                client.Update(SystemsViewKind.Atmos);
                SystemsViewStructure structure = new(SystemsViewKind.Atmos);
                structure.Set(new SystemsViewSegment { Key = new SystemsViewKey(new Vector2Int(3, 3), Supply), ViewId = 1 });
                structure.Set(new SystemsViewSegment { Key = new SystemsViewKey(new Vector2Int(8, 3), Supply), ViewId = 2 });
                // A vent on another line on the same tile: the view's own layer (the pipe) comes first.
                structure.Set(new SystemsViewBadge { Key = new SystemsViewKey(new Vector2Int(3, 3), (byte)TileLayer.FurnitureBase), Kind = KindVent, ViewId = 3 });
                structure.Set(new SystemsViewNetwork { ViewId = 1, Label = "Supply 1" });
                client.Receive(SystemsViewStructure.Diff(null, structure));

                // The raycast picked the table over the pipe: not part of the view, so the pointed tile's pipe is traced.
                Assert.IsTrue(client.TraceSelect(new Vector2Int(3, 3), TileLayer.FurnitureTop, new Vector2Int(3, 3)));
                CollectionAssert.AreEqual(new[] { new SystemsViewTraceSide(1) }, client.TraceSides);
                Assert.AreEqual(new Vector2Int(3, 3), client.TraceOrigin);

                // A wall picked far from the cursor: only the pointed tile counts, never a pipe beside it.
                Assert.IsFalse(client.TraceSelect(new Vector2Int(8, 3), TileLayer.Turf, new Vector2Int(5, 3)),
                    "Nothing in the view under the cursor: no trace, even with a pipe on the picked wall's row");
                Assert.IsFalse(client.IsTracing);
                Assert.IsTrue(client.TraceSelect(new Vector2Int(20, 20), TileLayer.Turf, new Vector2Int(8, 3)));
                CollectionAssert.AreEqual(new[] { new SystemsViewTraceSide(2) }, client.TraceSides);
            }
            finally
            {
                DestroyOverlay();
            }
        }

        // The overlay made its root and materials; EditMode can't Destroy, so take them down here.
        private static void DestroyOverlay()
        {
            foreach (GameObject root in Resources.FindObjectsOfTypeAll<GameObject>().Where(go => go.name == "MapEditorSystemsOverlay").ToArray())
                Object.DestroyImmediate(root);
            foreach (Material material in Resources.FindObjectsOfTypeAll<Material>()
                         .Where(m => m.name is "MapEditorSystemsRuns" or "MapEditorSystemsBadges").ToArray())
                Object.DestroyImmediate(material);
        }

        private sealed class FakeSource : ISystemsViewSource
        {
            private readonly Dictionary<int, SystemsViewLiveNetwork> _live = new();

            public SystemsViewKind View => SystemsViewKind.Atmos;
            public int StructureVersion => 1;
            public SystemsViewStructure BuildStructure() => new(SystemsViewKind.Atmos);
            public bool SimulationPaused { get; set; }
            public int LiveReads { get; private set; }

            public void Set(int viewId, string pressure) => _live[viewId] = new SystemsViewLiveNetwork
            {
                ViewId = viewId,
                Rows = new[] { new SystemsViewLiveRow("Pressure", pressure) },
                Devices = new SystemsViewLiveDevice[0],
            };

            public void Remove(int viewId) => _live.Remove(viewId);

            public bool ReadLive(int viewId, out SystemsViewLiveNetwork live)
            {
                LiveReads++;
                return _live.TryGetValue(viewId, out live);
            }

            public IReadOnlyList<SystemsViewIssue> Analyze() => SystemsViewProblems.None;

            public bool AnalysisCurrent => true;
        }
    }
}
