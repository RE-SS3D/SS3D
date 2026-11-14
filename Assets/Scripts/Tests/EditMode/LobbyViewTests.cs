using NUnit.Framework;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Rounds;
using SS3D.UI.Lobby;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Lobby effort Phase 4: the shell binds <c>Lobby.uxml</c> through the committed catalog, keeps its stylesheet
    /// after leaving the TemplateContainer, and paints the sidebar and tabs from snapshots. No panel or window: the
    /// tree is checked as built.
    /// </summary>
    public sealed class LobbyViewTests
    {
        private LobbyView _view;
        private VisualElement _layer;

        private static LobbySessionInfo PreRound() => new()
        {
            State = RoundState.Stopped,
            AutoStartEnabled = true,
            AutoStartSecondsLeft = 161,
            Connected = 1,
            MinimumConnected = 1,
            LateJoinAllowed = true,
            FirstName = "Rory",
        };

        [SetUp]
        public void SetUp()
        {
            LobbyAssetCatalog catalog = Resources.Load<LobbyAssetCatalog>(LobbyAssetPaths.ResourcesCatalogName);
            Assert.IsNotNull(catalog, "The lobby catalog is committed under Resources");
            Assert.IsTrue(catalog.HasRequiredAssets(out string missing), "Catalog is missing " + missing);
            _layer = new VisualElement();
            _view = new LobbyView(catalog);
            _view.Attach(_layer);
            Assert.IsNotNull(_view.Root, "The shell bound its template");
        }

        [TearDown]
        public void TearDown() => _view.Detach();

        [Test]
        public void Shell_KeepsItsStylesheetAndTabs()
        {
            Assert.AreSame(_layer, _view.Root.parent, "The root moved out of its TemplateContainer into the layer");
            Assert.Greater(_view.Root.styleSheets.count, 0, "Lobby.uss is linked on the root, so it survives the move");
            Assert.IsNull(_view.Root.Q("tab-jobs"), "No job list in the lobby (lobby.md §8)");
            Assert.IsNull(_view.Root.Q("tab-players"), "No player list in the lobby (lobby.md §8)");
            Assert.AreEqual(LobbyView.TabServerInfo, _view.ActiveTab, "A player lands on Server Info");
            Assert.IsNotNull(_view.Root.Q("server-info"), "Server Info is mounted");
        }

        [Test]
        public void Sidebar_PaintsTheCountdownAndReadyUp()
        {
            _view.SetSession(PreRound(), false);
            Assert.AreEqual("02:41", _view.Root.Q<Label>("lobby-round-timer").text);
            Assert.AreEqual("READY UP", _view.Root.Q<Label>("lobby-ready-title").text);
            Assert.IsFalse(_view.Root.Q<Button>("lobby-observe").enabledSelf, "Observe waits for a loaded station");
            Assert.IsTrue(_view.Root.Q("lobby-admin").ClassListContains("lobby__admin--hidden"), "Players see no Admin switch");
        }

        [Test]
        public void HeldCountdown_IsStruckThrough()
        {
            _view.SetSession(PreRound(), false);
            LobbySessionInfo held = PreRound();
            held.AutoStartSecondsLeft = 0;
            held.AutoStartHeld = true;
            _view.SetSession(held, false);
            Assert.IsTrue(_view.Root.Q("lobby-round-timer-wrap").ClassListContains("lobby__timer-wrap--held"));
            Assert.AreEqual("02:41", _view.Root.Q<Label>("lobby-round-timer").text, "The held timer keeps its last value");
            Assert.AreEqual("Countdown held by an admin", _view.Root.Q<Label>("lobby-round-note-text").text);
        }

        [Test]
        public void InRound_WithABody_OffersBackToRound()
        {
            LobbySessionInfo info = PreRound();
            info.State = RoundState.Ongoing;
            info.Spawned = true;
            _view.SetSession(info, false);
            Assert.IsTrue(_view.Root.Q("lobby-back-to-round").ClassListContains("lobby__back--shown"));
            Assert.AreEqual("IN ROUND", _view.Root.Q<Label>("lobby-ready-title").text);
        }

        [Test]
        public void Admin_GetsTheSwitch_ServerSettingsStaysOffUntilTurnedOn()
        {
            _view.SetSession(PreRound(), true);
            Assert.IsFalse(_view.Root.Q("lobby-admin").ClassListContains("lobby__admin--hidden"));
            Assert.IsTrue(_view.Root.Q("tab-server-settings").ClassListContains("lobby__tab--hidden"));
            Assert.IsTrue(_view.Root.Q("tab-last-round").ClassListContains("lobby__tab--hidden"), "No Last Round before a round ends");
        }

        [Test]
        public void CharacterCard_ShowsTheHighJobInItsDepartmentColour()
        {
            _view.SetCharacter("Rory Lindqvist", "Security", Department.Security, 3, 10);
            Assert.AreEqual("Rory Lindqvist", _view.Root.Q<Label>("lobby-character-name").text);
            Assert.AreEqual("Security", _view.Root.Q<Label>("lobby-high-job").text);
            Assert.IsTrue(_view.Root.Q("lobby-high-job").ClassListContains("lb-dept--security"));
            Assert.AreEqual("3/10", _view.Root.Q<Label>("lobby-character-count").text);

            _view.SetCharacter("Rory Lindqvist", null, Department.None, 3, 10);
            Assert.AreEqual("No job preferences", _view.Root.Q<Label>("lobby-high-prefix").text);
            Assert.IsFalse(_view.Root.Q("lobby-high-job").ClassListContains("lb-dept--security"));
        }
    }
}
