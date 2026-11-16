using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Atmospherics.SystemsView;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.Validation;
using UnityEngine;
using static SS3D.Systems.Atmospherics.SystemsView.AtmosSystemsViewCodes;

namespace EditorTests.Atmospherics
{
    /// <summary>
    /// The Atmos view's problems (Documents/architecture/systems/map-editor-systems-views.md, Problems) on hand-built data:
    /// a clean station that raises nothing, then one fixture per v1 problem.
    /// </summary>
    public class AtmosProblemAnalyzerTests
    {
        private const ushort Bar = 3;

        // Row y = 0. Supply (network 1): pipes 0..2, a vent ending it at 0; a pump at 3 feeding it from Surface (network 2):
        // pipes 4..5, a chamber intake scrubber ending it at 5, the pump's inlet taking its gas. Both machines in the Bar,
        // which has power.
        private static AtmosProblemInput Clean()
        {
            AtmosProblemInput input = new();
            AddPipe(input, 0, Supply, SystemsViewLinks.East, 1);
            AddPipe(input, 1, Supply, SystemsViewLinks.East | SystemsViewLinks.West, 1);
            AddPipe(input, 2, Supply, SystemsViewLinks.East | SystemsViewLinks.West, 1);
            AddPipe(input, 4, Surface, SystemsViewLinks.East | SystemsViewLinks.West, 2);
            AddPipe(input, 5, Surface, SystemsViewLinks.West, 2);
            input.Devices.Add(Device(AtmosProblemDeviceKind.Vent, 0, back: 1, powered: true));
            input.Devices.Add(Device(AtmosProblemDeviceKind.Pump, 3, back: 2, front: 1, powered: true));
            input.Devices.Add(Device(AtmosProblemDeviceKind.Scrubber, 5, back: 2, powered: true));
            return input;
        }

        private static void AddPipe(AtmosProblemInput input, int x, byte line, byte links, int viewId, int y = 0) =>
            input.Pipes.Add(new AtmosProblemPipe { Tile = new Vector2Int(x, y), Line = line, Links = links, ViewId = viewId });

        private static AtmosProblemDevice Device(AtmosProblemDeviceKind kind, int x, int back = 0, int front = 0, int side = 0,
            bool hasPipe = true, bool powered = false, ushort area = Bar, bool roomPowered = true, int y = 0) => new()
        {
            Kind = kind,
            Tile = new Vector2Int(x, y),
            Position = new Vector3(x, 0, y),
            BackViewId = back,
            FrontViewId = front,
            SideViewId = side,
            HasPipe = hasPipe,
            NeedsPower = powered,
            AreaId = area,
            RoomName = "Bar",
            RoomPowered = roomPowered,
        };

        private static List<SystemsViewIssue> Analyze(AtmosProblemInput input) => AtmosProblemAnalyzer.Analyze(input);

        [Test]
        public void ACleanStation_RaisesNothing()
        {
            Assert.IsEmpty(Analyze(Clean()));
            Assert.IsEmpty(Analyze(new AtmosProblemInput()), "An empty map has nothing wrong with it");
        }

        [Test]
        public void OpenPipeEnd_IsAPipeJoinedOnOneSideOrNone_UnlessAVentScrubberMeterOrInjectorIsOnItsTile()
        {
            AtmosProblemInput input = Clean();
            AddPipe(input, 7, Waste, 0, 3, y: 4);
            AddPipe(input, 1, Mixed, SystemsViewLinks.North, 4, y: 4);
            input.Devices.Add(Device(AtmosProblemDeviceKind.Meter, 1, back: 4, y: 4));
            // Supply and Waste run side by side to a vent: its tile ends the Waste stub as well as its own Supply.
            AddPipe(input, 0, Waste, 0, 5);

            SystemsViewIssue issue = Analyze(input).Single();
            Assert.AreEqual("open-end:2:7:4", issue.Id);
            Assert.AreEqual("Atmos", issue.Category);
            Assert.AreEqual("Open Waste pipe end at (7, 4)", issue.Title);
            StringAssert.Contains(AtmosProblemAnalyzer.OpenEnd.Words, issue.Body, "Map Check's text uses the legend's words");
            Assert.AreEqual(new Vector3(7, 0, 4), issue.Position);
            CollectionAssert.AreEqual(new[] { new SystemsViewKey(new Vector2Int(7, 4), SystemsViewProblems.MarkerSlot(AtmosProblemAnalyzer.CodeOpenEnd, Waste)) },
                issue.Markers);

            // The same stub on a tile with no device: open.
            AddPipe(input, 9, Waste, 0, 6);
            Assert.AreEqual(2, Analyze(input).Count(found => found.Id.StartsWith("open-end")));
        }

