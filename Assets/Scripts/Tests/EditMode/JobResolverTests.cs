using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Roles;

namespace EditorTests
{
    public class JobResolverTests
    {
        private const string Fallback = "Assistant";

        private static readonly JobSlotInfo[] Jobs =
        {
            new() { Name = Fallback, Slots = 0 },
            new() { Name = "Security", Slots = 1 },
            new() { Name = "Engineer", Slots = 2 },
        };

        private static Dictionary<string, IReadOnlyDictionary<string, JobPriority>> Prefs(
            params (string Player, string Job, JobPriority Priority)[] entries)
        {
            Dictionary<string, IReadOnlyDictionary<string, JobPriority>> result = new();
            foreach (IGrouping<string, (string Player, string Job, JobPriority Priority)> player in entries.GroupBy(e => e.Player))
            {
                result[player.Key] = player.ToDictionary(e => e.Job, e => e.Priority);
            }

            return result;
        }

        [Test]
        public void Resolve_NoPreferences_AllGetFallback()
        {
            Dictionary<string, string> result = JobResolver.Resolve(
                new[] { "a", "b" }, Prefs(), Jobs, Fallback, new System.Random(1));

            Assert.AreEqual(Fallback, result["a"]);
            Assert.AreEqual(Fallback, result["b"]);
        }

        [Test]
        public void Resolve_RespectsSlotCap_OverflowFallsBack()
        {
            Dictionary<string, string> result = JobResolver.Resolve(
                new[] { "a", "b", "c" },
                Prefs(("a", "Security", JobPriority.High), ("b", "Security", JobPriority.High), ("c", "Security", JobPriority.High)),
                Jobs, Fallback, new System.Random(2));

            Assert.AreEqual(1, result.Values.Count(job => job == "Security"));
            Assert.AreEqual(2, result.Values.Count(job => job == Fallback));
        }

        [Test]
        public void Resolve_HighBeatsMediumForTheSameSlot()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                Dictionary<string, string> result = JobResolver.Resolve(
                    new[] { "medium", "high" },
                    Prefs(("medium", "Security", JobPriority.Medium), ("high", "Security", JobPriority.High)),
                    Jobs, Fallback, new System.Random(seed));

