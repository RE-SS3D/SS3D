using System.Collections.Generic;

namespace SS3D.Systems.Gamemodes.Uplink
{
    /// <summary>Why the server turned a purchase down. The client hides most of these; the server is the authority.</summary>
    public enum UplinkRefusal : byte
    {
        None = 0,
        UnknownListing = 1,
        NotEnoughTelecrystals = 2,
        SoldOut = 3,
        Downed = 4,
        NotHeld = 5,
        DeliveryFailed = 6,
    }

    /// <summary>One line of the purchase ledger. Never trimmed: telecrystals are a real, logged currency (§4).</summary>
    public readonly struct UplinkLedgerEntry
    {
        public UplinkLedgerEntry(int number, int roundSeconds, string itemName, int cost, int balanceAfter)
        {
            Number = number;
            RoundSeconds = roundSeconds;
            ItemName = itemName;
            Cost = cost;
            BalanceAfter = balanceAfter;
        }

        /// <summary>1-based purchase number on this uplink.</summary>
        public int Number { get; }

        /// <summary>Round time of the purchase, in seconds.</summary>
        public int RoundSeconds { get; }

        public string ItemName { get; }

        public int Cost { get; }

        public int BalanceAfter { get; }
    }

    /// <summary>
    /// One uplink's telecrystal balance and ledger. The balance is personal: it lives on the device, not the squad.
    /// Pure; the server adapter feeds it the holder's state and the shared <see cref="UplinkSquadStock"/>.
    /// </summary>
    public sealed class UplinkAccount
    {
        private readonly List<UplinkLedgerEntry> _ledger = new();

        public UplinkAccount(int startingBalance = UplinkCatalog.DefaultStartingBalance)
        {
            StartingBalance = startingBalance;
            Balance = startingBalance;
        }

        public int StartingBalance { get; }

        public int Balance { get; private set; }

        public int Spent => StartingBalance - Balance;

        /// <summary>Oldest first.</summary>
        public IReadOnlyList<UplinkLedgerEntry> Ledger => _ledger;

        /// <summary>
        /// The server-side checks in the order the panel explains them: a downed holder, a device that isn't in
        /// hand, then the listing, the squad's stock and the balance.
        /// </summary>
        public UplinkRefusal Check(UplinkListing listing, int squadRemaining, bool holderDowned, bool held)
        {
            if (holderDowned)
            {
                return UplinkRefusal.Downed;
            }

            if (!held)
            {
                return UplinkRefusal.NotHeld;
            }

            if (listing == null)
            {
                return UplinkRefusal.UnknownListing;
            }

            if (listing.HasSquadStock && squadRemaining <= 0)
            {
                return UplinkRefusal.SoldOut;
            }

            return Balance < listing.Price ? UplinkRefusal.NotEnoughTelecrystals : UplinkRefusal.None;
        }

        /// <summary>
        /// Checks, takes squad stock and charges the balance in one step. On <see cref="UplinkRefusal.None"/> the
        /// caller delivers; if delivery fails it calls <see cref="Refund"/>.
        /// </summary>
        public UplinkRefusal TryBuy(
            UplinkListing listing,
            UplinkSquadStock stock,
            int roundId,
            bool holderDowned,
            bool held,
            int roundSeconds)
        {
            int remaining = stock != null ? stock.Remaining(listing, roundId) : UplinkListing.Unlimited;
            UplinkRefusal refusal = Check(listing, remaining, holderDowned, held);
            if (refusal != UplinkRefusal.None)
            {
                return refusal;
            }

            if (stock != null && !stock.TryTake(listing, roundId))
            {
                return UplinkRefusal.SoldOut;
            }

            Balance -= listing.Price;
            _ledger.Add(new UplinkLedgerEntry(_ledger.Count + 1, roundSeconds, listing.Name, listing.Price, Balance));
            return UplinkRefusal.None;
        }

        /// <summary>Undoes the last purchase of <paramref name="listing"/> when nothing could be delivered.</summary>
        public void Refund(UplinkListing listing, UplinkSquadStock stock, int roundId)
        {
            if (listing == null || _ledger.Count == 0)
            {
                return;
            }

            UplinkLedgerEntry last = _ledger[_ledger.Count - 1];
            if (last.ItemName != listing.Name || last.Cost != listing.Price)
            {
                return;
            }

            _ledger.RemoveAt(_ledger.Count - 1);
            Balance += listing.Price;
            stock?.Return(listing, roundId);
        }

        /// <summary>Banner text for a refusal; every refusal says the balance did not change.</summary>
        public static string Explain(UplinkRefusal refusal, UplinkListing listing)
        {
            string name = listing != null ? listing.Name : "That item";
            return refusal switch
            {
                UplinkRefusal.Downed => "You can’t buy while downed. No TC spent.",
                UplinkRefusal.NotHeld => "Hold the uplink in your hand to buy. No TC spent.",
                UplinkRefusal.SoldOut => name + " sold out a moment ago (a squadmate bought the last one). No TC spent.",
                UplinkRefusal.NotEnoughTelecrystals => "Not enough telecrystals for " + name + ". No TC spent.",
                UplinkRefusal.DeliveryFailed => name + " could not be delivered. No TC spent.",
                UplinkRefusal.UnknownListing => "That listing is no longer offered. No TC spent.",
                _ => string.Empty,
            };
        }

        /// <summary>Ledger time, e.g. "00:14:32".</summary>
        public static string FormatClock(int seconds)
        {
            if (seconds < 0)
            {
                seconds = 0;
            }

            int hours = seconds / 3600;
            int minutes = (seconds % 3600) / 60;
            int secs = seconds % 60;
            return $"{hours:00}:{minutes:00}:{secs:00}";
        }
    }
}