        [Test]
        public void DeviceOnNoPipe_KeepsMapChecksIdsAndWording()
        {
            AtmosProblemInput input = new();
            input.Devices.Add(Device(AtmosProblemDeviceKind.Vent, 1, hasPipe: false, y: 2));
            input.Devices.Add(Device(AtmosProblemDeviceKind.Pump, 2, hasPipe: false, y: 2));
            input.Devices.Add(Device(AtmosProblemDeviceKind.Valve, 3, hasPipe: false, y: 2));
            input.Devices.Add(Device(AtmosProblemDeviceKind.Filter, 4, hasPipe: false, y: 2));
            input.Devices.Add(Device(AtmosProblemDeviceKind.Meter, 5, hasPipe: false, y: 2));
            input.Devices.Add(Device(AtmosProblemDeviceKind.LineFill, 6, hasPipe: false, y: 2));
            input.Devices.Add(Device(AtmosProblemDeviceKind.Scrubber, 7, hasPipe: false, y: 2));

            Dictionary<string, SystemsViewIssue> issues = Analyze(input).ToDictionary(issue => issue.Id);
            Assert.AreEqual(7, issues.Count, "Each device on no pipe once, and nothing else (no network to judge)");
            Assert.AreEqual("Vent at (1, 2) has no Supply or Mixed pipe on its tile", issues["pipe:Vent:1:2"].Title);
            Assert.AreEqual("Vent at (1, 2) has no Supply or Mixed pipe on its tile, so it does nothing until it is on a pipe it can use. Run one under it.",
                issues["pipe:Vent:1:2"].Body, "The title from before Phase 4, then the legend's words, then the fix");
            Assert.AreEqual("Gas pump at (2, 2) needs a Surface pipe at both ports", issues["pipe:GasPump:2:2"].Title);
            Assert.AreEqual("Valve at (3, 2) needs a Surface pipe at both ports", issues["pipe:Valve:3:2"].Title);
            Assert.AreEqual("Filter or mixer at (4, 2) can't tell which pipe line it's on", issues["pipe:ThreeWay:4:2"].Title);
            Assert.AreEqual("Meter or injector at (5, 2) has no gas pipe on its tile", issues["pipe:LineReader:5:2"].Title);
            StringAssert.Contains(AtmosProblemAnalyzer.NoPipe.Words, issues["pipe:LineReader:5:2"].Body);
            Assert.AreEqual("Line fill at (6, 2) has no matching pipe on its tile", issues["pipe:LineFill:6:2"].Title);
            Assert.AreEqual("Scrubber at (7, 2) has no Waste or Mixed pipe on its tile", issues["pipe:Scrubber:7:2"].Title);
            Assert.AreEqual(new SystemsViewKey(new Vector2Int(1, 2), SystemsViewProblems.MarkerSlot(AtmosProblemAnalyzer.CodeNoPipe)),
                issues["pipe:Vent:1:2"].Markers.Single());
        }

        [Test]
        public void VentWithNothingFeedingIt_OneIssuePerNetwork_MarkingEveryVent()
        {
            AtmosProblemInput input = Clean();
            input.Devices.RemoveAll(device => device.Kind == AtmosProblemDeviceKind.Pump);
            input.Devices.Add(Device(AtmosProblemDeviceKind.Vent, 2, back: 1));

            List<SystemsViewIssue> issues = Analyze(input);
            SystemsViewIssue unfed = issues.Single(issue => issue.Id.StartsWith("no-source"));
            Assert.AreEqual("no-source:0:0", unfed.Id, "Named by its first vent, west to east");
            Assert.AreEqual("Nothing feeds 2 vents on one network (first at (0, 0))", unfed.Title);
            Assert.AreEqual(2, unfed.Markers.Length);
            Assert.AreEqual(AtmosProblemAnalyzer.CodeNoSource, SystemsViewProblems.CodeOf(unfed.Markers[0].Slot));
            Assert.AreEqual(1, issues.Count(issue => issue.Id.StartsWith("no-sink")), "The intake lost its outlet too");

            // A valve, a line fill or a filter's side each feed it.
            foreach (AtmosProblemDevice feeder in new[]
                     {
                         Device(AtmosProblemDeviceKind.Valve, 9, back: 5, front: 1),
                         Device(AtmosProblemDeviceKind.LineFill, 1, back: 1),
                         Device(AtmosProblemDeviceKind.Filter, 9, back: 5, front: 6, side: 1),
                     })
            {
                AtmosProblemInput fed = Clean();
                fed.Devices.RemoveAll(device => device.Kind == AtmosProblemDeviceKind.Pump);
                fed.Devices.Add(feeder);
                Assert.IsFalse(Analyze(fed).Any(issue => issue.Id.StartsWith("no-source")), $"{feeder.Kind} feeds the network");
            }
        }

