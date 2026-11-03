using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Rounds;

namespace SS3D.Tests.EditMode
{
    /// <summary>Round-end loop P1 (<c>2026-11_round-end-loop</c>): outcome per trigger, the beat and hold, the viewer's summary, auto-start.</summary>
    public sealed class RoundEndLoopTests
    {
        private static RoundOutcomePlayer Player(string ckey, string name, string job, bool alive, bool antagonist = false) =>
            new() { Ckey = ckey, Name = name, Job = job, Alive = alive, Antagonist = antagonist };

        private static RoundOutcome Outcome(RoundEndRequest request, params RoundOutcomePlayer[] players) =>
            RoundOutcomeBuilder.Build(request, 12, "Secret", "Nuclear Emergency", "ministation", 1721, players, "Operatives");

        [Test]
        public void Timer_HasNoWinner_AndSaysTimeRanOut()
        {
            RoundOutcome outcome = Outcome(RoundEndRequest.Timer);

            Assert.AreEqual(RoundEndTrigger.Timer, outcome.Trigger);
            Assert.AreEqual(RoundWinner.None, outcome.Winner);
            Assert.AreEqual("Time ran out.", outcome.Reason);
            Assert.IsFalse(outcome.StationDestroyed);
        }

        [Test]
        public void Admin_HasNoWinner_AndSaysEndedByAnAdmin()
        {
            RoundOutcome outcome = Outcome(RoundEndRequest.Admin);

            Assert.AreEqual(RoundEndTrigger.Admin, outcome.Trigger);
            Assert.AreEqual(RoundWinner.None, outcome.Winner);
            Assert.AreEqual("Ended by an admin.", outcome.Reason);
        }

        [Test]
        public void Detonation_IsAnOperativeWin_WithTheStationDestroyed()
        {
            RoundOutcome outcome = Outcome(RoundEndRequest.Detonation);

            Assert.AreEqual(RoundEndTrigger.GamemodeCondition, outcome.Trigger);
            Assert.AreEqual(RoundWinner.Operatives, outcome.Winner);
            Assert.IsTrue(outcome.StationDestroyed);
            Assert.AreEqual("The nuclear device detonated.", outcome.Reason);
            Assert.AreEqual("Operatives win", RoundOutcomeText.Title(outcome.Winner));
        }

        [Test]
        public void Build_RecordsRoundFacts()
        {
            RoundOutcome outcome = Outcome(RoundEndRequest.Timer);

            Assert.AreEqual(12, outcome.RoundId);
            Assert.AreEqual("Secret", outcome.ConfiguredMode);
            Assert.AreEqual("Nuclear Emergency", outcome.Mode);
            Assert.AreEqual("ministation", outcome.Map);
            Assert.AreEqual(1721, outcome.DurationSeconds);
            Assert.AreEqual("28:41", RoundOutcomeText.Duration(outcome.DurationSeconds));
        }

        [Test]
        public void Build_KeepsARespawnedPlayerOnce_AsTheirLatestBody()
        {
            RoundOutcome outcome = Outcome(RoundEndRequest.Timer,
                Player("ada", "Ada Vance", "Chief Engineer", alive: false),
                Player("bo", "Bo Sterling", "Cargo Technician", alive: true),
                Player("ada", "Ada Vance", "Chief Engineer", alive: true));

            Assert.AreEqual(2, outcome.Players.Length);
            Assert.IsTrue(outcome.Players.Single(p => p.Ckey == "ada").Alive);
        }

        [Test]
        public void Build_FillsMissingNameAndJob_AndDropsRowsWithoutCkey()
        {
            RoundOutcome outcome = Outcome(RoundEndRequest.Timer, Player("ada", "", "", true), Player("", "Nobody", "Clown", true));

            Assert.AreEqual(1, outcome.Players.Length);
            Assert.AreEqual("ada", outcome.Players[0].Name);
            Assert.AreEqual("Crew", outcome.Players[0].Job);
        }

        [Test]
        public void Build_NoAntagonists_HasNoAntagonistRoster()
        {
            RoundOutcome outcome = Outcome(RoundEndRequest.Timer, Player("ada", "Ada", "Captain", true));

            Assert.AreEqual("", outcome.AntagonistLabel);
            Assert.IsFalse(new RoundSummaryModel(outcome, "ada").HasAntagonists);
            Assert.AreEqual("EVERYONE", new RoundSummaryModel(outcome, "ada").CrewLabel);
        }

