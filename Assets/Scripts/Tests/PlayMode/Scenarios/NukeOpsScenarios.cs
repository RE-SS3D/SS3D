using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Combat;
using SS3D.Systems.Comms;
using SS3D.Systems.Examine;
using SS3D.Systems.Furniture;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.Gamemodes.Uplink;
using SS3D.Systems.Health;
using SS3D.Interactions;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.Roles;
using SS3D.Systems.Rounds;
using SS3D.Systems.ScreenEffects;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.SpawnPoints;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    // Guards: a Nuclear Emergency round draws the opted-in host as an operative who spawns on the marker with
    // the kit, briefing and team objective (effort 2026-11_round-end-loop P2), and both §6 outcomes are reachable
    // (P3, Decisions 17 and 19: detonation → operatives win with the station destroyed; squad killed → crew win
    // at once), and the device loop works through real interactions (MVP1 M3: load the disk, arm with a
    // station-wide announcement, examine shows the state, screwdriver then wirecutters defuse, the countdown
    // detonates) · Not cheaper because: round flow is the subject (testing philosophy rule 3): the draw, spawn,
    // gamemode init, death swap and outcome only meet in a live round on a networked host
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.NukeOps)]
    public class NukeOpsScenarios : ScenarioFixture
    {
        private static readonly List<string> Briefings = new();
        private static readonly List<string> Announcements = new();
        private static CommsSubSystem _briefingSource;

        protected override string Mode => "Nuclear Emergency";

        protected override bool FreshBodyPerScenario => false;

        protected override void BeforeRoundStart()
        {
            SubSystems.Get<RoleSubSystem>().OptIntoAntagonistForTesting(ServerHelpers.LocalCkey, AntagonistCategories.NuclearOperative);
            Briefings.Clear();
            Announcements.Clear();
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
            if (message.Kind == CommsChannelKind.Announcement)
                Announcements.Add(message.Text);
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
            Assert.IsEmpty(Object.FindObjectsByType<SyndicateBombBeacon>(FindObjectsSortMode.None),
                "The squad's one bomb beacon is bought on the uplink, not issued");
            SS3D.UI.MachineInterface.OperativeUplinkController[] uplinks =
                Object.FindObjectsByType<SS3D.UI.MachineInterface.OperativeUplinkController>(FindObjectsSortMode.None);
            Assert.AreEqual(1, uplinks.Length, "The operative carries a telecrystal uplink");
            Assert.IsNotNull(uplinks[0].GetComponent<Item>().Container, "The uplink fits in the operative's bag");

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

            Nuke.SetCountdownSecondsForTesting(10);
            try
            {
                yield return LoadDiskAndArm(nuke);
            }
            finally
            {
                Nuke.SetCountdownSecondsForTesting(null);
            }

            RoundOutcome outcome = null;
            yield return WaitFor(() => (outcome = CurrentOutcome()) != null, "the countdown runs out and ends the round", 15f);

            Assert.AreEqual(RoundWinner.Operatives, outcome.Winner);
            Assert.IsTrue(outcome.StationDestroyed, "The summary says the station was destroyed");
            Assert.IsTrue(outcome.Objectives.Length == 1 && outcome.Objectives[0].Result == ObjectiveResult.Met,
                "The squad's team objective is met");
            Assert.IsTrue(outcome.Players.Any(player => player.Ckey == ServerHelpers.LocalCkey && player.Antagonist),
                "The host is listed in the antagonist roster");
            Assert.IsTrue(StationDetonationPresentation.IsPlaying, "The host's client plays the white-out");
        }

        [UnityTest]
        public IEnumerator NukeLoop_LoadArmAnnounceExamineDefuse()
        {
            // The body leaves the marker and loses its gun; the next scenario reboots.
            ChangesRoom = true;
            Nuke nuke = Object.FindFirstObjectByType<Nuke>();
            Assert.IsNotNull(nuke, "The test room has a nuclear device");
            StringAssert.Contains("not armed", Examine(nuke));
            StringAssert.Contains("slot is", Examine(nuke), "An empty slot reads as empty");

            Announcements.Clear();
            yield return LoadDiskAndArm(nuke);
            Assert.Greater(nuke.Remaining, Nuke.CountdownSeconds - 10f, "The countdown just started");
            yield return WaitFor(() => Announcements.Any(text => text.Contains("nuclear device has been armed")),
                "the station-wide arm announcement", 5f);
            StringAssert.Contains("Armed", Examine(nuke));
            StringAssert.Contains("loaded", Examine(nuke));

            Hand hand = Body.GetComponent<Hands>().SelectedHand;
            Assert.IsNotNull(Run(nuke, "NukeCutWires"), "Nothing to cut without a tool");

            // Pulling the disk does not stop the clock.
            Assert.IsNull(Run(nuke, "NukeEjectDisk"), "An empty hand ejects the disk");
            yield return WaitFor(() => hand.ItemInHand is NukeCard, "the disk lands in the hand", 3f);
            Assert.IsFalse(nuke.State.DiskLoaded);
            Assert.IsTrue(nuke.State.Armed, "The clock keeps running without the disk");
            hand.ItemInHand.Delete();

            Item screwdriver = SubSystems.Get<ItemSubSystem>().SpawnItem(Data.Generated.Items.ScrewdriverBlue, Body.transform.position, Quaternion.identity);
            Assert.IsTrue(hand.Container.AddItem(screwdriver), "The screwdriver goes in the active hand");
            yield return Hold(0.3f);
            Assert.IsNull(Run(nuke, "NukeOpenPanel"), "The screwdriver opens the panel");
            yield return WaitFor(() => nuke.State.PanelOpen, "the wiring panel opens", Nuke.OpenPanelSeconds + 2f);
            StringAssert.Contains("panel is", Examine(nuke));
            screwdriver.Delete();

            Item cutters = SubSystems.Get<ItemSubSystem>().SpawnItem(Data.Generated.Items.Wirecutters, Body.transform.position, Quaternion.identity);
            Assert.IsTrue(hand.Container.AddItem(cutters), "The wirecutters go in the active hand");
            yield return Hold(0.3f);
            Assert.IsNull(Run(nuke, "NukeCutWires"), "The wirecutters cut the wires");
            yield return WaitFor(() => !nuke.State.Armed, "the device is defused", Nuke.CutWiresSeconds + 2f);
            Assert.IsNull(CurrentOutcome(), "A defuse does not end the round");
            StringAssert.Contains("not armed", Examine(nuke));
            cutters.Delete();
        }

        // The operative uplink (antagonist-content.md §4): opens from hand, one click buys into the free hand, a full
        // pair of hands drops the item at the feet, and the squad's one bomb sells out with no TC spent.
        [UnityTest]
        public IEnumerator Uplink_OpensFromHand_BuysDeliversAndRefusesSoldOut()
        {
            // The body loses its gun and gains items; the next scenario reboots.
            ChangesRoom = true;
            SS3D.UI.MachineInterface.OperativeUplinkController uplink =
                Object.FindFirstObjectByType<SS3D.UI.MachineInterface.OperativeUplinkController>();
            Assert.IsNotNull(uplink, "The operative carries an uplink");

            EmptyHands(Body);
            Hands hands = Body.GetComponent<Hands>();
            Hand hand = hands.SelectedHand;
            Assert.IsTrue(hand.Container.AddItem(uplink.GetComponent<Item>()), "The uplink comes out of the bag into the hand");
            yield return Hold(0.3f);

            SS3D.UI.MachineInterface.MachineInterfaceSubSystem panels = SubSystems.Get<SS3D.UI.MachineInterface.MachineInterfaceSubSystem>();
            Assert.IsNull(Run(uplink.gameObject, "OpenUplink"), "Use in hand opens the uplink");
            yield return WaitFor(() => panels.IsOpen, "the uplink panel opens", 3f);

            int start = uplink.Account.Balance;
            Assert.AreEqual(OperativeUplinkCatalog.StartingBalance, start);
            Hand other = hands.PlayerHands.First(h => h != hand);

            Assert.AreEqual(UplinkRefusal.None, uplink.ServerBuy(Index("c4"), hands.Owner), "C4 is affordable");
            Assert.AreEqual(start - 2, uplink.Account.Balance);
            Assert.IsNotNull(other.ItemInHand, "The C4 lands in the free hand");
            Assert.AreEqual(1, uplink.Account.Ledger.Count);

            Item[] before = Object.FindObjectsByType<Item>(FindObjectsSortMode.None);
            Assert.AreEqual(UplinkRefusal.None, uplink.ServerBuy(Index("gauze"), hands.Owner), "Gauze with both hands full");
            yield return Hold(0.2f);
            Item dropped = Object.FindObjectsByType<Item>(FindObjectsSortMode.None).Except(before).SingleOrDefault();
            Assert.IsNotNull(dropped, "The gauze spawns");
            Assert.IsNull(dropped.Container, "Both hands are full, so it lands at the feet");
            Assert.Less(Vector3.Distance(dropped.transform.position, Body.transform.position), 1.5f, "At the operative's feet");

            Assert.AreEqual(UplinkRefusal.None, uplink.ServerBuy(Index("synbomb"), hands.Owner), "The squad's one bomb");
            int afterBomb = uplink.Account.Balance;
            Assert.AreEqual(UplinkRefusal.SoldOut, uplink.ServerBuy(Index("synbomb"), hands.Owner), "Only one per squad");
            Assert.AreEqual(afterBomb, uplink.Account.Balance, "A refusal spends nothing");
            Assert.AreEqual(3, uplink.Account.Ledger.Count);

            panels.Close();
            yield return WaitFor(() => !panels.IsOpen, "the uplink panel closes", 3f);
        }

        private static int Index(string listingId)
        {
            for (int i = 0; i < OperativeUplinkCatalog.Listings.Count; i++)
                if (OperativeUplinkCatalog.Listings[i].Id == listingId) return i;
            throw new System.ArgumentException(listingId);
        }

        private string Run(GameObject target, string genericName) =>
            Body.GetComponent<InteractionController>()
                .RunInteractionForTesting(target, target.transform.position, genericName, IntentType.Help);

        // Down is not out (health.md §4): the crew-win check needs operatives dead, so a Downed or Critical one keeps the
        // round going and can still be rescued by the squad.
        [UnityTest]
        public IEnumerator SquadDownedThenCritical_RoundGoesOn()
        {
            ChangesRoom = true;
            HumanHealthController health = Body.GetComponent<HumanHealthController>();
            BodyZone[] centerMass = { BodyZone.Chest, BodyZone.Groin };
            int rounds = 0;
            while (health.Snapshot.State == HealthState.Healthy && rounds < 40)
            {
                // Two rounds, then let the pain become shock, so the operative is caught Downed before Critical.
                for (int i = 0; i < 2; i++, rounds++)
                {
                    CombatInteractionNetwork.ServerApplyLivingHit(RangedWeaponProfile.M4, health, centerMass[rounds % 2]);
                }

                yield return WaitFor(() => health.Snapshot.IncomingPain <= 0f, "the pain becomes shock", HealthConstants.ShockBuildSeconds + 2f);
            }

            Assert.AreEqual(HealthState.Downed, health.Snapshot.State, $"M4 rounds put the operative down ({rounds} rounds)");
            yield return Hold(2f);
            Assert.IsNull(CurrentOutcome(), "A Downed operative does not end the round");

            while (health.Snapshot.State == HealthState.Downed && rounds < 80)
            {
                CombatInteractionNetwork.ServerApplyLivingHit(RangedWeaponProfile.M4, health, centerMass[rounds++ % 2]);
                yield return Hold(0.2f);
            }

            yield return WaitFor(() => health.Snapshot.State == HealthState.Critical, "more rounds make the operative critical",
                HealthConstants.ShockBuildSeconds + 2f);
            yield return Hold(2f);
            Assert.IsNull(CurrentOutcome(), "A Critical operative does not end the round either");
            yield return HealFully();
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

        /// <summary>Puts the host beside the device with the disk in hand, loads it and arms with an empty hand.</summary>
        private IEnumerator LoadDiskAndArm(Nuke nuke)
        {
            EmptyHands(Body);
            Vector3 device = nuke.transform.position;
            // MainRoom is tile x/y, which is world x/z.
            Vector2 room = ScenarioAtmos.MainRoom.center;
            Vector3 toRoom = new(room.x - device.x, 0f, room.y - device.z);
            Vector3 beside = device + (toRoom.sqrMagnitude > 0.01f ? toRoom.normalized : Vector3.forward);
            Teleport(Body, new Vector3(beside.x, Body.transform.position.y, beside.z));
            yield return Console.Expect("disk me", "Disk moved");
            NukeCard disk = Object.FindFirstObjectByType<NukeCard>();
            Hand hand = Body.GetComponent<Hands>().SelectedHand;
            Assert.IsTrue(hand.Container.AddItem(disk), "The disk goes in the active hand");
            yield return Hold(0.3f);

            Assert.IsNotNull(Run(nuke, "NukeArm"), "No arming before the disk is loaded");
            Assert.IsNull(Run(nuke, "NukeLoadDisk"), "The held disk loads into the device");
            yield return WaitFor(() => nuke.State.DiskLoaded, "the disk is seated", Nuke.LoadDiskSeconds + 2f);
            Assert.IsNull(hand.ItemInHand, "The disk left the hand");

            Assert.IsNull(Run(nuke, "NukeArm"), "An empty hand arms the loaded device");
            yield return WaitFor(() => nuke.State.Armed, "the device arms", Nuke.ArmSeconds + 2f);
        }

        private string Run(Nuke nuke, string genericName) =>
            Body.GetComponent<InteractionController>()
                .RunInteractionForTesting(nuke.gameObject, nuke.transform.position, genericName, IntentType.Help);

        /// <summary>The device's examine lines as any client would read them.</summary>
        private static string Examine(Nuke nuke)
        {
            List<ExamineSection> sections = new();
            nuke.AppendSections(null, sections);
            return string.Join(" ", sections.Select(section => section.Text));
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