        [Test]
        public void ScrubberWithNowhereToSendGas_ButAPumpInletValveInjectorOrVentTakesIt()
        {
            AtmosProblemInput input = Clean();
            input.Devices.RemoveAll(device => device.Kind == AtmosProblemDeviceKind.Pump);
            input.Devices.RemoveAll(device => device.Kind == AtmosProblemDeviceKind.Vent);
            input.Pipes.RemoveAll(pipe => pipe.Line == Supply);

            SystemsViewIssue issue = Analyze(input).Single();
            Assert.AreEqual("no-sink:5:0", issue.Id);
            Assert.AreEqual("Scrubber at (5, 0) has nowhere to send gas", issue.Title);
            StringAssert.Contains(AtmosProblemAnalyzer.NoSink.Words, issue.Body);

            foreach (AtmosProblemDevice outlet in new[]
                     {
                         Device(AtmosProblemDeviceKind.Pump, 9, back: 2, front: 7),
                         Device(AtmosProblemDeviceKind.Valve, 9, back: 7, front: 2),
                         Device(AtmosProblemDeviceKind.Injector, 4, back: 2),
                         Device(AtmosProblemDeviceKind.Vent, 4, back: 2),
                         Device(AtmosProblemDeviceKind.Mixer, 9, back: 7, front: 8, side: 2),
                     })
            {
                AtmosProblemInput drained = new();
                drained.Pipes.AddRange(input.Pipes);
                drained.Devices.AddRange(input.Devices);
                drained.Devices.Add(outlet);
                Assert.IsFalse(Analyze(drained).Any(found => found.Id.StartsWith("no-sink")), $"{outlet.Kind} takes the gas");
            }
        }

        [Test]
        public void ChamberIntake_IsNamedAsItselfNotAsAScrubber()
        {
            AtmosProblemInput input = Clean();
            input.Devices.RemoveAll(device => device.Kind == AtmosProblemDeviceKind.Pump);
            input.Devices.RemoveAll(device => device.Kind == AtmosProblemDeviceKind.Vent);
            input.Pipes.RemoveAll(pipe => pipe.Line == Supply);
            for (int i = 0; i < input.Devices.Count; i++)
            {
                AtmosProblemDevice device = input.Devices[i];
                if (device.Kind == AtmosProblemDeviceKind.Scrubber)
                    device.Kind = AtmosProblemDeviceKind.Intake;
                input.Devices[i] = device;
            }

            SystemsViewIssue issue = Analyze(input).Single();
            Assert.AreEqual("Chamber intake at (5, 0) has nowhere to send gas", issue.Title);
            Assert.IsTrue(AtmosProblemAnalyzer.EndsPipes(AtmosProblemDeviceKind.Intake));
        }

        [Test]
        public void ChamberIntakeBadge_HasItsOwnNameAndStateWords()
        {
            Assert.AreEqual("Chamber intake", KindName(
                KindIntake));
            Assert.AreEqual("on, drawing air", StateWords(
                KindIntake,
                State(On)));
        }