                Assert.AreEqual("Security", result["high"], $"seed {seed}");
                Assert.AreEqual(Fallback, result["medium"], $"seed {seed}");
            }
        }

        [Test]
        public void Resolve_FullFirstChoice_MovesDownTheList()
        {
            Dictionary<string, string> result = JobResolver.Resolve(
                new[] { "a", "b" },
                Prefs(
                    ("a", "Security", JobPriority.High),
                    ("b", "Security", JobPriority.High),
                    ("a", "Engineer", JobPriority.Low),
                    ("b", "Engineer", JobPriority.Low)),
                Jobs, Fallback, new System.Random(3));

            CollectionAssert.AreEquivalent(new[] { "Security", "Engineer" }, result.Values);
        }

        [Test]
        public void Resolve_NeverIsNotAssigned()
        {
            Dictionary<string, string> result = JobResolver.Resolve(
                new[] { "a" }, Prefs(("a", "Security", JobPriority.Never)), Jobs, Fallback, new System.Random(4));

            Assert.AreEqual(Fallback, result["a"]);
        }

        [Test]
        public void Resolve_FallbackIgnoresItsOwnCap()
        {
            JobSlotInfo[] jobs = { new() { Name = Fallback, Slots = 1 } };

            Dictionary<string, string> result = JobResolver.Resolve(
                new[] { "a", "b", "c" }, Prefs(), jobs, Fallback, new System.Random(5));

            Assert.IsTrue(result.Values.All(job => job == Fallback));
        }

        [Test]
        public void ResolveLateJoin_SkipsFullJobs()
        {
            JobSlotInfo[] jobs =
            {
                new() { Name = Fallback, Slots = 0 },
                new() { Name = "Security", Slots = 1, Assigned = 1 },
                new() { Name = "Engineer", Slots = 2, Assigned = 1 },
            };
            Dictionary<string, JobPriority> prefs = new()
            {
                ["Security"] = JobPriority.High,
                ["Engineer"] = JobPriority.Low,
            };

            Assert.AreEqual("Engineer", JobResolver.ResolveLateJoin(prefs, jobs, Fallback, new System.Random(6)));
        }

        [Test]
        public void ResolveLateJoin_NoPreferences_ReturnsFallback()
        {
            Assert.AreEqual(Fallback, JobResolver.ResolveLateJoin(null, Jobs, Fallback, new System.Random(7)));
        }

        // --- Per-character fallback (lobby effort Decision 7) ---

        [Test]
        public void Resolve_AssistantFallbackIsGuaranteed_EvenPastSlotCounts()
        {
            string[] players = Enumerable.Range(0, 12).Select(i => "p" + i).ToArray();
            Dictionary<string, string> result = JobResolver.Resolve(
                players, Prefs(players.Select(p => (p, "Security", JobPriority.High)).ToArray()), Jobs, Fallback, new System.Random(8));

            Assert.AreEqual(players.Length, result.Count, "everybody comes out with a job");
            Assert.AreEqual(1, result.Values.Count(job => job == "Security"));
            Assert.AreEqual(players.Length - 1, result.Values.Count(job => job == Fallback));
        }

        [Test]
        public void Resolve_ReturnToLobby_LeavesUnplacedPlayersOut()
        {
            Dictionary<string, string> result = JobResolver.Resolve(
                new[] { "a", "b", "c" },
                Prefs(("a", "Security", JobPriority.High), ("b", "Security", JobPriority.High)),
                Jobs, Fallback, new System.Random(9), new HashSet<string> { "a", "b", "c" });

            Assert.AreEqual(1, result.Count, "one Security slot; the others have no open job and return to the lobby");
            Assert.AreEqual("Security", result.Values.Single());
            Assert.IsFalse(result.ContainsKey("c"), "no preferences and return-to-lobby means no job");
        }

        [Test]
        public void Resolve_ReturnToLobby_IsPerPlayer()
        {
            Dictionary<string, string> result = JobResolver.Resolve(
                new[] { "lobby", "assistant" }, Prefs(), Jobs, Fallback, new System.Random(10), new HashSet<string> { "lobby" });

            Assert.IsFalse(result.ContainsKey("lobby"));
            Assert.AreEqual(Fallback, result["assistant"]);
        }

        [Test]
        public void ResolveLateJoin_ReturnToLobby_ReturnsNullWhenNothingIsOpen()
        {
            JobSlotInfo[] jobs = { new() { Name = Fallback, Slots = 0 }, new() { Name = "Security", Slots = 1, Assigned = 1 } };
            Dictionary<string, JobPriority> prefs = new() { ["Security"] = JobPriority.High };

            Assert.IsNull(JobResolver.ResolveLateJoin(prefs, jobs, Fallback, new System.Random(11), JobFallback.ReturnToLobby));
            Assert.AreEqual(Fallback, JobResolver.ResolveLateJoin(prefs, jobs, Fallback, new System.Random(11), JobFallback.Assistant));
        }

        [Test]
        public void LateJoinPick_RejectsAFullJob()
        {
            JobSlotInfo[] jobs =
            {
                new() { Name = Fallback, Slots = 1, Assigned = 1 },
                new() { Name = "Security", Slots = 1, Assigned = 1 },
                new() { Name = "Engineer", Slots = 2, Assigned = 1 },
            };

            StringAssert.Contains("full", JobResolver.LateJoinPickRefusal("Security", jobs, Fallback));
            Assert.IsNull(JobResolver.LateJoinPickRefusal("Engineer", jobs, Fallback));
            Assert.IsNull(JobResolver.LateJoinPickRefusal(Fallback, jobs, Fallback), "The fallback job has no cap");
            Assert.IsNotNull(JobResolver.LateJoinPickRefusal("Captain", jobs, Fallback), "Not a job on this station");
            Assert.IsNotNull(JobResolver.LateJoinPickRefusal("", jobs, Fallback));
        }

        [Test]
        public void Manifest_ListsCrewOnce_LeavesOutOperatives()
        {
            CrewManifest manifest = new();
            Assert.AreEqual(0, manifest.Record("a", "Ann Lee", "Engineer", antagonist: false));
            Assert.AreEqual(-1, manifest.Record("op", "Agent Red", "Nuclear Operative", antagonist: true));
            Assert.AreEqual(-1, manifest.Record("ghost", "Watcher", null, antagonist: false), "Observers have no job");
            Assert.AreEqual(1, manifest.Record("b", "", Fallback, antagonist: false));

            Assert.AreEqual(0, manifest.Record("a", "Ann Lee", "Security", antagonist: false), "A respawn updates its row");

            Assert.AreEqual(2, manifest.Entries.Count);
            Assert.AreEqual("Security", manifest.Entries[0].Job);
            Assert.AreEqual("b", manifest.Entries[1].Name, "No character name falls back to the ckey");

            manifest.Clear();
            Assert.AreEqual(0, manifest.Entries.Count);
        }
    }
}
