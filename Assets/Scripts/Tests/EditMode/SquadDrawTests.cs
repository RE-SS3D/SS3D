using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.Roles;

namespace EditorTests
{
    /// <summary>The Nuke Ops squad draw and briefing (effort <c>2026-11_round-end-loop</c> P2, Decisions 10, 11, 14).</summary>
    public class SquadDrawTests
    {
        private static string[] Players(int count) => Enumerable.Range(0, count).Select(i => $"p{i}").ToArray();

        [TestCase(1, 1)]
        [TestCase(4, 1)]
        [TestCase(5, 2)]
        [TestCase(8, 2)]
        [TestCase(9, 3)]
        [TestCase(20, 5)]
        [TestCase(64, 5)]
        public void Size_IsOnePerFourReadyPlayers_CappedAtFive(int ready, int expected)
        {
            Assert.AreEqual(expected, SquadDraw.Size(ready, 1));
        }

        [Test]
        public void Size_IsAtLeastTheMinimum()
        {
            Assert.AreEqual(3, SquadDraw.Size(4, 3));
            Assert.AreEqual(SquadDraw.MaxSize, SquadDraw.Size(4, 99));
            Assert.AreEqual(0, SquadDraw.Size(0, 1));
        }

        [Test]
        public void Draw_TakesOnlyOptedInPlayers()
        {
            string[] ready = Players(20);
            HashSet<string> optedIn = new() { "p3", "p7", "p11" };

            SquadDrawResult result = SquadDraw.Draw(ready, optedIn.Contains, 1, new System.Random(4));

            Assert.IsFalse(result.FellBack);
            Assert.AreEqual(3, result.Squad.Count, "five wanted, but only three opted in");
            CollectionAssert.IsSubsetOf(result.Squad, optedIn);
        }

        [Test]
        public void Draw_TakesTheSquadSizeFromManyOptIns()
        {
            string[] ready = Players(12);

            SquadDrawResult result = SquadDraw.Draw(ready, _ => true, 1, new System.Random(1));

            Assert.AreEqual(3, result.Squad.Count);
            CollectionAssert.AllItemsAreUnique(result.Squad);
        }

        [Test]
        public void Draw_IsRandomAmongOptIns()
        {
            string[] ready = Players(4);
            HashSet<string> drawn = new();
            for (int seed = 0; seed < 50; seed++)
                drawn.Add(SquadDraw.Draw(ready, _ => true, 1, new System.Random(seed)).Squad.Single());

            Assert.Greater(drawn.Count, 1);
        }

        [Test]
        public void Draw_FallsBackWhenOptInsAreBelowTheMinimum()
        {
            string[] ready = Players(8);
            HashSet<string> optedIn = new() { "p1" };

            SquadDrawResult result = SquadDraw.Draw(ready, optedIn.Contains, 2, new System.Random(1));

            Assert.IsTrue(result.FellBack);
            Assert.IsEmpty(result.Squad);
            Assert.AreEqual(1, result.OptedIn);
            Assert.AreEqual(2, result.Minimum);
        }

        [Test]
        public void Draw_FallsBackWithNoOptIns()
        {
            SquadDrawResult result = SquadDraw.Draw(Players(3), _ => false, 1, new System.Random(1));

            Assert.IsTrue(result.FellBack);
            StringAssert.Contains("Extended", SquadDraw.FallbackNotice("Secret", result));
        }

        [Test]
        public void Draw_OnlyConsidersThePlayersPassedIn()
        {
            // Latejoiners are never in the round-start list, so an opted-in latejoiner cannot be drawn.
            string[] ready = { "a", "b" };

            SquadDrawResult result = SquadDraw.Draw(ready, ckey => ckey is "late" or "a", 1, new System.Random(1));

            CollectionAssert.AreEqual(new[] { "a" }, result.Squad);
        }

        [Test]
        public void Briefing_NamesRoleObjectiveAndSquad()
        {
            string text = OperativeBriefing.Text("Operative", new[] { "Detonate the station's nuclear device" },
                new[] { "Ada Vance", "Bo Lind" });

            StringAssert.Contains("Operative", text);
            StringAssert.Contains("Detonate the station's nuclear device", text);
            StringAssert.Contains("Ada Vance, Bo Lind", text);
        }

        [Test]
        public void Briefing_SaysWhenAlone()
        {
            string text = OperativeBriefing.Text("Operative", new[] { "Detonate" }, new string[0]);

            StringAssert.Contains("alone", text);
        }
    }
}
