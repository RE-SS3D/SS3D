using System;
using System.Collections.Generic;
using SS3D.Systems.Rounds;

namespace SS3D.Systems.Gamemodes
{
    /// <summary>One drawn operative as the rules see them, read from the world on the server.</summary>
    public readonly struct OperativeBody
    {
        public readonly string Ckey;
        /// <summary>The operative still has a body; a destroyed body (or none ever spawned) is out.</summary>
        public readonly bool HasBody;
        /// <summary>The body is dead (<c>health.md</c> §4). In this codebase a ghost always means a dead body.</summary>
        public readonly bool Dead;
        /// <summary>The player is connected. SSD bodies stay in the fight (Decision 16), so the rules ignore this.</summary>
        public readonly bool Connected;

        public OperativeBody(string ckey, bool hasBody, bool dead, bool connected = true)
        {
            Ckey = ckey;
            HasBody = hasBody;
            Dead = dead;
            Connected = connected;
        }

        /// <summary>Still in the fight: a body that is not dead. Restraint is ignored until it exists (Decision 16).</summary>
        public bool IsUp => HasBody && !Dead;
    }

    /// <summary>What the Nuke Ops rules read at a trigger: the squad and who carries the disk.</summary>
    public readonly struct NukeOpsSnapshot
    {
        public readonly IReadOnlyList<OperativeBody> Squad;
        /// <summary>Ckey of the body the disk is on, anywhere in its container tree; null when loose, stored or missing.</summary>
        public readonly string DiskCarrierCkey;

        public NukeOpsSnapshot(IReadOnlyList<OperativeBody> squad, string diskCarrierCkey)
        {
            Squad = squad ?? Array.Empty<OperativeBody>();
            DiskCarrierCkey = diskCarrierCkey;
        }
    }

    /// <summary>
    /// How a Nuclear Operatives round ends (<c>antagonist-content.md</c> §6 "How the round ends"; effort
    /// <c>2026-11_round-end-loop</c> P3, Decisions 8, 16–18). Pure: the server reads the world into a
    /// <see cref="NukeOpsSnapshot"/> and these rules pick the winner.
    /// </summary>
    public static class NukeOpsRules
    {
        public const string SquadOutReason = "Every operative is dead.";
        public const string TimerDiskHeldReason = "Time ran out with the disk in operative hands.";
        public const string TimerDiskSafeReason = "Time ran out with the disk still on the station.";
        public const string AdminDiskHeldReason = "Ended by an admin with the disk in operative hands.";
        public const string AdminDiskSafeReason = "Ended by an admin with the disk still on the station.";

        /// <summary>
        /// Crew win the moment every drawn operative is out (§6 row 2, Decision 17). A round without a squad
        /// never ends this way.
        /// </summary>
        public static bool TryCrewWin(NukeOpsSnapshot snapshot, out RoundEndRequest request)
        {
            request = default;
            if (snapshot.Squad.Count == 0)
                return false;

            foreach (OperativeBody operative in snapshot.Squad)
            {
                if (operative.IsUp)
                    return false;
            }

            request = new RoundEndRequest(RoundEndTrigger.GamemodeCondition, RoundWinner.Crew, reason: SquadOutReason);
            return true;
        }

        /// <summary>
        /// The disk counts as held only on a living operative (Decision 18). A disk on a dead operative,
        /// on a crew member, on the floor or missing is still on the station.
        /// </summary>
        public static bool DiskHeldByOperative(NukeOpsSnapshot snapshot)
        {
            if (string.IsNullOrEmpty(snapshot.DiskCarrierCkey))
                return false;

            foreach (OperativeBody operative in snapshot.Squad)
            {
                if (operative.Ckey == snapshot.DiskCarrierCkey)
                    return operative.IsUp;
            }

            return false;
        }

        /// <summary>
        /// Gives a timer or admin end its Nuke Ops winner (§6 rows 3–4; Decision 8 resolves an admin end as a
        /// timeout). Gamemode conditions (detonation, squad out) already carry theirs and pass through, as
        /// does any request in a round without a squad.
        /// </summary>
        public static RoundEndRequest Resolve(RoundEndRequest request, NukeOpsSnapshot snapshot)
        {
            if (request.Trigger == RoundEndTrigger.GamemodeCondition || snapshot.Squad.Count == 0)
                return request;

            bool held = DiskHeldByOperative(snapshot);
            bool admin = request.Trigger == RoundEndTrigger.Admin;
            string reason = held
                ? admin ? AdminDiskHeldReason : TimerDiskHeldReason
                : admin ? AdminDiskSafeReason : TimerDiskSafeReason;
            return new RoundEndRequest(request.Trigger, held ? RoundWinner.OperativesMinor : RoundWinner.Crew,
                request.StationDestroyed, reason);
        }

        /// <summary>
        /// Walks from an item up through whatever holds it (hand, pocket, bag in a bag) and returns the ckey
        /// of the first node that is a carrier, or null when the chain ends without one (floor, locker).
        /// </summary>
        /// <param name="start">The item, e.g. the disk.</param>
        /// <param name="holderOf">The item or body holding a node; null at the top of the chain.</param>
        /// <param name="carrierCkeyOf">The ckey a node carries for, or null when the node is not a carrier.</param>
        public static string CarrierCkey<TNode>(TNode start, Func<TNode, TNode> holderOf,
            Func<TNode, string> carrierCkeyOf, int maxDepth = 32) where TNode : class
        {
            TNode node = start;
            for (int depth = 0; node != null && depth <= maxDepth; depth++)
            {
                string ckey = carrierCkeyOf(node);
                if (!string.IsNullOrEmpty(ckey))
                    return ckey;

                node = holderOf(node);
            }

            return null;
        }
    }
}
