using System.Collections.Generic;

namespace SS3D.Systems.Gamemodes.Uplink
{
    /// <summary>
    /// Squad-wide stock for limited listings (the squad's one Syndicate bomb). Every uplink in a round draws
    /// from the same count, so a squadmate's purchase can sell out a card another operative still shows as
    /// "1 left". Counts reset when the round id changes; server only.
    /// </summary>
    public sealed class UplinkSquadStock
    {
        /// <summary>The server's shared instance.</summary>
        public static readonly UplinkSquadStock Shared = new();

        private readonly Dictionary<string, int> _taken = new();
        private int _roundId = int.MinValue;

        /// <summary>How many of <paramref name="listing"/> the squad may still buy, or <see cref="UplinkListing.Unlimited"/>.</summary>
        public int Remaining(UplinkListing listing, int roundId)
        {
            if (listing == null || !listing.HasSquadStock)
            {
                return UplinkListing.Unlimited;
            }

            SyncRound(roundId);
            _taken.TryGetValue(listing.Id, out int taken);
            return listing.SquadStock > taken ? listing.SquadStock - taken : 0;
        }

        /// <summary>Takes one from the squad's stock; false when none are left. Unlimited listings always succeed.</summary>
        public bool TryTake(UplinkListing listing, int roundId)
        {
            if (listing == null)
            {
                return false;
            }

            if (!listing.HasSquadStock)
            {
                return true;
            }

            if (Remaining(listing, roundId) <= 0)
            {
                return false;
            }

            _taken.TryGetValue(listing.Id, out int taken);
            _taken[listing.Id] = taken + 1;
            return true;
        }

        /// <summary>Gives one back (a purchase whose delivery failed).</summary>
        public void Return(UplinkListing listing, int roundId)
        {
            if (listing == null || !listing.HasSquadStock)
            {
                return;
            }

            SyncRound(roundId);
            if (_taken.TryGetValue(listing.Id, out int taken) && taken > 0)
            {
                _taken[listing.Id] = taken - 1;
            }
        }

        private void SyncRound(int roundId)
        {
            if (roundId == _roundId)
            {
                return;
            }

            _roundId = roundId;
            _taken.Clear();
        }
    }
}
