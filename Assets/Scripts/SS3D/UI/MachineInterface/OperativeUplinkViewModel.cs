using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    public sealed class UplinkListingViewData
    {
        /// <summary>Catalog index; what Buy sends.</summary>
        public int Index { get; set; }

        public string Name { get; set; }

        public string Category { get; set; }

        public int Price { get; set; }

        public string Kind { get; set; }

        public string Description { get; set; }

        public string Includes { get; set; }

        public string Restriction { get; set; }

        public bool HasStock { get; set; }

        public int Remaining { get; set; }

        public bool OutOfStock { get; set; }

        public bool Bundle { get; set; }

        /// <summary>TC missing to buy it; 0 when affordable.</summary>
        public int Shortfall { get; set; }

        public bool CanBuy => !OutOfStock && Shortfall <= 0;
    }

    public sealed class UplinkLedgerViewData
    {
        public int Number { get; set; }

        public string Time { get; set; }

        public string ItemName { get; set; }

        public int Cost { get; set; }

        public int BalanceAfter { get; set; }
    }

    public sealed class OperativeUplinkViewModel : IMachineInterfaceViewModel
    {
        public string ModelLabel { get; set; } = "SYN-UL · encrypted telecrystal uplink";

        public string OwnerLine { get; set; } = "Syndicate field uplink";

        public int Balance { get; set; }

        public int StartingBalance { get; set; }

        public int Spent => StartingBalance - Balance;

        public List<string> Categories { get; } = new();

        public List<UplinkListingViewData> Listings { get; } = new();

        /// <summary>Newest first, as the panel lists it.</summary>
        public List<UplinkLedgerViewData> Ledger { get; } = new();

        public int RefusalSequence { get; set; }

        public string RefusalText { get; set; } = string.Empty;

        public int DeliverySequence { get; set; }

        public string DeliveryName { get; set; } = string.Empty;

        public bool DeliveredToHand { get; set; }
    }
}
