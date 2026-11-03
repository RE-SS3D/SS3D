using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Gamemodes;
using SS3D.Systems.Rounds;

namespace EditorTests
{
    /// <summary>
    /// The Nuke Ops win/lose table (<c>antagonist-content.md</c> §6; effort <c>2026-11_round-end-loop</c> P3,
    /// Decisions 8, 16–18): every row, plus the SSD, ghosted, nested-bag and missing-disk cases.
    /// </summary>
    public class NukeOpsRulesTests
    {
        private static OperativeBody Up(string ckey) => new(ckey, hasBody: true, dead: false);
        private static OperativeBody Dead(string ckey) => new(ckey, hasBody: true, dead: true);
        private static OperativeBody Gone(string ckey) => new(ckey, hasBody: false, dead: false);
        private static OperativeBody Ssd(string ckey) => new(ckey, hasBody: true, dead: false, connected: false);

        private static NukeOpsSnapshot Snapshot(string diskCarrier, params OperativeBody[] squad) => new(squad, diskCarrier);

        // Row 1: the device detonates.
        [Test]
        public void Detonation_IsAnOperativeWin_WhateverTheSquad()
        {
            RoundEndRequest request = NukeOpsRules.Resolve(RoundEndRequest.Detonation, Snapshot(null, Dead("a")));

            Assert.AreEqual(RoundWinner.Operatives, request.Winner);
            Assert.IsTrue(request.StationDestroyed);
            Assert.AreEqual(RoundEndTrigger.GamemodeCondition, request.Trigger);
        }

        // Row 2: every operative is dead (restraint ignored, Decision 16).
        [Test]
        public void SquadOut_IsACrewWin()
        {
            Assert.IsTrue(NukeOpsRules.TryCrewWin(Snapshot(null, Dead("a"), Gone("b")), out RoundEndRequest request));

            Assert.AreEqual(RoundWinner.Crew, request.Winner);
            Assert.AreEqual(RoundEndTrigger.GamemodeCondition, request.Trigger);
            Assert.AreEqual(NukeOpsRules.SquadOutReason, request.Reason);
        }

        [Test]
        public void OneOperativeUp_IsNotACrewWin()
        {
            Assert.IsFalse(NukeOpsRules.TryCrewWin(Snapshot(null, Dead("a"), Up("b")), out _));
        }

        [Test]
        public void SsdOperative_StillCounts()
        {
            Assert.IsFalse(NukeOpsRules.TryCrewWin(Snapshot(null, Dead("a"), Ssd("b")), out _));
            Assert.AreEqual(RoundWinner.OperativesMinor,
                NukeOpsRules.Resolve(RoundEndRequest.Timer, Snapshot("b", Ssd("b"))).Winner);
        }

        // In this codebase a ghost is what a body leaves when it dies; the adapter reports it as a dead body.
        [Test]
        public void GhostedOperative_IsOut()
        {
            Assert.IsTrue(NukeOpsRules.TryCrewWin(Snapshot(null, Dead("a")), out _));
        }

        [Test]
        public void NoSquad_NeverEndsByCrewWin()
        {
            Assert.IsFalse(NukeOpsRules.TryCrewWin(Snapshot(null), out _));
        }

        // Row 3: the timer runs out with the disk still on the station.
        [Test]
        public void Timeout_DiskOnStation_IsACrewWin()
        {
            RoundEndRequest request = NukeOpsRules.Resolve(RoundEndRequest.Timer, Snapshot(null, Up("a")));

            Assert.AreEqual(RoundWinner.Crew, request.Winner);
            Assert.AreEqual(RoundEndTrigger.Timer, request.Trigger);
            Assert.AreEqual(NukeOpsRules.TimerDiskSafeReason, request.Reason);
        }

        // Row 4: the timer runs out with the disk held by an operative.
        [Test]
        public void Timeout_DiskOnLivingOperative_IsAMinorOperativeWin()
        {
            RoundEndRequest request = NukeOpsRules.Resolve(RoundEndRequest.Timer, Snapshot("b", Dead("a"), Up("b")));

            Assert.AreEqual(RoundWinner.OperativesMinor, request.Winner);
            Assert.AreEqual(NukeOpsRules.TimerDiskHeldReason, request.Reason);
        }

        [Test]
        public void Timeout_DiskOnDeadOperative_IsACrewWin()
        {
            Assert.AreEqual(RoundWinner.Crew, NukeOpsRules.Resolve(RoundEndRequest.Timer, Snapshot("a", Dead("a"), Up("b"))).Winner);
        }

        [Test]
        public void Timeout_DiskOnCrewMember_IsACrewWin()
        {
            Assert.AreEqual(RoundWinner.Crew, NukeOpsRules.Resolve(RoundEndRequest.Timer, Snapshot("crew", Up("a"))).Winner);
        }

        [Test]
        public void Timeout_MissingDisk_IsACrewWin()
        {
            Assert.AreEqual(RoundWinner.Crew, NukeOpsRules.Resolve(RoundEndRequest.Timer, Snapshot(null, Up("a"))).Winner);
        }

        // Decision 8: an admin end in a Nuke Ops round resolves as a timeout.
        [Test]
        public void AdminEnd_ResolvesAsATimeout()
        {
            RoundEndRequest held = NukeOpsRules.Resolve(RoundEndRequest.Admin, Snapshot("a", Up("a")));
            RoundEndRequest safe = NukeOpsRules.Resolve(RoundEndRequest.Admin, Snapshot(null, Up("a")));

            Assert.AreEqual(RoundWinner.OperativesMinor, held.Winner);
            Assert.AreEqual(RoundEndTrigger.Admin, held.Trigger);
            Assert.AreEqual(NukeOpsRules.AdminDiskHeldReason, held.Reason);
            Assert.AreEqual(RoundWinner.Crew, safe.Winner);
            Assert.AreEqual(NukeOpsRules.AdminDiskSafeReason, safe.Reason);
        }

        [Test]
        public void NoSquad_TimerAndAdminKeepNoWinner()
        {
            Assert.AreEqual(RoundWinner.None, NukeOpsRules.Resolve(RoundEndRequest.Timer, Snapshot(null)).Winner);
            Assert.AreEqual(RoundWinner.None, NukeOpsRules.Resolve(RoundEndRequest.Admin, Snapshot(null)).Winner);
        }

        private sealed class Node
        {
            public Node Holder;
            public string Carrier;
        }

        private static string CarrierOf(Node item) => NukeOpsRules.CarrierCkey(item, n => n.Holder, n => n.Carrier);

        [Test]
        public void Carrier_InHand_IsTheBody()
        {
            Node body = new() { Carrier = "a" };
            Assert.AreEqual("a", CarrierOf(new Node { Holder = body }));
        }

        [Test]
        public void Carrier_InABagInABag_IsTheBody()
        {
            Node body = new() { Carrier = "a" };
            Node backpack = new() { Holder = body };
            Node box = new() { Holder = backpack };
            Assert.AreEqual("a", CarrierOf(new Node { Holder = box }));
        }

        [Test]
        public void Carrier_OnTheFloorOrInALocker_IsNobody()
        {
            Assert.IsNull(CarrierOf(new Node()));
            Assert.IsNull(CarrierOf(new Node { Holder = new Node() }));
        }

        [Test]
        public void Carrier_StopsOnACycle()
        {
            Node a = new();
            Node b = new() { Holder = a };
            a.Holder = b;
            Assert.IsNull(CarrierOf(a));
        }
    }
}
