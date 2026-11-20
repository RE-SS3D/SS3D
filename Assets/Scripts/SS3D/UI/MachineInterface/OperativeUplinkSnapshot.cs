namespace SS3D.UI.MachineInterface
{
    /// <summary>One catalog card as the server sends it, with the squad's current stock.</summary>
    public struct UplinkListingSnapshot
    {
        /// <summary><see cref="Remaining"/> for a listing with no squad limit.</summary>
        public const int Unlimited = -1;

        public string Name;

        public string Category;

        public int Price;

        public string Kind;

        public string Description;

        public string Includes;

        public string Restriction;

        /// <summary>Squad-wide stock left, or <see cref="Unlimited"/>.</summary>
        public int Remaining;

        public bool Bundle;

        /// <summary>Asset id of the listing's first item, for its icon; the client needs no catalog.</summary>
        public string IconItemId;
    }

    public struct UplinkLedgerSnapshot
    {
        public int Number;

        public int RoundSeconds;

        public string ItemName;

        public int Cost;

        public int BalanceAfter;
    }

    /// <summary>
    /// The operative uplink panel (Claude Design "Operative Uplink"). Listing index = catalog index, which is
    /// what <see cref="MachineInterfaceControlIds.Uplink.Buy"/> carries. Refusal and delivery carry a sequence
    /// number so the panel shows each one once, however often the snapshot is refreshed.
    /// </summary>
    public struct OperativeUplinkSnapshot
    {
        public const int MaxListings = 64;

        public const int MaxCategories = 16;

        public const int MaxLedgerEntries = 255;

        public int MachineObjectId;

        public string InterfaceId;

        public string ModelLabel;

        /// <summary>"Operative Kovač · Syndicate field uplink".</summary>
        public string OwnerLine;

        public int Balance;

        public int StartingBalance;

        public string[] Categories;

        public UplinkListingSnapshot[] Listings;

        /// <summary>Oldest first, never trimmed (the serializer caps the wire at <see cref="MaxLedgerEntries"/>, newest kept).</summary>
        public UplinkLedgerSnapshot[] Ledger;

        public int RefusalSequence;

        public string RefusalText;

        public int DeliverySequence;

        public string DeliveryName;

        /// <summary>Catalog index of the last delivered listing, or -1.</summary>
        public int DeliveryListingIndex;

        /// <summary>True: put in the holder's free hand. False: both hands were full, so it landed at their feet.</summary>
        public bool DeliveredToHand;
    }
}
