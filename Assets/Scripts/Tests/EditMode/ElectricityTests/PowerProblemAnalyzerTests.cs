using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Electricity.SystemsView;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.Validation;
using UnityEngine;
using static SS3D.Systems.Electricity.SystemsView.PowerSystemsViewCodes;

namespace EditorTests
{
    /// <summary>
    /// The Power view's problems (Documents/architecture/2026-11_map-editor-systems-views.md Phase 4) on hand-built data:
    /// a clean station that raises nothing, then one fixture per v1 problem.
    /// </summary>
    public class PowerProblemAnalyzerTests
    {
        private const ushort Bridge = 1;
        private const ushort Storeroom = 2;

        // Circuit 1: a 10 kW generator, a 50 kW SMES and two APCs (the Bridge's and the Storeroom's); the Bridge draws 3 kW
        // over two machines and the Storeroom 1 kW.
        private static PowerProblemInput Clean()
        {
            PowerProblemInput input = new();
            input.Devices.Add(Device(KindGenerator, 0, 0, circuit: 1, supply: 10f));
            input.Devices.Add(Device(KindSmes, 2, 0, circuit: 1, supply: 50f));
            input.Devices.Add(Device(KindApc, 4, 0, circuit: 1, area: Bridge));
            input.Devices.Add(Device(KindApc, 8, 0, circuit: 1, area: Storeroom));
            input.Devices.Add(Device(KindLight, 1, 3, circuit: 1, area: Bridge, demand: 1f));
            input.Devices.Add(Device(KindConsumer, 2, 3, circuit: 1, area: Bridge, demand: 2f));
            input.Devices.Add(Device(KindConsumer, 8, 3, circuit: 1, area: Storeroom, demand: 1f));
            input.Rooms.Add(new PowerProblemRoom { AreaId = Bridge, Name = "Bridge", HasApc = true });
            input.Rooms.Add(new PowerProblemRoom { AreaId = Storeroom, Name = "Storeroom", HasApc = true });
            return input;
        }

        private static PowerProblemDevice Device(byte kind, int x, int y, int circuit = 0, ushort area = 0, float supply = 0f,
            float demand = 0f) => new()
        {
            Kind = kind,
            Key = new SystemsViewKey(new Vector2Int(x, y), DeviceSlot(SS3D.Systems.Tile.TileLayer.FurnitureBase, SS3D.Systems.Tile.Direction.North)),
            Position = new Vector3(x, 0, y),
            CircuitViewId = circuit,
            AreaId = area,
            SupplyKw = supply,
            DemandKw = demand,
        };

        private static List<SystemsViewIssue> Analyze(PowerProblemInput input) => PowerProblemAnalyzer.Analyze(input);

        private static void Set(PowerProblemInput input, byte kind, System.Func<PowerProblemDevice, PowerProblemDevice> change)
        {
            for (int i = 0; i < input.Devices.Count; i++)
            {
                if (input.Devices[i].Kind == kind)
                    input.Devices[i] = change(input.Devices[i]);
            }
        }

        [Test]
        public void ACleanStation_RaisesNothing()
        {
            Assert.IsEmpty(Analyze(Clean()));
            Assert.IsEmpty(Analyze(new PowerProblemInput()));
        }

        [Test]
        public void ApcOnNoCable()
        {
            PowerProblemInput input = Clean();
            input.Devices.Add(Device(KindApc, 6, 6, area: 9));

            SystemsViewIssue issue = Analyze(input).Single();
            Assert.AreEqual("apc-cable:6:6", issue.Id);
            Assert.AreEqual("Power", issue.Category);
            Assert.AreEqual("APC at (6, 6) is on no cable", issue.Title);
            StringAssert.Contains(PowerProblemAnalyzer.ApcNoCable.Words, issue.Body);
            Assert.AreEqual(new Vector3(6, 0, 6), issue.Position);
            Assert.AreEqual(new SystemsViewKey(new Vector2Int(6, 6), SystemsViewProblems.MarkerSlot(PowerProblemAnalyzer.CodeApcNoCable)),
                issue.Markers.Single());
        }

        [Test]
        public void CircuitWithNoGeneratorOrStorage_MarksEveryApcOnIt_ButAnEmptyCableIsFine()
        {
            PowerProblemInput input = Clean();
            input.Devices.RemoveAll(device => device.Kind is KindGenerator or KindSmes);

            SystemsViewIssue issue = Analyze(input).Single();
            Assert.AreEqual("circuit-source:4:0", issue.Id);
            Assert.AreEqual("No generator or SMES on the circuit of 2 APCs (first at (4, 0))", issue.Title);
            CollectionAssert.AreEqual(new[] { new Vector2Int(4, 0), new Vector2Int(8, 0) }, issue.Markers.Select(key => key.Tile));

            // An SMES alone is storage: enough.
            input.Devices.Add(Device(KindSmes, 2, 0, circuit: 1, supply: 50f));
            Assert.IsEmpty(Analyze(input));
        }

