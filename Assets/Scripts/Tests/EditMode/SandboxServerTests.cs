using NUnit.Framework;
using SS3D.Systems.Rounds;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.Commands;

namespace SS3D.Tests.EditMode
{
    /// <summary>Sandbox server P1: the profile, map persistence gate, build denylist and invincibility refusal.</summary>
    public sealed class SandboxServerTests
    {
        [TearDown]
        public void RestorePersistence() => MapEditorPermissions.PersistenceAllowed = true;

        [Test]
        public void Profile_AppliesSandboxRules_KeepsMapAndName()
        {
            ServerSettings original = ServerSettings.Default;
            original.Map = "ministation";
            original.ServerName = "Test";

            ServerSettings sandbox = SandboxProfile.Apply(original);

            Assert.AreEqual("Extended", sandbox.Mode);
            Assert.AreEqual(20, sandbox.RoundLengthMinutes);
            Assert.AreEqual(16, sandbox.PlayerCap);
            Assert.IsTrue(sandbox.AllowLateJoin);
            Assert.IsTrue(sandbox.AutoStart);
            Assert.IsTrue(sandbox.AllConnectedReady);
            Assert.IsTrue(sandbox.RespawnEnabled);
            Assert.IsTrue(sandbox.RemoveCorpseOnRespawn);
            Assert.AreEqual(30, sandbox.RespawnCooldownSeconds);
            Assert.IsFalse(sandbox.MapPersistence);
            Assert.AreEqual(SandboxProfile.Motd, sandbox.Motd);
            Assert.AreEqual("ministation", sandbox.Map);
            Assert.AreEqual("Test", sandbox.ServerName);
            Assert.IsTrue(sandbox.IsValid);
        }

        [Test]
        public void Defaults_KeepNormalServerBehaviour()
        {
            ServerSettings settings = ServerSettings.Default;
            Assert.IsFalse(settings.AutoStart);
            Assert.IsFalse(settings.AllConnectedReady);
            Assert.IsFalse(settings.RespawnEnabled);
            Assert.IsFalse(settings.RemoveCorpseOnRespawn);
            Assert.IsTrue(settings.MapPersistence);
        }

        [Test]
        public void AdminFormSave_KeepsServerRules()
        {
            ServerSettings current = SandboxProfile.Apply(ServerSettings.Default);
            // The admin form doesn't know the rule fields, so it sends their defaults.
            ServerSettings form = ServerSettings.Default;
            form.Mode = "Secret";
            form.RoundLengthMinutes = 45;

            ServerSettings saved = form.WithRulesOf(current);

            Assert.AreEqual("Secret", saved.Mode);
            Assert.AreEqual(45, saved.RoundLengthMinutes);
            Assert.IsTrue(saved.AutoStart);
            Assert.IsTrue(saved.RespawnEnabled);
            Assert.IsTrue(saved.RemoveCorpseOnRespawn);
            Assert.IsFalse(saved.MapPersistence);
            Assert.AreEqual(SandboxProfile.Motd, saved.Motd);
        }

        [Test]
        public void RespawnCooldown_OutOfRange_IsInvalid()
        {
            ServerSettings settings = ServerSettings.Default;
            settings.RespawnCooldownSeconds = -1;
            Assert.IsFalse(settings.IsValid);
            settings.RespawnCooldownSeconds = 3601;
            Assert.IsFalse(settings.IsValid);
        }

        [Test]
        public void PersistenceGate_RefusesWithReason_WhenOff()
        {
            MapEditorPermissions.PersistenceAllowed = false;
            Assert.IsFalse(MapEditorPermissions.TryAllowPersistence(out string refusal));
            Assert.AreEqual(MapEditorPermissions.PersistenceRefusedMessage, refusal);

            MapEditorPermissions.PersistenceAllowed = true;
            Assert.IsTrue(MapEditorPermissions.TryAllowPersistence(out refusal));
            Assert.IsNull(refusal);
        }

        [Test]
        public void Capabilities_FollowPersistenceAndSandboxPlayer()
        {
            MapEditorCapabilities sandboxPlayer = MapEditorCapabilities.For(persistenceAllowed: false, isSandboxPlayer: true);
            Assert.IsFalse(sandboxPlayer.Persistence);
            Assert.IsFalse(sandboxPlayer.Invincibility);
            Assert.IsTrue(sandboxPlayer.RestrictedPalette);

            MapEditorCapabilities sandboxAdmin = MapEditorCapabilities.For(persistenceAllowed: false, isSandboxPlayer: false);
            Assert.IsFalse(sandboxAdmin.Persistence);
            Assert.IsTrue(sandboxAdmin.Invincibility);
            Assert.IsFalse(sandboxAdmin.RestrictedPalette);

            MapEditorCapabilities full = MapEditorCapabilities.Full;
            Assert.IsTrue(full.Persistence && full.Invincibility && !full.RestrictedPalette);
        }