        [Test]
        public void DeviceInARoomWithNoPower_OneIssuePerRoom_OnlyForDevicesThatNeedPower()
        {
            AtmosProblemInput input = Clean();
            for (int i = 0; i < input.Devices.Count; i++)
            {
                AtmosProblemDevice device = input.Devices[i];
                device.RoomPowered = false;
                device.NeedsPower = device.Kind != AtmosProblemDeviceKind.Pump;
                input.Devices[i] = device;
            }

            input.Devices.Add(Device(AtmosProblemDeviceKind.Meter, 4, back: 2, powered: true, area: 0, roomPowered: false));
            SystemsViewIssue issue = Analyze(input).Single();
            Assert.AreEqual("atmos-power:3", issue.Id);
            Assert.AreEqual("1 vent and 1 scrubber in Bar (0, 0) have no power", issue.Title);
            Assert.AreEqual(MapValidator.ApcEntry, issue.EntryKey, "Show in palette finds the APC");
            CollectionAssert.AreEqual(new[] { new Vector2Int(0, 0), new Vector2Int(5, 0) }, issue.Markers.Select(key => key.Tile),
                "A marker on each, none on the unpowered pump or the meter outside every room");
        }

        [Test]
        public void TwoDevicesOnOneTileOnNoPipe_EachGetTheirOwnMarker()
        {
            AtmosProblemInput input = new();
            input.Devices.Add(Device(AtmosProblemDeviceKind.Vent, 1, hasPipe: false));
            AtmosProblemDevice meter = Device(AtmosProblemDeviceKind.Meter, 1, hasPipe: false);
            meter.Slot = 40;
            input.Devices.Add(meter);

            List<SystemsViewIssue> issues = Analyze(input);
            Assert.AreEqual(2, issues.Count);
            Assert.AreEqual(2, issues.Select(issue => issue.Markers.Single()).Distinct().Count(), "Each issue has a marker key of its own");
            Assert.IsTrue(issues.All(issue => SystemsViewProblems.CodeOf(issue.Markers[0].Slot) == AtmosProblemAnalyzer.CodeNoPipe));
        }

        [Test]
        public void ALineFill_EndsTheStubItSitsOn()
        {
            AtmosProblemInput input = Clean();
            AddPipe(input, 8, Mixed, SystemsViewLinks.East, 7);
            AddPipe(input, 9, Mixed, SystemsViewLinks.West, 7);
            input.Devices.Add(Device(AtmosProblemDeviceKind.LineFill, 8, back: 7));
            Assert.AreEqual("open-end:1:9:0", Analyze(input).Single().Id, "The fill's own tile ends the run; the far end is open");
        }

        [Test]
        public void EveryBody_CarriesItsKindsLegendWords()
        {
            AtmosProblemInput input = new();
            AddPipe(input, 9, Waste, 0, 3, y: 9);
            input.Devices.Add(Device(AtmosProblemDeviceKind.Pump, 1, hasPipe: false));
            input.Devices.Add(Device(AtmosProblemDeviceKind.Vent, 2, back: 1, powered: true, roomPowered: false));
            input.Devices.Add(Device(AtmosProblemDeviceKind.Vent, 2, back: 1, y: 1));
            input.Devices.Add(Device(AtmosProblemDeviceKind.Scrubber, 3, back: 2, powered: true, roomPowered: false));
            input.Devices.Add(Device(AtmosProblemDeviceKind.Scrubber, 3, back: 2, y: 1));

            List<SystemsViewIssue> issues = Analyze(input);
            Assert.AreEqual(AtmosProblemAnalyzer.Kinds.Length, issues.Count, "One of each kind, plural where it can be");
            foreach (SystemsViewIssue issue in issues)
            {
                SystemsViewProblems.TryGetKind(AtmosProblemAnalyzer.Kinds, SystemsViewProblems.CodeOf(issue.Markers[0].Slot), out SystemsViewProblemKind kind);
                StringAssert.Contains(kind.Words, issue.Body, $"{issue.Id} says what the legend says");
            }
        }

        [Test]
        public void EveryProblemKind_HasALegendLine_InTheWordsMapCheckUses()
        {
            IReadOnlyList<SystemsViewLegendEntry> legend = AtmosSystemsViewDomain.Legend;
            foreach (SystemsViewProblemKind kind in AtmosProblemAnalyzer.Kinds)
                Assert.IsTrue(legend.Any(line => line.Text == kind.Sentence), $"The legend explains {kind.Name}");
            Assert.AreEqual(AtmosProblemAnalyzer.Kinds.Length, AtmosProblemAnalyzer.Kinds.Select(kind => kind.Code).Distinct().Count());
        }
    }
}
