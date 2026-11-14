using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Roles;
using SS3D.UI.Lobby;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Lobby effort Phase 6: the Join picker's rules. Only open jobs are offered, in roster order, with open/total counts;
    /// the choice starts on the character's best-ranked open job and moves, with a notice, when that job fills; the
    /// "only Assistant open" state; and the crew manifest grouped by department with the open gaps.
    /// </summary>
    public sealed class LobbyJoinModelTests
    {
        private const string Fallback = "Assistant";

        private static JobSlotInfo Job(string name, Department department, int slots, int assigned = 0) =>
            new() { Name = name, Department = (byte)department, Slots = slots, Assigned = assigned };

        private static List<JobSlotInfo> Station(int doctors = 0, int security = 2) => new()
        {
            Job("Assistant", Department.Civilian, 0),
            Job("Security", Department.Security, 2, security),
            Job("Doctor", Department.Medical, 3, doctors),
            Job("Captain", Department.Command, 1, 1),
            Job("Engineer", Department.Engineering, 2, 0),
        };

        private static CharacterPreferences Preferences(params (string job, JobPriority priority)[] ranks)
        {
            CharacterPreferences preferences = new();
            foreach ((string job, JobPriority priority) in ranks) preferences.SetPriority(job, priority);
            return preferences;
        }

        private static CrewManifestEntry Crew(string name, string job) => new() { Name = name, Job = job };

        [Test]
        public void OpenJobs_LeaveOutFullJobs_KeepTheFallback_InRosterOrder()
        {
            LobbyJoinModel model = new();
            model.Set(Station(), Fallback, null);
            CollectionAssert.AreEqual(new[] { "Engineer", "Doctor", "Assistant" }, model.OpenJobs.Select(job => job.Name).ToArray(),
                "Captain and Security are full; the departments run in roster order and Assistant is never capped");
            Assert.AreEqual("3 open", model.OpenCountText);
            Assert.AreEqual("3/3", model.CountText(model.OpenJobs[1]), "Open and total slots");
            Assert.AreEqual("", model.CountText(model.OpenJobs[2]), "The fallback reads unlimited");
        }

        [Test]
        public void Choice_StartsOnTheBestRankedOpenJob()
        {
            LobbyJoinModel model = new();
            model.Set(Station(), Fallback, Preferences(("Security", JobPriority.High), ("Doctor", JobPriority.Medium)));
            Assert.AreEqual("Doctor", model.Selected, "High Security is full, so the Medium Doctor is chosen");
            Assert.AreEqual("JOIN AS DOCTOR", model.JoinTitle.ToUpperInvariant());

            model.Set(Station(), Fallback, Preferences());
            Assert.AreEqual("Engineer", model.Selected, "No open preference: the first open job");
        }

        [Test]
        public void ChosenJobThatFills_MovesTheChoice_AndSaysSo()
        {
            LobbyJoinModel model = new();
            model.Set(Station(), Fallback, null);
            Assert.IsTrue(model.Select("Doctor"));
            Assert.AreEqual(JoinNoticeKind.None, model.Notice);

            model.Set(Station(doctors: 3), Fallback, null);
            Assert.AreEqual("Engineer", model.Selected, "The choice moved to the first open job");
            Assert.AreEqual(JoinNoticeKind.ChosenFilled, model.Notice);
            StringAssert.Contains("Doctor filled up", model.NoticeText);

            model.Select("Assistant");
            Assert.AreEqual(JoinNoticeKind.None, model.Notice, "Picking again clears the notice");
            Assert.IsFalse(model.Select("Doctor"), "A full job can't be picked");
        }

        [Test]
        public void PlayerPick_SurvivesCountChanges()
        {
            LobbyJoinModel model = new();
            CharacterPreferences preferences = Preferences(("Engineer", JobPriority.High));
            model.Set(Station(), Fallback, preferences);
            model.Select("Assistant");
            model.Set(Station(doctors: 1), Fallback, preferences);
            Assert.AreEqual("Assistant", model.Selected, "A refresh doesn't undo the player's pick");
            model.Reset();
            Assert.AreEqual("Engineer", model.Selected, "Reopening starts over from the preferences");
        }

        [Test]
        public void OnlyTheFallbackOpen_IsItsOwnState()
        {
            List<JobSlotInfo> full = new()
            {
                Job("Assistant", Department.Civilian, 0),
                Job("Security", Department.Security, 2, 2),
                Job("Doctor", Department.Medical, 3, 3),
            };
            LobbyJoinModel model = new();
            model.Set(full, Fallback, Preferences(("Doctor", JobPriority.High)));
            Assert.IsTrue(model.OnlyFallbackOpen);
            Assert.AreEqual("Assistant", model.Selected);
            Assert.AreEqual(JoinNoticeKind.OnlyFallback, model.Notice);
            StringAssert.StartsWith("Every other job is full right now. Assistant is always open", model.NoticeText);
        }

        [Test]
        public void NoJobsYet_NothingToJoin()
        {
            LobbyJoinModel model = new();
            model.Set(null, Fallback, null);
            Assert.IsNull(model.Selected);
            Assert.AreEqual(JoinNoticeKind.None, model.Notice);
        }

        [Test]
        public void Manifest_GroupsByDepartment_InRosterOrder_WithOpenGaps()
        {
            LobbyJoinModel model = new();
            model.Set(Station(security: 1), Fallback, null);
            List<JoinManifestGroup> groups = model.Manifest(new[]
            {
                Crew("Sam Doyle", "Assistant"),
                Crew("Lena Hart", "Security"),
                Crew("Aiko Mori", "Captain"),
                Crew("Mirelle Sand", "Engineer"),
            });

            CollectionAssert.AreEqual(
                new[] { Department.Command, Department.Security, Department.Engineering, Department.Medical, Department.Civilian },
                groups.Select(group => group.Department).ToArray());
            JoinManifestGroup security = groups[1];
            Assert.AreEqual("Lena Hart", security.Crew.Single().Name);
            CollectionAssert.IsEmpty(security.Missing, "Someone holds Security, so no gap row");
            JoinManifestGroup medical = groups[3];
            CollectionAssert.IsEmpty(medical.Crew);
            CollectionAssert.AreEqual(new[] { "Doctor" }, medical.Missing, "An open job nobody holds shows as a gap");
            Assert.IsFalse(groups.Any(group => group.Missing.Contains("Assistant")), "The fallback is never a gap");
        }

        [Test]
        public void ManifestColumns_SplitByRows_KeepingDepartmentOrder()
        {
            JoinManifestGroup Group(Department department, int rows)
            {
                JoinManifestGroup group = new() { Department = department };
                for (int i = 0; i < rows; i++) group.Crew.Add(Crew("Crew " + i, "Job"));
                return group;
            }

            // The canvas board: Command 1, Security 2, Engineering 3, Medical 2 | Science 1, Cargo 1, Service 1, Civilian 2.
            List<JoinManifestGroup> groups = new()
            {
                Group(Department.Command, 1), Group(Department.Security, 2), Group(Department.Engineering, 3),
                Group(Department.Medical, 2), Group(Department.Science, 1), Group(Department.Cargo, 1),
                Group(Department.Service, 1), Group(Department.Civilian, 2),
            };
            List<List<JoinManifestGroup>> columns = LobbyJoinModel.SplitColumns(groups);
            CollectionAssert.AreEqual(groups.Take(4), columns[0]);
            CollectionAssert.AreEqual(groups.Skip(4), columns[1]);
        }
    }
}
