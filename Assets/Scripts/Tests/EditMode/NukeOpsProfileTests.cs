using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Roles;
using SS3D.Systems.Rounds;
using SS3D.UI.Lobby;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Round-end loop P4: the <c>-nukeops</c> 24/7 server profile, Secret's ready-player threshold (Decision 23)
    /// and Nuclear Operative opt-in on by default (Decision 22).
    /// </summary>
    public sealed class NukeOpsProfileTests
    {
        [Test]
        public void Profile_AppliesNukeOpsRules_KeepsMapAndName()
        {
            ServerSettings original = ServerSettings.Default;
            original.Mode = "Extended";
            original.Map = "ministation";
            original.ServerName = "Test";
            original.RespawnEnabled = true;
            original.AllConnectedReady = true;

            ServerSettings profile = NukeOpsProfile.Apply(original, NukeOpsProfile.RoundLengthMinutes, NukeOpsProfile.MinimumReadyPlayers);

            Assert.AreEqual("Secret", profile.Mode);
            Assert.AreEqual(30, profile.RoundLengthMinutes);
            Assert.AreEqual(4, profile.MinimumReadyForNukeOps);
            Assert.IsTrue(profile.AutoStart);
            Assert.IsTrue(profile.AllowLateJoin);
            Assert.IsFalse(profile.AllConnectedReady, "players ready themselves, as on a normal server");
            Assert.IsFalse(profile.RespawnEnabled);
            Assert.IsTrue(profile.MapPersistence, "persistence as a normal server");
            StringAssert.Contains("4 or more ready players", profile.Motd);
            Assert.AreEqual("ministation", profile.Map);
            Assert.AreEqual("Test", profile.ServerName);
            Assert.IsTrue(profile.IsValid);
        }

        [Test]
        public void Flags_OverrideRoundLengthAndThreshold_ForTests()
        {
            string[] args = { "game", "-nukeops", "-nukeops-round-minutes=1", "-nukeops-min-ready=1" };
            Assert.AreEqual(1, NukeOpsProfile.RoundMinutesFrom(args));
            Assert.AreEqual(1, NukeOpsProfile.MinimumReadyFrom(args));

            string[] invalid = { "-nukeops", "-nukeops-round-minutes=0", "-nukeops-min-ready=abc" };
            Assert.AreEqual(NukeOpsProfile.RoundLengthMinutes, NukeOpsProfile.RoundMinutesFrom(invalid));
            Assert.AreEqual(NukeOpsProfile.MinimumReadyPlayers, NukeOpsProfile.MinimumReadyFrom(invalid));
        }

        [Test]
        public void Threshold_DefaultsToFour_AndSurvivesAnAdminFormSave()
        {
            Assert.AreEqual(4, ServerSettings.Default.MinimumReadyForNukeOps);

            ServerSettings current = NukeOpsProfile.Apply(ServerSettings.Default, 30, 2);
            ServerSettings saved = ServerSettings.Default.WithRulesOf(current);

            Assert.AreEqual(2, saved.MinimumReadyForNukeOps);
        }

        [Test]
        public void Threshold_OutOfRange_IsInvalid()
        {
            ServerSettings settings = ServerSettings.Default;
            settings.MinimumReadyForNukeOps = 0;
            Assert.IsFalse(settings.IsValid);
        }

        [TestCase("Secret", 3, 4, false)]
        [TestCase("Secret", 4, 4, true)]
        [TestCase("Secret", 1, 1, true)]
        [TestCase("Nuclear Emergency", 1, 4, true)]
        public void Threshold_GatesOnlySecret(string mode, int ready, int minimum, bool expected)
        {
            Assert.AreEqual(expected, SquadDraw.HasEnoughReady(mode, ready, minimum));
        }

        [Test]
        public void TooFewReadyNotice_NamesBothCounts()
        {
            string notice = SquadDraw.TooFewReadyNotice(3, 4);
            StringAssert.Contains("4 ready players", notice);
            StringAssert.Contains("3 were ready", notice);
            StringAssert.Contains("Extended", notice);
        }

        [Test]
        public void OptIn_IsOnWhenNoPreferencesWereSent()
        {
            Assert.IsTrue(AntagonistCategories.IsOptedIn(null, AntagonistCategories.NuclearOperative));
            Assert.IsFalse(AntagonistCategories.IsOptedIn(null, "traitor"), "unknown categories never count");
        }

        [Test]
        public void OptIn_FollowsSentPreferences()
        {
            Assert.IsFalse(AntagonistCategories.IsOptedIn(new HashSet<string>(), AntagonistCategories.NuclearOperative), "an empty list is an opt-out");
            Assert.IsTrue(AntagonistCategories.IsOptedIn(new HashSet<string> { AntagonistCategories.NuclearOperative },
                AntagonistCategories.NuclearOperative));
        }

        [Test]
        public void LobbyList_StartsWithEveryAntagonistRoleTicked()
        {
            LobbyShellView view = new(null);

            CollectionAssert.AreEquivalent(AntagonistCategories.All.Select(role => role.Key), view.AntagonistOptIns);
        }
    }
}