        [Test]
        public void DemandAboveSupply_ComparesWhatTheRoomsDrawWithRatedOutputPlusStorage()
        {
            PowerProblemInput input = Clean();
            Set(input, KindSmes, smes => { smes.SupplyKw = 0.5f; return smes; });
            Set(input, KindGenerator, generator => { generator.SupplyKw = 3f; return generator; });
            Assert.AreEqual(3.5f, PowerProblemAnalyzer.NominalSupplyKw(1, input.Devices), 1e-4f);
            Assert.AreEqual(4f, PowerProblemAnalyzer.NominalDemandKw(1, input.Devices), 1e-4f);

            SystemsViewIssue issue = Analyze(input).Single();
            Assert.AreEqual("circuit-demand:4:0", issue.Id);
            Assert.AreEqual("The circuit of 2 APCs (first at (4, 0)) needs more power than it gives", issue.Title);
            StringAssert.Contains(PowerProblemAnalyzer.OverDemand.Words, issue.Body);
            StringAssert.Contains("up to 4.0 kW against at most 3.5 kW", issue.Body);

            // Equal is enough.
            Set(input, KindGenerator, generator => { generator.SupplyKw = 3.5f; return generator; });
            Assert.IsEmpty(Analyze(input));
        }

        [Test]
        public void RoomWithMachinesButNoApc_KeepsMapChecksIdAndWording()
        {
            PowerProblemInput input = Clean();
            input.Devices.Add(Device(KindConsumer, 12, 3, area: 5, demand: 1f));
            input.Devices.Add(Device(KindLight, 11, 4, area: 5, demand: 1f));
            input.Rooms.Add(new PowerProblemRoom { AreaId = 5, Name = "Kitchen", HasApc = false });
            input.Rooms.Add(new PowerProblemRoom { AreaId = 6, Name = "Closet", HasApc = false });

            SystemsViewIssue issue = Analyze(input).Single();
            Assert.AreEqual("no-apc:5", issue.Id, "A room with nothing in it needs no APC");
            Assert.AreEqual("No APC in Kitchen (11, 4)", issue.Title, "Named by its first machine, west to east");
            Assert.AreEqual("No APC in Kitchen (11, 4), so nothing here gets local power. Place an APC in the room.", issue.Body);
            Assert.AreEqual(MapValidator.ApcEntry, issue.EntryKey);
            Assert.AreEqual(new Vector2Int(11, 4), issue.Markers.Single().Tile);
        }

        [Test]
        public void SmesOnNoCircuit()
        {
            PowerProblemInput input = Clean();
            input.Devices.Add(Device(KindSmes, 7, 7, supply: 50f));

            SystemsViewIssue issue = Analyze(input).Single();
            Assert.AreEqual("smes-circuit:7:7", issue.Id);
            Assert.AreEqual("SMES at (7, 7) is on no circuit", issue.Title);
        }

        [Test]
        public void NoSourceAndOverDemand_NeverBothFire()
        {
            PowerProblemInput input = Clean();
            input.Devices.RemoveAll(device => IsSupply(device.Kind));
            Assert.Greater(PowerProblemAnalyzer.NominalDemandKw(1, input.Devices), PowerProblemAnalyzer.NominalSupplyKw(1, input.Devices));
            Assert.AreEqual("circuit-source:4:0", Analyze(input).Single().Id, "No supply at all is the one problem, not also a shortfall");
        }

        [Test]
        public void TwoApcsOnOneWallOnNoCable_EachGetTheirOwnMarker()
        {
            PowerProblemInput input = new();
            input.Devices.Add(Device(KindApc, 3, 3));
            PowerProblemDevice back = Device(KindApc, 3, 3);
            back.Key = new SystemsViewKey(new Vector2Int(3, 3), DeviceSlot(SS3D.Systems.Tile.TileLayer.WallMountHigh, SS3D.Systems.Tile.Direction.South));
            input.Devices.Add(back);
            List<SystemsViewIssue> issues = Analyze(input);
            Assert.AreEqual(1, issues.Select(issue => issue.Id).Distinct().Count(), "Both share a tile, so one id names them");
            Assert.AreEqual(2, issues.SelectMany(issue => issue.Markers).Distinct().Count());
        }

        [Test]
        public void EveryBody_CarriesItsKindsLegendWords()
        {
            PowerProblemInput input = new();
            input.Devices.Add(Device(KindApc, 1, 5));
            input.Devices.Add(Device(KindApc, 2, 5, circuit: 1));
            input.Devices.Add(Device(KindApc, 3, 5, circuit: 2));
            input.Devices.Add(Device(KindGenerator, 4, 5, circuit: 2, supply: 1f));
            input.Devices.Add(Device(KindConsumer, 5, 5, circuit: 2, area: 6, demand: 5f));
            input.Devices.Add(Device(KindSmes, 6, 5));
            input.Rooms.Add(new PowerProblemRoom { AreaId = 6, Name = "Kitchen", HasApc = false });

            List<SystemsViewIssue> issues = Analyze(input);
            Assert.AreEqual(PowerProblemAnalyzer.Kinds.Length, issues.Count);
            foreach (SystemsViewIssue issue in issues)
            {
                SystemsViewProblems.TryGetKind(PowerProblemAnalyzer.Kinds, SystemsViewProblems.CodeOf(issue.Markers[0].Slot), out SystemsViewProblemKind kind);
                StringAssert.Contains(kind.Words, issue.Body, $"{issue.Id} says what the legend says");
            }
        }

        [Test]
        public void EveryProblemKind_HasALegendLine_InTheWordsMapCheckUses()
        {
            IReadOnlyList<SystemsViewLegendEntry> legend = PowerSystemsViewDomain.Legend;
            foreach (SystemsViewProblemKind kind in PowerProblemAnalyzer.Kinds)
                Assert.IsTrue(legend.Any(line => line.Text == kind.Sentence), $"The legend explains {kind.Name}");
            Assert.AreEqual(PowerProblemAnalyzer.Kinds.Length, PowerProblemAnalyzer.Kinds.Select(kind => kind.Code).Distinct().Count());
        }
    }
}