        [Test]
        public void Summary_HighlightsTheViewer_AndSpellsOutTheirFate()
        {
            RoundOutcome outcome = Outcome(RoundEndRequest.Detonation,
                Player("ada", "Ada Vance", "Chief Engineer", alive: false),
                Player("bo", "Bo Sterling", "Cargo Technician", alive: true),
                Player("vik", "Viktor Kessler", "Nuclear Operative", alive: true, antagonist: true));

            RoundSummaryModel model = new(outcome, "ada");

            Assert.AreEqual(RoundSummaryModel.FateKind.Dead, model.Fate);
            Assert.AreEqual("Dead", model.FateText);
            Assert.AreEqual("Ada Vance · Chief Engineer · ghost", model.FateWho);
            Assert.AreEqual(1, model.Crew.Count(r => r.IsViewer));
            Assert.AreEqual(2, model.Crew.Count);
            Assert.AreEqual(1, model.Antagonists.Count);
            Assert.AreEqual("CREW", model.CrewLabel);
            Assert.AreEqual("1 of 2 alive", model.CrewCount);
            Assert.IsTrue(model.ShowConfiguredMode);
        }

        [Test]
        public void Summary_ViewerWhoNeverTookABody_IsNotPlayed()
        {
            RoundSummaryModel model = new(Outcome(RoundEndRequest.Admin, Player("bo", "Bo", "Chef", true)), "lobbyist");

            Assert.AreEqual(RoundSummaryModel.FateKind.NotPlayed, model.Fate);
            Assert.AreEqual("In the lobby this round", model.FateWho);
            Assert.IsFalse(model.Crew.Any(r => r.IsViewer));
        }

        [Test]
        public void Beat_RunsThirtySeconds_ThenEnds()
        {
            RoundEndBeat beat = new();
            beat.Begin();
            Assert.AreEqual(RoundEndBeat.Seconds, beat.SecondsLeft);

            int ticks = 0;
            while (!beat.Tick())
                ticks++;

            Assert.AreEqual(RoundEndBeat.Seconds - 1, ticks);
            Assert.IsFalse(beat.Running);
        }

        [Test]
        public void Beat_Held_DoesNotAdvance_UntilResumed()
        {
            RoundEndBeat beat = new();
            beat.Begin();
            beat.Tick();
            Assert.IsTrue(beat.SetHeld(true));

            for (int i = 0; i < 100; i++)
                Assert.IsFalse(beat.Tick());
            Assert.AreEqual(RoundEndBeat.Seconds - 1, beat.SecondsLeft);

            Assert.IsTrue(beat.SetHeld(false));
            beat.Tick();
            Assert.AreEqual(RoundEndBeat.Seconds - 2, beat.SecondsLeft);
        }

        [Test]
        public void Beat_CannotBeHeld_WhenNotRunning_OrTwice()
        {
            RoundEndBeat beat = new();
            Assert.IsFalse(beat.SetHeld(true));

            beat.Begin();
            Assert.IsTrue(beat.SetHeld(true));
            Assert.IsFalse(beat.SetHeld(true));
        }

        [Test]
        public void Countdown_Text_AndProgress()
        {
            Assert.AreEqual("Returning to lobby in 0:24", RoundSummaryModel.Countdown(24, held: false, shortForm: false));
            Assert.AreEqual("Lobby in 0:09", RoundSummaryModel.Countdown(9, held: false, shortForm: true));
            Assert.AreEqual("Held by an admin", RoundSummaryModel.Countdown(9, held: true, shortForm: true));
            Assert.AreEqual(1f, RoundSummaryModel.Progress(3, held: true));
            Assert.AreEqual(0.5f, RoundSummaryModel.Progress(15, held: false), 0.001f);
        }

        [Test]
        public void AutoStart_WaitsForMinimumReady()
        {
            AutoStartCountdown countdown = new();
            countdown.Tick(enabled: true, idle: true, readyCount: 2, minimumReady: 3);
            Assert.AreEqual(0, countdown.SecondsLeft);

            countdown.Tick(true, true, 3, 3);
            Assert.AreEqual(AutoStartCountdown.CountdownSeconds, countdown.SecondsLeft);
        }

        [Test]
        public void DedicatedServer_AutoStartsByDefault_ListenHostDoesNot()
        {
            Assert.IsFalse(ServerSettings.Default.AutoStart);
            Assert.AreEqual(1, ServerSettings.Default.MinimumReady);
            Assert.IsTrue(ServerSettings.ForDedicatedServer(ServerSettings.Default).AutoStart);
        }

        [Test]
        public void Settings_RejectMinimumReadyBelowOne()
        {
            ServerSettings settings = ServerSettings.Default;
            settings.MinimumReady = 0;
            Assert.IsFalse(settings.IsValid);
        }
    }
}