        [Test]
        public void Invincibility_RefusedForSandboxPlayersOnly()
        {
            Assert.IsFalse(MapEditorPermissions.MayToggleInvincibility(isSandboxPlayer: true));
            Assert.IsTrue(MapEditorPermissions.MayToggleInvincibility(isSandboxPlayer: false));
        }

        [TestCase(MapEditorCommandKind.PlaceSpawnPoint, "spawn:job:Assistant")]
        [TestCase(MapEditorCommandKind.ClearSpawnPoint, "")]
        [TestCase(MapEditorCommandKind.ClearSpawnPoint, null)]
        [TestCase(MapEditorCommandKind.PlaceItem, "NuclearAuthenticationDisk")]
        [TestCase(MapEditorCommandKind.PlaceTile, "NukeCart")]
        [TestCase(MapEditorCommandKind.PlaceTile, "nukecart")]
        [TestCase(MapEditorCommandKind.MoveTile, "NukeCart")]
        public void Denylist_RefusesSpawnPointsAndNuke(MapEditorCommandKind kind, string assetName)
        {
            Assert.IsTrue(MapEditorSandboxRules.IsDenied(new MapEditorCommandDto { Kind = kind, AssetName = assetName }));
        }

        [TestCase(MapEditorCommandKind.PlaceTile, "Table")]
        [TestCase(MapEditorCommandKind.PlaceItem, "Grenade")]
        [TestCase(MapEditorCommandKind.ClearTile, "Wall")]
        [TestCase(MapEditorCommandKind.SetFloorDecal, "floor-decal:3")]
        public void Denylist_AllowsEverythingElse(MapEditorCommandKind kind, string assetName)
        {
            Assert.IsFalse(MapEditorSandboxRules.IsDenied(new MapEditorCommandDto { Kind = kind, AssetName = assetName }));
        }

        [Test]
        public void Catalog_HidesDeniedEntries_OnlyWhenRestricted()
        {
            MapEditorCatalog catalog = new();
            catalog.Build(System.Array.Empty<SS3D.Systems.Tile.GenericObjectSo>(), null);

            int Spawns() => System.Linq.Enumerable.Count(
                catalog.Query(MapEditorMode.Scripting, MapEditorSubcategory.SpawnPoints, null));

            Assert.Greater(Spawns(), 0);
            catalog.HideSandboxDenied = true;
            Assert.AreEqual(0, Spawns());
        }
    
        [Test]
        public void AutoStart_CountsDownOnceAPlayerIsReady_ThenStartsOnce()
        {
            AutoStartCountdown countdown = new();
            Assert.IsFalse(countdown.Tick(enabled: true, idle: true, readyCount: 0));
            Assert.AreEqual(0, countdown.SecondsLeft);

            Assert.IsFalse(countdown.Tick(true, true, 1));
            Assert.AreEqual(AutoStartCountdown.CountdownSeconds, countdown.SecondsLeft);

            int starts = 0;
            for (int i = 0; i < AutoStartCountdown.CountdownSeconds; i++)
                if (countdown.Tick(true, true, 1)) starts++;
            Assert.AreEqual(1, starts);
            Assert.AreEqual(0, countdown.SecondsLeft);
        }

        [TestCase(false, true, 3, false)]
        [TestCase(true, false, 3, false)]
        [TestCase(true, true, 0, false)]
        [TestCase(true, true, 3, true)]
        public void AutoStart_CountsOnlyWhenEnabledIdleAndReady(bool enabled, bool idle, int ready, bool counts)
        {
            AutoStartCountdown countdown = new();
            countdown.Tick(enabled, idle, ready);
            Assert.AreEqual(counts, countdown.SecondsLeft > 0);
        }

        [Test]
        public void AutoStart_ResetsWhenReadyPlayersLeave_AndHoldStopsIt()
        {
            AutoStartCountdown countdown = new();
            countdown.Tick(true, true, 1);
            countdown.Tick(true, true, 1);
            countdown.Tick(true, true, 0);
            Assert.AreEqual(0, countdown.SecondsLeft);

            countdown.Tick(true, true, 1);
            countdown.Held = true;
            for (int i = 0; i <= AutoStartCountdown.CountdownSeconds; i++)
                Assert.IsFalse(countdown.Tick(true, true, 1));
            Assert.AreEqual(0, countdown.SecondsLeft);
        }

