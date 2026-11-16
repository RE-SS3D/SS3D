using System;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Atmospherics.SystemsView;
using SS3D.Systems.Electricity.SystemsView;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.SpawnPoints;
using SS3D.Systems.Tile.Validation;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.MapEditor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EditorTests
{
    public class MapValidationTests
    {
        private static SpawnPointRecord Job(string name) =>
            SpawnPointRecord.ForJob(name, Vector3.zero, Direction.North);

        private static SpawnPointRecord Operative => SpawnPointRecord.ForAntagonist(
            AntagonistSpawnCategory.NuclearOperatives, Vector3.one, Direction.North);

        [Test]
        public void ExtendedRequiresEveryOfferedJobEvenWithNoReadyPlayers()
        {
            MapValidationReport report = MapValidator.Validate("Extended", new[] { "Captain", "Assistant", "Captain" },
                new[] { Job("Assistant") }, false, false);
            Assert.IsFalse(report.CanStart);
            Assert.AreEqual(1, report.Errors.Count);
            StringAssert.Contains("Captain", report.Errors[0]);
        }

        [Test]
        public void ExtendedDoesNotRequireNuclearContent()
        {
            Assert.IsTrue(MapValidator.Validate("Extended", new[] { "Assistant" },
                new[] { Job("Assistant") }, false, false).CanStart);
        }

        [TestCase("Secret")]
        [TestCase("Nuclear Emergency")]
        public void NuclearModesListAllMissingRequirementsTogether(string mode)
        {
            MapValidationReport report = MapValidator.Validate(mode, new[] { "Assistant", "Captain" },
                Array.Empty<SpawnPointRecord>(), false, false);
            Assert.AreEqual(5, report.Errors.Count);
            Assert.IsFalse(report.CanStart);
            string message = report.Describe("Test station", mode, saved: false);
            foreach (string expected in new[] { "Assistant", "Captain", "Nuclear Operatives", "device", "disk", "save" })
                StringAssert.Contains(expected, message);
        }

        [TestCase(true, true, true, 0)]
        [TestCase(false, true, true, 1)]
        [TestCase(true, false, true, 1)]
        [TestCase(true, true, false, 1)]
        public void NuclearRequirementsAreIndependent(bool nuke, bool disk, bool operative, int errors)
        {
            SpawnPointRecord[] spawns = operative ? new[] { Job("Assistant"), Operative } : new[] { Job("Assistant") };
            MapValidationReport report = MapValidator.Validate("Nuclear Emergency", new[] { "Assistant" }, spawns, nuke, disk);
            Assert.AreEqual(errors, report.Errors.Count);
            Assert.AreEqual(errors == 0, report.CanStart);
        }

        [Test]
        public void WrongJobAndAntagonistTagsDoNotSatisfyRequirements()
        {
            MapValidationReport report = MapValidator.Validate("Nuclear Emergency", new[] { "Assistant" },
                new[] { Job("assistant"), SpawnPointRecord.ForAntagonist(AntagonistSpawnCategory.Traitor,
                    Vector3.one, Direction.North) }, true, true);
            Assert.AreEqual(2, report.Errors.Count);
            Assert.AreEqual(1, report.Warnings.Count);
        }

        [Test]
        public void UnknownJobWarningDoesNotBlockStart()
        {
            MapValidationReport report = MapValidator.Validate("Extended", new[] { "Assistant" },
                new[] { Job("Assistant"), Job("RetiredJob") }, false, false);
            Assert.IsTrue(report.CanStart);
            Assert.IsTrue(report.HasIssues);
            StringAssert.Contains("RetiredJob", report.Warnings.Single());
            StringAssert.Contains("Saved", report.Describe("draft", "Extended", saved: true));
        }

        [Test]
        public void UnconfiguredJobsAndUnknownModesFailClosed()
        {
            Assert.IsFalse(MapValidator.Validate("Extended", Array.Empty<string>(),
                new[] { Job("Assistant") }, true, true).CanStart);
            Assert.IsFalse(MapValidator.Validate("FutureMode", new[] { "Assistant" },
                new[] { Job("Assistant"), Operative }, true, true).CanStart);
        }

        [Test]
        public void RevalidationAfterRepairDoesNotRetainOldErrors()
        {
            Assert.IsFalse(MapValidator.Validate("Extended", new[] { "Assistant" },
                Array.Empty<SpawnPointRecord>(), false, false).CanStart);
            MapValidationReport repaired = MapValidator.Validate("Extended", new[] { "Assistant" },
                new[] { Job("Assistant") }, false, false);
            Assert.IsTrue(repaired.CanStart);
            Assert.IsFalse(repaired.HasIssues);
        }

        [Test]
        public void MissingMapIsAnExplainedFailure()
        {
            MapValidationReport report = MapValidationService.Validate(null, "Extended");
            Assert.IsFalse(report.CanStart);
            StringAssert.Contains("No station map", report.Errors.Single());
        }

        [Test]
        public void EditorChecklistsIncludeMetAndMissingRequirementsWithoutChangingPolicy()
        {
            MapValidationReport report = MapValidator.Validate("Nuclear Emergency", new[] { "Assistant", "Captain" },
                new[] { Job("Assistant"), Operative }, false, true);
            Assert.AreEqual(2, report.Errors.Count);
            Assert.AreEqual(2, report.Issues.Count);
            MapValidationIssue jobs = report.Issues.Single(issue => issue.Id == "job-spawns");
            Assert.IsTrue(jobs.Checks.Single(check => check.Label == "Assistant").IsMet);
            Assert.IsFalse(jobs.Checks.Single(check => check.Label == "Captain").IsMet);
            MapValidationIssue nuclear = report.Issues.Single(issue => issue.Id == "nuclear");
            Assert.AreEqual(2, nuclear.Checks.Count(check => check.IsMet));
            Assert.AreEqual("Nuclear device", nuclear.Checks.Single(check => !check.IsMet).Label);
        }

        private static RoomDeviceRecord Device(RoomDeviceKind kind, float x, ushort area, string room = "Bar") =>
            new(kind, new Vector3(x, 0, 2), area, room);

        private static RoomDeviceRecord Vent(float x, ushort area, string room = "Bar") => Device(RoomDeviceKind.Vent, x, area, room);

        private static RoomDeviceRecord Scrubber(float x, ushort area, string room = "Bar") => Device(RoomDeviceKind.Scrubber, x, area, room);

        private static RoomDeviceRecord Alarm(float x, ushort area, string room = "Bar") => Device(RoomDeviceKind.AirAlarm, x, area, room);

        private static MapValidationReport RoomChecks(params RoomDeviceRecord[] devices)
        {
            MapValidationReport report = new();
            MapValidator.AddRoomDeviceChecks(report, devices, paintedRooms: false);
            return report;
        }

        [Test]
        public void CompleteRoomRaisesNoWarning()
        {
            Assert.IsFalse(RoomChecks(Vent(1, 3), Scrubber(2, 3), Alarm(3, 3)).HasIssues);
        }

        [Test]
        public void RoomWithoutAnAirAlarmWarnsOnceAndLocatesADevice()
        {
            MapValidationReport report = RoomChecks(Vent(4, 3), Vent(1, 3), Scrubber(2, 3));
            MapValidationIssue issue = report.Issues.Single();
            Assert.IsTrue(issue.IsWarning);
            Assert.IsTrue(report.CanStart);
            Assert.AreEqual("No air alarm in Bar (1, 2)", issue.Title);
            StringAssert.Contains("2 vents and 1 scrubber", issue.Body);
            Assert.IsTrue(issue.HasPosition);
            Assert.AreEqual(1f, issue.Position.x);
        }

        [Test]
        public void RoomsSharingANameGetTitlesThatTellThemApart()
        {
            MapValidationReport report = RoomChecks(
                Vent(1, 3, "APC"), Scrubber(2, 3, "APC"), Vent(8, 4, "APC"), Scrubber(9, 4, "APC"));
            Assert.AreEqual(2, report.Issues.Select(issue => issue.Title).Distinct().Count());
        }

        [Test]
        public void AnAirAlarmOnlyCoversItsOwnRoom()
        {
            MapValidationReport report = RoomChecks(Vent(1, 3), Scrubber(2, 3), Alarm(3, 3),
                Vent(5, 4, "Kitchen"), Scrubber(6, 4, "Kitchen"));
            StringAssert.StartsWith("No air alarm in Kitchen", report.Issues.Single().Title);
        }

        [Test]
        public void DevicesOutsideEveryRoomShareOneRowThatNamesTheFixForTheMapKind()
        {
            MapValidationReport report = RoomChecks(Vent(2, 0, null), Vent(1, 0, null), Scrubber(3, 0, null),
                Alarm(4, 3), Vent(5, 3), Scrubber(6, 3));
            MapValidationIssue issue = report.Issues.Single();
            Assert.IsTrue(report.CanStart);
            Assert.AreEqual("2 vents and 1 scrubber outside every room", issue.Title);
            Assert.AreEqual(1f, issue.Position.x);
            StringAssert.Contains("Add an APC", issue.Body);

            MapValidationReport painted = new();
            MapValidator.AddRoomDeviceChecks(painted, new[] { Vent(1, 0, null) }, paintedRooms: true);
            StringAssert.DoesNotContain("Add an APC", painted.Issues.Single().Body);
        }

        [Test]
        public void AirAlarmFacingNoRoomWarnsEvenThoughItHasNoDevices()
        {
            MapValidationIssue issue = RoomChecks(Alarm(1, 0, null)).Issues.Single();
            StringAssert.Contains("Air alarm at (1, 2) faces no room", issue.Title);
            Assert.IsTrue(issue.HasPosition);
        }

        // A device on no pipe and a room with no APC are the systems views' problems since Phase 4; their tests are
        // AtmosProblemAnalyzerTests and PowerProblemAnalyzerTests. Here: Map Check carries them as warnings with Locate.
        [Test]
        public void SystemsIssues_AreWarningsWithLocate_UnderTheAnalyzersIds_AndNeverBlockARound()
        {
            AtmosProblemInput atmos = new();
            atmos.Pipes.Add(new AtmosProblemPipe { Tile = new Vector2Int(5, 2), Line = 0, Links = 0, ViewId = 1 });
            atmos.Devices.Add(new AtmosProblemDevice { Kind = AtmosProblemDeviceKind.Vent, Tile = new Vector2Int(1, 2),
                Position = new Vector3(1, 0, 2), HasPipe = false });
            PowerProblemInput power = new();
            power.Devices.Add(new PowerProblemDevice { Kind = PowerSystemsViewCodes.KindApc, Key = new SystemsViewKey(new Vector2Int(7, 2)),
                Position = new Vector3(7, 0, 2), AreaId = 3 });
            power.Rooms.Add(new PowerProblemRoom { AreaId = 3, Name = "Bar", HasApc = true });

            MapValidationReport report = MapValidator.Validate("Extended", new[] { "Assistant" }, new[] { Job("Assistant") }, false, false);
            Assert.IsFalse(report.HasIssues);
            MapValidator.AddSystemsIssues(report, AtmosProblemAnalyzer.Analyze(atmos).Concat(PowerProblemAnalyzer.Analyze(power)));

            CollectionAssert.AreEquivalent(new[] { "open-end:0:5:2", "pipe:Vent:1:2", "apc-cable:7:2" }, report.Issues.Select(issue => issue.Id));
            Assert.IsTrue(report.CanStart, "A systems problem is a warning, never a round-start block");
            Assert.IsTrue(report.Issues.All(issue => issue.IsWarning && issue.HasPosition));
            MapValidationIssue vent = report.Issues.Single(issue => issue.Id == "pipe:Vent:1:2");
            Assert.AreEqual("Atmos", vent.Category);
            Assert.AreEqual("Vent at (1, 2) has no Supply or Mixed pipe on its tile", vent.Title, "Map Check's wording from before Phase 4");
            Assert.AreEqual(new Vector3(1, 0, 2), vent.Position, "Locate goes to the device");
            Assert.AreEqual("Power", report.Issues.Single(issue => issue.Id == "apc-cable:7:2").Category);

            // A second pass (Map Check re-run into the same report) adds nothing twice.
            MapValidator.AddSystemsIssues(report, AtmosProblemAnalyzer.Analyze(atmos));
            Assert.AreEqual(3, report.Issues.Count);
        }

        [Test]
        public void SystemsIssueIds_CoverEveryV1Problem()
        {
            // One input that raises every problem kind once, so a renamed id fails here as well as in the analyzer tests.
            AtmosProblemInput atmos = new();
            atmos.Pipes.Add(new AtmosProblemPipe { Tile = new Vector2Int(9, 9), Line = 3, Links = SystemsViewLinks.East, ViewId = 3 });
            atmos.Devices.Add(new AtmosProblemDevice { Kind = AtmosProblemDeviceKind.Pump, Tile = new Vector2Int(1, 1), HasPipe = false });
            atmos.Devices.Add(new AtmosProblemDevice { Kind = AtmosProblemDeviceKind.Vent, Tile = new Vector2Int(2, 1), HasPipe = true, BackViewId = 1,
                NeedsPower = true, AreaId = 4, RoomName = "Bar" });
            atmos.Devices.Add(new AtmosProblemDevice { Kind = AtmosProblemDeviceKind.Scrubber, Tile = new Vector2Int(3, 1), HasPipe = true, BackViewId = 2 });
            PowerProblemInput power = new();
            power.Devices.Add(new PowerProblemDevice { Kind = PowerSystemsViewCodes.KindApc, Key = new SystemsViewKey(new Vector2Int(1, 5)) });
            power.Devices.Add(new PowerProblemDevice { Kind = PowerSystemsViewCodes.KindApc, Key = new SystemsViewKey(new Vector2Int(2, 5)), CircuitViewId = 1 });
            power.Devices.Add(new PowerProblemDevice { Kind = PowerSystemsViewCodes.KindApc, Key = new SystemsViewKey(new Vector2Int(3, 5)), CircuitViewId = 2 });
            power.Devices.Add(new PowerProblemDevice { Kind = PowerSystemsViewCodes.KindGenerator, Key = new SystemsViewKey(new Vector2Int(4, 5)),
                CircuitViewId = 2, SupplyKw = 1f });
            power.Devices.Add(new PowerProblemDevice { Kind = PowerSystemsViewCodes.KindConsumer, Key = new SystemsViewKey(new Vector2Int(5, 5)),
                CircuitViewId = 2, AreaId = 6, DemandKw = 5f });
            power.Devices.Add(new PowerProblemDevice { Kind = PowerSystemsViewCodes.KindSmes, Key = new SystemsViewKey(new Vector2Int(6, 5)) });
            power.Rooms.Add(new PowerProblemRoom { AreaId = 6, Name = "Kitchen", HasApc = false });

            MapValidationReport report = new();
            MapValidator.AddSystemsIssues(report, AtmosProblemAnalyzer.Analyze(atmos).Concat(PowerProblemAnalyzer.Analyze(power)));
            CollectionAssert.AreEquivalent(new[]
            {
                "open-end:3:9:9", "pipe:GasPump:1:1", "no-source:2:1", "no-sink:3:1", "atmos-power:4",
                "apc-cable:1:5", "circuit-source:2:5", "circuit-demand:3:5", "no-apc:6", "smes-circuit:6:5",
            }, report.Issues.Select(issue => issue.Id));
            Assert.IsTrue(report.CanStart);
        }

        [Test]
        public void RoomMissingOneHalfOfItsAirLoopNamesWhatIsMissing()
        {
            StringAssert.StartsWith("No scrubber in Bar", RoomChecks(Vent(1, 3), Alarm(2, 3)).Issues.Single().Title);
            StringAssert.StartsWith("No vent in Bar", RoomChecks(Scrubber(1, 3), Alarm(2, 3)).Issues.Single().Title);
        }

        [Test]
        public void RoomWithTwoApcsWarnsAndNamesTheExtraApc()
        {
            MapValidationIssue issue = RoomChecks(Device(RoomDeviceKind.Apc, 1, 3), Device(RoomDeviceKind.Apc, 4, 3),
                Device(RoomDeviceKind.Apc, 7, 5, "Kitchen")).Issues.Single();
            Assert.AreEqual("apc-shared:3", issue.Id);
            StringAssert.StartsWith("2 APCs in Bar", issue.Title);
            StringAssert.Contains("APC at (4, 2)", issue.Body);
            Assert.AreEqual(new Vector3(4, 0, 2), issue.Position);
        }

        [Test]
        public void MapCheckFiltersIgnoresWarningsAndRestoresWithoutChangingServerPolicy()
        {
            MapValidationReport report = MapValidator.Validate("Extended", new[] { "Captain" },
                new[] { Job("RetiredJob") }, false, false);
            MapEditorViewModel model = new();
            model.SetValidation(report.Issues.ToArray(), "Extended");
            MapEditorView view = new(null, null, model, new MapEditorCatalog(), null);
            // Buttons only react to submit events inside a live panel, so host the view in a window.
            EditorWindow window = ScriptableObject.CreateInstance<EditorWindow>();
            window.Show();
            try
            {
                VisualElement root = window.rootVisualElement;
                view.Build(root);
                Assert.IsNotNull(root.Q<ScrollView>("map-check-issues"));
                Click(root, "map-check-filter-warning");
                Assert.IsNull(root.Q("map-check-issue-job-spawns"));
                Click(root, "map-check-toggle-unavailable:RetiredJob");
                Click(root, "map-check-ignore-unavailable:RetiredJob");
                Assert.IsNull(root.Q("map-check-issue-unavailable:RetiredJob"));
                Assert.IsFalse(report.CanStart);
                Assert.AreEqual(1, model.ValidationIssues.Count(issue => issue.IsWarning));
                // Dismissal survives a re-check but never crosses into another map.
                model.SetValidation(report.Issues.ToArray(), "Extended");
                Assert.IsNull(root.Q("map-check-issue-unavailable:RetiredJob"));
                Click(root, "map-check-unignore");
                Assert.IsNotNull(root.Q("map-check-issue-unavailable:RetiredJob"));
                Click(root, "map-check-ignore-unavailable:RetiredJob");
                model.SetValidation(report.Issues.ToArray(), "Extended", resetDismissed: true);
                Assert.IsNotNull(root.Q("map-check-issue-unavailable:RetiredJob"));
            }
            finally { view.Destroy(); window.Close(); }
        }

        [Test]
        public void MapCheckUsesLiteralTextAndShowsCheckingWhileStale()
        {
            Vector3 position = new(4, 0, 9);
            MapEditorViewModel model = new();
            model.SetValidation(new[] { new MapValidationIssue { Id = "test", IsWarning = true,
                Title = "<b>draft</b>", Body = "Literal <i>body</i>", Category = "Jobs",
                HasPosition = true, Position = position } }, "Extended");
            MapEditorView view = new(null, null, model, new MapEditorCatalog(), null);
            Vector3 located = Vector3.zero;
            view.ValidationLocateRequested += issue => located = issue.Position;
            // Buttons only react to submit events inside a live panel, so host the view in a window.
            EditorWindow window = ScriptableObject.CreateInstance<EditorWindow>();
            window.Show();
            try
            {
                VisualElement root = window.rootVisualElement;
                view.Build(root);
                Click(root, "map-check-toggle-test");
                foreach (Label label in root.Query<Label>().ToList())
                    if (label.text.Contains("<")) Assert.IsFalse(label.enableRichText);
                Assert.AreEqual("Literal <i>body</i>", root.Q<Label>(className: "map-check__body").text);
                Click(root, "map-check-locate-test");
                Assert.AreEqual(position, located);
                Assert.IsNull(root.Q<Button>("map-check-recheck"));
                model.SetValidation(Array.Empty<MapValidationIssue>(), "Extended");
                Assert.IsNull(root.Q("map-check-issue-test"));
                Assert.AreEqual("Map is playable.", root.Q<Label>(className: "map-check__status").text);
                Assert.IsNotNull(root.Q<Label>(className: "map-check__success"));
                model.ValidationStale = true;
                model.NotifyChanged();
                Assert.AreEqual("Checking the map...", root.Q<Label>(className: "map-check__status").text);
            }
            finally { view.Destroy(); window.Close(); }
        }

        private static void Click(VisualElement root, string name)
        {
            Button button = root.Q<Button>(name);
            Assert.IsNotNull(button, name);
            using NavigationSubmitEvent evt = NavigationSubmitEvent.GetPooled();
            evt.target = button;
            button.SendEvent(evt);
        }
    }
}
