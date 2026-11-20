using System;
using FishNet.Serializing;

namespace SS3D.UI.MachineInterface
{
    public static class OperativeUplinkSnapshotSerializer
    {
        public static void WriteOperativeUplinkSnapshot(this Writer writer, OperativeUplinkSnapshot snapshot)
        {
            writer.WriteInt32(snapshot.MachineObjectId);
            writer.WriteString(snapshot.InterfaceId);
            writer.WriteString(snapshot.ModelLabel);
            writer.WriteString(snapshot.OwnerLine);
            writer.WriteInt32(snapshot.Balance);
            writer.WriteInt32(snapshot.StartingBalance);

            int categoryCount = Math.Min(snapshot.Categories?.Length ?? 0, OperativeUplinkSnapshot.MaxCategories);
            writer.WriteByte((byte)categoryCount);
            for (int i = 0; i < categoryCount; i++)
            {
                writer.WriteString(snapshot.Categories[i]);
            }

            int listingCount = Math.Min(snapshot.Listings?.Length ?? 0, OperativeUplinkSnapshot.MaxListings);
            writer.WriteByte((byte)listingCount);
            for (int i = 0; i < listingCount; i++)
            {
                UplinkListingSnapshot listing = snapshot.Listings[i];
                writer.WriteString(listing.Name);
                writer.WriteString(listing.Category);
                writer.WriteInt32(listing.Price);
                writer.WriteString(listing.Kind);
                writer.WriteString(listing.Description);
                writer.WriteString(listing.Includes);
                writer.WriteString(listing.Restriction);
                writer.WriteInt32(listing.Remaining);
                writer.WriteBoolean(listing.Bundle);
                writer.WriteString(listing.IconItemId);
            }

            int ledgerLength = snapshot.Ledger?.Length ?? 0;
            int ledgerCount = Math.Min(ledgerLength, OperativeUplinkSnapshot.MaxLedgerEntries);
            int ledgerStart = ledgerLength - ledgerCount;
            writer.WriteByte((byte)ledgerCount);
            for (int i = ledgerStart; i < ledgerLength; i++)
            {
                UplinkLedgerSnapshot entry = snapshot.Ledger[i];
                writer.WriteInt32(entry.Number);
                writer.WriteInt32(entry.RoundSeconds);
                writer.WriteString(entry.ItemName);
                writer.WriteInt32(entry.Cost);
                writer.WriteInt32(entry.BalanceAfter);
            }

            writer.WriteInt32(snapshot.RefusalSequence);
            writer.WriteString(snapshot.RefusalText);
            writer.WriteInt32(snapshot.DeliverySequence);
            writer.WriteString(snapshot.DeliveryName);
            writer.WriteInt32(snapshot.DeliveryListingIndex);
            writer.WriteBoolean(snapshot.DeliveredToHand);
        }

        public static OperativeUplinkSnapshot ReadOperativeUplinkSnapshot(this Reader reader)
        {
            OperativeUplinkSnapshot snapshot = new()
            {
                MachineObjectId = reader.ReadInt32(),
                InterfaceId = reader.ReadString(),
                ModelLabel = reader.ReadString(),
                OwnerLine = reader.ReadString(),
                Balance = reader.ReadInt32(),
                StartingBalance = reader.ReadInt32(),
            };

            snapshot.Categories = new string[Math.Min((int)reader.ReadByte(), OperativeUplinkSnapshot.MaxCategories)];
            for (int i = 0; i < snapshot.Categories.Length; i++)
            {
                snapshot.Categories[i] = reader.ReadString();
            }

            snapshot.Listings = new UplinkListingSnapshot[Math.Min((int)reader.ReadByte(), OperativeUplinkSnapshot.MaxListings)];
            for (int i = 0; i < snapshot.Listings.Length; i++)
            {
                snapshot.Listings[i] = new UplinkListingSnapshot
                {
                    Name = reader.ReadString(),
                    Category = reader.ReadString(),
                    Price = reader.ReadInt32(),
                    Kind = reader.ReadString(),
                    Description = reader.ReadString(),
                    Includes = reader.ReadString(),
                    Restriction = reader.ReadString(),
                    Remaining = reader.ReadInt32(),
                    Bundle = reader.ReadBoolean(),
                    IconItemId = reader.ReadString(),
                };
            }

            snapshot.Ledger = new UplinkLedgerSnapshot[reader.ReadByte()];
            for (int i = 0; i < snapshot.Ledger.Length; i++)
            {
                snapshot.Ledger[i] = new UplinkLedgerSnapshot
                {
                    Number = reader.ReadInt32(),
                    RoundSeconds = reader.ReadInt32(),
                    ItemName = reader.ReadString(),
                    Cost = reader.ReadInt32(),
                    BalanceAfter = reader.ReadInt32(),
                };
            }

            snapshot.RefusalSequence = reader.ReadInt32();
            snapshot.RefusalText = reader.ReadString();
            snapshot.DeliverySequence = reader.ReadInt32();
            snapshot.DeliveryName = reader.ReadString();
            snapshot.DeliveryListingIndex = reader.ReadInt32();
            snapshot.DeliveredToHand = reader.ReadBoolean();
            return snapshot;
        }
    }
}
