using NUnit.Framework;
using SS3D.Systems.Rounds;
using SS3D.UI.Lobby;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Lobby effort Phase 4: the sidebar's round section and entry buttons, one case per state on the canvas's
    /// "Sidebar states" artboard.
    /// </summary>
    public sealed class LobbySidebarModelTests
    {
        private static LobbySessionInfo PreRound() => new()
        {
            State = RoundState.Stopped,
            AutoStartEnabled = true,
            Connected = 3,
            MinimumConnected = 5,
            LateJoinAllowed = true,
            FirstName = "Rory",
        };

        [Test]
        public void Countdown_ShowsTheTimerAndReadyUp()
        {
            LobbySessionInfo info = PreRound();
            info.AutoStartSecondsLeft = 161;
            LobbyRoundStatus round = LobbySidebarModel.Round(info, 0);
            Assert.AreEqual("Round starts in", round.Caption);
            Assert.AreEqual("02:41", round.Timer);
            Assert.IsFalse(round.Held);

            LobbyEntryButton entry = LobbySidebarModel.Entry(info);
            Assert.AreEqual("Ready up", entry.Title);
            Assert.AreEqual("Spawn as Rory when the round starts", entry.Subtitle);
            Assert.AreEqual(LobbyButtonLook.Accent, entry.Look);
            Assert.IsTrue(entry.Enabled);
        }

        [Test]
        public void Ready_IsConfirmedAndCancellable()
        {
            LobbySessionInfo info = PreRound();
            info.Ready = true;
            LobbyEntryButton entry = LobbySidebarModel.Entry(info);
            Assert.AreEqual(LobbyButtonLook.Confirmed, entry.Look);
            Assert.IsTrue(entry.Enabled);
            StringAssert.Contains("Click to cancel", entry.Subtitle);
        }

        [Test]
        public void Waiting_CountsConnectedPlayersAgainstTheMinimum()
        {
            LobbyRoundStatus round = LobbySidebarModel.Round(PreRound(), 0);
            Assert.AreEqual("Waiting for players", round.Caption);
            Assert.AreEqual("", round.Timer);
            Assert.AreEqual("3/5 connected. The countdown starts at 5.", round.Note);
        }

        [Test]
        public void Held_StrikesTheLastCountdownAndSaysWhy()
        {
            LobbySessionInfo info = PreRound();
            info.AutoStartHeld = true;
            LobbyRoundStatus round = LobbySidebarModel.Round(info, 161);
            Assert.IsTrue(round.Held);
            Assert.AreEqual("02:41", round.Timer);
            Assert.AreEqual("Countdown held by an admin", round.Note);
            Assert.AreEqual(LobbyTone.Warning, round.NoteTone);
        }

        [Test]
        public void ManualStart_SaysAnAdminStartsIt()
        {
            LobbySessionInfo info = PreRound();
            info.AutoStartEnabled = false;
            Assert.AreEqual("Waiting for an admin", LobbySidebarModel.Round(info, 0).Caption);
        }

        [Test]
        public void InRound_ShowsElapsedTimeAndJoin()
        {
            LobbySessionInfo info = PreRound();
            info.State = RoundState.Ongoing;
            info.TimerSeconds = 47 * 60 + 12;
            LobbyRoundStatus round = LobbySidebarModel.Round(info, 0);
            Assert.AreEqual("In progress", round.Status);
            Assert.AreEqual(LobbyTone.Live, round.Tone);
            Assert.AreEqual("00:47:12", round.Timer);

            LobbyEntryButton entry = LobbySidebarModel.Entry(info);
            Assert.AreEqual("Join", entry.Title);
            Assert.IsTrue(entry.Enabled);
        }

        [Test]
        public void InRound_WithABody_IsDisabled()
        {
            LobbySessionInfo info = PreRound();
            info.State = RoundState.Ongoing;
            info.Spawned = true;
            LobbyEntryButton entry = LobbySidebarModel.Entry(info);
            Assert.AreEqual("In round", entry.Title);
            Assert.IsFalse(entry.Enabled);
            Assert.IsFalse(LobbySidebarModel.Observe(info).Enabled);
        }

        [Test]
        public void InRound_LateJoinOff_DisablesJoinButNotObserve()
        {
            LobbySessionInfo info = PreRound();
            info.State = RoundState.Ongoing;
            info.LateJoinAllowed = false;
            info.CanObserve = true;
            LobbyEntryButton entry = LobbySidebarModel.Entry(info);
            Assert.AreEqual("Join", entry.Title);
            Assert.IsFalse(entry.Enabled);
            StringAssert.Contains("You can still observe", entry.Note);
            Assert.IsTrue(LobbySidebarModel.Observe(info).Enabled);
        }

        [Test]
        public void Observe_WaitsForALoadedStation()
        {
            LobbyEntryButton observe = LobbySidebarModel.Observe(PreRound());
            Assert.IsFalse(observe.Enabled);
            Assert.AreEqual("Opens once the station has loaded", observe.Subtitle);
        }

        [Test]
        public void FirstName_IsTheFirstWord()
        {
            Assert.AreEqual("Rory", LobbySidebarModel.FirstName("  Rory Lindqvist "));
            Assert.AreEqual("", LobbySidebarModel.FirstName(null));
        }
    }
}