        [TestCase(300, "The round ends in 5 minutes.")]
        [TestCase(60, "The round ends in 1 minute.")]
        [TestCase(299, null)]
        [TestCase(0, null)]
        public void RoundEndWarnings_At5And1Minutes(int secondsLeft, string expected)
        {
            Assert.AreEqual(expected, RoundEndWarnings.For(secondsLeft));
        }
    
        [Test]
        public void RespawnCooldown_CountsFromDeath()
        {
            SS3D.Systems.Entities.RespawnCooldown cooldown = new();
            Assert.AreEqual(0, cooldown.SecondsLeft("a", 100, 30), "Never died: no wait");

            cooldown.RecordDeath("a", 100);
            Assert.AreEqual(30, cooldown.SecondsLeft("a", 100, 30));
            Assert.AreEqual(1, cooldown.SecondsLeft("a", 129.5, 30));
            Assert.AreEqual(0, cooldown.SecondsLeft("a", 130, 30));
            Assert.AreEqual(0, cooldown.SecondsLeft("b", 100, 30), "Cooldowns are per player");

            cooldown.RecordDeath("a", 200);
            Assert.AreEqual(30, cooldown.SecondsLeft("a", 200, 30), "A second death restarts it");
            cooldown.Clear();
            Assert.AreEqual(0, cooldown.SecondsLeft("a", 200, 30));
        }

        [Test]
        public void RespawnRules_RefuseUntilEverythingHolds()
        {
            Assert.IsNotNull(SS3D.Systems.Entities.RespawnRules.Refusal(enabled: false, roundOngoing: true, isGhost: true, secondsLeft: 0));
            Assert.IsNotNull(SS3D.Systems.Entities.RespawnRules.Refusal(true, false, true, 0));
            Assert.IsNotNull(SS3D.Systems.Entities.RespawnRules.Refusal(true, true, false, 0));
            StringAssert.Contains("5 seconds", SS3D.Systems.Entities.RespawnRules.Refusal(true, true, true, 5));
            Assert.IsNull(SS3D.Systems.Entities.RespawnRules.Refusal(true, true, true, 0));
        }
    
        [TestCase("/bug the door ate me", true, "the door ate me")]
        [TestCase("/BUG  spaced  ", true, "spaced")]
        [TestCase("/bug", true, "")]
        [TestCase("/bugs are fun", false, null)]
        [TestCase("bug report", false, null)]
        [TestCase("/eng hello", false, null)]
        public void BugCommand_ParsesNote(string text, bool isBug, string note)
        {
            Assert.AreEqual(isBug, BugCommand.TryParse(text, out string parsed));
            Assert.AreEqual(note, parsed);
        }

        [Test]
        public void BugCommand_ClampsLongNotes()
        {
            Assert.IsTrue(BugCommand.TryParse("/bug " + new string('x', 2000), out string note));
            Assert.AreEqual(BugCommand.MaxNoteLength, note.Length);
        }
    
        [Test]
        public void RoundMinutesOverride_ParsesValidValuesOnly()
        {
            Assert.AreEqual(1, SandboxProfile.RoundMinutesFrom(new[] { "game", "-sandbox", "-sandbox-round-minutes=1" }));
            Assert.AreEqual(SandboxProfile.RoundLengthMinutes, SandboxProfile.RoundMinutesFrom(new[] { "-sandbox" }));
            Assert.AreEqual(SandboxProfile.RoundLengthMinutes, SandboxProfile.RoundMinutesFrom(new[] { "-sandbox-round-minutes=0" }));
            Assert.AreEqual(SandboxProfile.RoundLengthMinutes, SandboxProfile.RoundMinutesFrom(new[] { "-sandbox-round-minutes=abc" }));
            Assert.AreEqual(7, SandboxProfile.Apply(ServerSettings.Default, 7).RoundLengthMinutes);
        }
    
        [Test]
        public void ServerLifetime_ParsesHeartbeatAndRoundLimit()
        {
            ServerLifetimeOptions options = ServerLifetimeOptions.Parse(new[]
            {
                "SS3D.x86_64", "-serveronly", "-heartbeat-file=/data/heartbeat", "-exit-after-rounds=12",
            });
            Assert.AreEqual("/data/heartbeat", options.HeartbeatPath);
            Assert.AreEqual(12, options.ExitAfterRounds);
            Assert.IsFalse(options.ShouldExit(11));
            Assert.IsTrue(options.ShouldExit(12));

            ServerLifetimeOptions none = ServerLifetimeOptions.Parse(new[] { "-serveronly", "-exit-after-rounds=nope" });
            Assert.AreEqual(string.Empty, none.HeartbeatPath);
            Assert.IsFalse(none.ShouldExit(1000), "No limit: never exits on its own");
        }
    }
}
