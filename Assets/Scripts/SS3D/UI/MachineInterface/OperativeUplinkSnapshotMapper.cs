using SS3D.Systems.Gamemodes.Uplink;

namespace SS3D.UI.MachineInterface
{
    public static class OperativeUplinkSnapshotMapper
    {
        public static OperativeUplinkViewModel ToViewModel(OperativeUplinkSnapshot snapshot)
        {
            OperativeUplinkViewModel model = new()
            {
                ModelLabel = snapshot.ModelLabel ?? string.Empty,
                OwnerLine = snapshot.OwnerLine ?? string.Empty,
                Balance = snapshot.Balance,
                StartingBalance = snapshot.StartingBalance,
                RefusalSequence = snapshot.RefusalSequence,
                RefusalText = snapshot.RefusalText ?? string.Empty,
                DeliverySequence = snapshot.DeliverySequence,
                DeliveryName = snapshot.DeliveryName ?? string.Empty,
                DeliveredToHand = snapshot.DeliveredToHand,
            };

            if (snapshot.Categories != null)
            {
                model.Categories.AddRange(snapshot.Categories);
            }

            if (snapshot.Listings != null)
            {
                for (int i = 0; i < snapshot.Listings.Length; i++)
                {
                    UplinkListingSnapshot listing = snapshot.Listings[i];
                    bool hasStock = listing.Remaining != UplinkListingSnapshot.Unlimited;
                    model.Listings.Add(new UplinkListingViewData
                    {
                        Index = i,
                        Name = listing.Name ?? string.Empty,
                        Category = listing.Category ?? string.Empty,
                        Price = listing.Price,
                        Kind = listing.Kind ?? string.Empty,
                        Description = listing.Description ?? string.Empty,
                        Includes = listing.Includes ?? string.Empty,
                        Restriction = listing.Restriction ?? string.Empty,
                        HasStock = hasStock,
                        Remaining = hasStock ? listing.Remaining : 0,
                        OutOfStock = hasStock && listing.Remaining <= 0,
                        Bundle = listing.Bundle,
                        Shortfall = listing.Price > snapshot.Balance ? listing.Price - snapshot.Balance : 0,
                    });
                }
            }

            if (snapshot.Ledger != null)
            {
                for (int i = snapshot.Ledger.Length - 1; i >= 0; i--)
                {
                    UplinkLedgerSnapshot entry = snapshot.Ledger[i];
                    model.Ledger.Add(new UplinkLedgerViewData
                    {
                        Number = entry.Number,
                        Time = UplinkAccount.FormatClock(entry.RoundSeconds),
                        ItemName = entry.ItemName ?? string.Empty,
                        Cost = entry.Cost,
                        BalanceAfter = entry.BalanceAfter,
                    });
                }
            }

            return model;
        }
    }
}
