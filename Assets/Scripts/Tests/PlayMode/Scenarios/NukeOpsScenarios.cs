using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Comms;
using SS3D.Systems.Furniture;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Roles;
using SS3D.Systems.Rounds;
using SS3D.Systems.ScreenEffects;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.SpawnPoints;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: a Nuclear Emergency round draws the opted-in host as an operative who spawns on the marker with
    // the kit, briefing and team objective (effort 2026-11_round-end-loop P2), and both §6 outcomes are reachable
    // (P3, Decisions 17 and 19: detonation → operatives win with the station destroyed; squad killed → crew win
    // at once) · Not cheaper because: round flow is the subject (testing philosophy rule 3): the draw, spawn,
    // gamemode init, death swap and outcome only meet in a live round on a networked host
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.NukeOps)]
    public class NukeOpsScenarios : ScenarioFixture
    {
        private static readonly List<string> Briefings = new();
        private static CommsSubSystem _briefingSource;

        protected override string Mode => "Nuclear Emergency";

        protected override bool FreshBodyPerScenario => false;

        protected override void BeforeRoundStart()
        {
            SubSystems.Get<RoleSubSystem>().OptIntoAntagonistForTesting(ServerHelpers.LocalCkey, AntagonistCategories.NuclearOperative);
            Briefings.Clear();
            CommsSubSystem comms = SubSystems.Get<CommsSubSystem>();
            if (_briefingSource != comms)
            {
                if (_briefingSource != null)
                    _briefingSource.OnCommsMessageReceived -= RecordBriefing;
                comms.OnCommsMessageReceived += RecordBriefing;
                _briefingSource = comms;
            }
        }

        [OneTimeTearDown]
        public void StopRecordingBriefings()
        {
            if (_briefingSource != null)
                _briefingSource.OnCommsMessageReceived -= RecordBriefing;
            _briefingSource = null;
        }

        private static void RecordBriefing(CommsMessage message)
        {
            if (message.ChannelId == OperativeBriefing.Header)
                Briefings.Add(message.Text);
        }

        [UnityTest]
        public IEnumerator RoundStart_HostSpawnsAsAGearedBriefedOperative()
        {
            RoleSubSystem roles = SubSystems.Get<RoleSubSystem>();
            Assert.IsTrue(roles.IsOperative(Host), "The opted-in host is drawn into the squad");
            Assert.IsTrue(roles.TryGetRole(Host, out RoleData role) && role.Name == "Operative", "The host's role is Operative");

            SpawnPointRecord marker = SubSystems.Get<TileSubSystem>().SpawnPoints.Records
                .Single(spawn => spawn.Kind == SpawnPointKind.Antagonist && spawn.AntagonistCategory == AntagonistSpawnCategory.NuclearOperatives);
            Vector3 offset = Body.transform.position - marker.Position;
            Assert.Less(new Vector2(offset.x, offset.z).magnitude, 0.75f, $"The operative spawns on the marker at {marker.Position}");

            Hands hands = Body.GetComponentInParent<HumanInventory>().Hands;
            Assert.IsTrue(hands.PlayerHands.Any(hand => hand.ItemInHand != null), "The operative kit puts a gun in hand");

            yield return WaitFor(() => Briefings.Count > 0, "the briefing reaches the operative's feed", 10f);
            StringAssert.Contains("Operative", Briefings[0], "The briefing names the role");
            Assert.IsTrue(Briefings[0].IndexOf("detonate", System.StringComparison.OrdinalIgnoreCase) >= 0,
                $"The briefing names the team objective: {Briefings[0]}");
        }

        [UnityTest]
        public IEnumerator Detonation_OperativesWin_StationDestroyed()
        {
            ChangesRoom = true;
            Nuke nuke = Object.FindFirstObjectByType<Nuke>();
            Assert.IsNotNull(nuke, "The test room has a nuclear device");

            nuke.Detonate();
            RoundOutcome outcome = null;
            yield return WaitFor(() => (outcome = CurrentOutcome()) != null, "the detonation ends the round", 10f);

            Assert.AreEqual(RoundWinner.Operatives, outcome.Winner);
            Assert.IsTrue(outcome.StationDestroyed, "The summary says the station was destroyed");
            Assert.IsTrue(outcome.Objectives.Length == 1 && outcome.Objectives[0].Result == ObjectiveResult.Met,
                "The squad's team objective is met");
            Assert.IsTrue(outcome.Players.Any(player => player.Ckey == ServerHelpers.LocalCkey && player.Antagonist),
                "The host is listed in the antagonist roster");
            Assert.IsTrue(StationDetonationPresentation.IsPlaying, "The host's client plays the white-out");
        }

        [UnityTest]
        public IEnumerator SquadKilled_CrewWinAtOnce()
        {
            ChangesRoom = true;
            yield return Console.Expect($"kill {ServerHelpers.LocalCkey}", "Player killed");
            RoundOutcome outcome = null;
            yield return WaitFor(() => (outcome = CurrentOutcome()) != null, "the last operative's death ends the round", 5f);

            Assert.AreEqual(RoundWinner.Crew, outcome.Winner);
            Assert.AreEqual(RoundEndTrigger.GamemodeCondition, outcome.Trigger);
            Assert.AreEqual(NukeOpsRules.SquadOutReason, outcome.Reason);
            Assert.IsFalse(outcome.StationDestroyed);
        }

        /// <summary>This round's outcome as the host's client received it, or null while it runs.</summary>
        private static RoundOutcome CurrentOutcome()
        {
            RoundSubSystem rounds = SubSystems.Get<RoundSubSystem>();
            RoundOutcome outcome = rounds.LastOutcome;
            return outcome != null && outcome.RoundId == rounds.RoundId ? outcome : null;
        }
    }
}
