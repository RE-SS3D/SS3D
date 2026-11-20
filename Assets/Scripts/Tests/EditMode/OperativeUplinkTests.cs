using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Serializing;
using NUnit.Framework;
using ObjectAssetReference = SS3D.Data.AssetDatabases.ObjectAssetReference;
using SS3D.Systems.Gamemodes.Uplink;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Roles;
using SS3D.Tests;
using SS3D.UI.MachineInterface;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// The operative uplink (antagonist-content.md §4, §6 later pass; Claude Design "Operative Uplink" states 1–7b):
    /// personal balance, one-click buy, squad-wide stock, server refusals that never spend TC, the never-trimmed
    /// ledger, and the snapshot round trip the panel reads.
    /// </summary>
    public class OperativeUplinkTests : EditModeTest
    {
        private const int Round = 7;

        private static UplinkListing Get(string id) => OperativeUplinkCatalog.Listings.First(l => l.Id == id);

        [Test]
        public void Catalog_HasUniqueIds_KnownCategories_AndPositivePrices()
        {
            HashSet<string> ids = new();
            foreach (UplinkListing listing in OperativeUplinkCatalog.Listings)
            {
                Assert.IsTrue(ids.Add(listing.Id), "duplicate id " + listing.Id);
                CollectionAssert.Contains(OperativeUplinkCatalog.Categories, listing.Category, listing.Id);
                Assert.Greater(listing.Price, 0, listing.Id);
                Assert.IsNotEmpty(listing.ItemNames, listing.Id);
            }

            Assert.LessOrEqual(OperativeUplinkCatalog.Listings.Count, OperativeUplinkSnapshot.MaxListings);
        }

        [Test]
        public void SyndicateBomb_IsOnePerSquad()
        {
            UplinkListing bomb = Get("synbomb");
            Assert.IsTrue(bomb.HasSquadStock);
            Assert.AreEqual(1, bomb.SquadStock);
            Assert.AreEqual("One per squad", bomb.Restriction);
        }

        // State 5: one click buys, the balance drops, a ledger line appears.
        [Test]
        public void Buy_ChargesBalance_AndWritesLedger()
        {
            UplinkAccount account = new();
            UplinkListing smg = Get("c20r");

            UplinkRefusal refusal = account.TryBuy(smg, new UplinkSquadStock(), Round, holderDowned: false, held: true, roundSeconds: 872);

            Assert.AreEqual(UplinkRefusal.None, refusal);
            Assert.AreEqual(OperativeUplinkCatalog.StartingBalance - smg.Price, account.Balance);
            Assert.AreEqual(smg.Price, account.Spent);
            Assert.AreEqual(1, account.Ledger.Count);
            UplinkLedgerEntry line = account.Ledger[0];
            Assert.AreEqual(1, line.Number);
            Assert.AreEqual(872, line.RoundSeconds);
            Assert.AreEqual(smg.Name, line.ItemName);
            Assert.AreEqual(smg.Price, line.Cost);
            Assert.AreEqual(account.Balance, line.BalanceAfter);
        }

        // State 3: unaffordable.
        [Test]
        public void Buy_WithTooFewTelecrystals_IsRefused_AndSpendsNothing()
        {
            UplinkAccount account = new(startingBalance: 5);

            UplinkRefusal refusal = account.TryBuy(Get("c20r"), new UplinkSquadStock(), Round, false, true, 0);

            Assert.AreEqual(UplinkRefusal.NotEnoughTelecrystals, refusal);
            Assert.AreEqual(5, account.Balance);
            Assert.IsEmpty(account.Ledger);
        }

        // State 6: everything spent.
        [Test]
        public void Buy_SpendingEverything_LeavesZero_AndRefusesMore()
        {
            UplinkAccount account = new(startingBalance: 4);
            UplinkSquadStock stock = new();
            UplinkListing c4 = Get("c4");

            Assert.AreEqual(UplinkRefusal.None, account.TryBuy(c4, stock, Round, false, true, 1));
            Assert.AreEqual(UplinkRefusal.None, account.TryBuy(c4, stock, Round, false, true, 2));
            Assert.AreEqual(0, account.Balance);
            Assert.AreEqual(UplinkRefusal.NotEnoughTelecrystals, account.TryBuy(Get("gauze"), stock, Round, false, true, 3));
            Assert.AreEqual(2, account.Ledger.Count);
        }

        // State 7a: the card said 1 left, but a squadmate's uplink got there first.
        [Test]
        public void SquadStock_IsShared_SoASquadmatesPurchaseSellsOut()
        {
            UplinkSquadStock stock = new();
            UplinkAccount first = new();
            UplinkAccount second = new();
            UplinkListing bomb = Get("synbomb");

            Assert.AreEqual(UplinkRefusal.None, first.TryBuy(bomb, stock, Round, false, true, 10));
            UplinkRefusal refusal = second.TryBuy(bomb, stock, Round, false, true, 11);

            Assert.AreEqual(UplinkRefusal.SoldOut, refusal);
            Assert.AreEqual(OperativeUplinkCatalog.StartingBalance, second.Balance);
            Assert.IsEmpty(second.Ledger);
            Assert.AreEqual(0, stock.Remaining(bomb, Round));
        }

        [Test]
        public void SquadStock_ResetsOnANewRound()
        {
            UplinkSquadStock stock = new();
            UplinkListing bomb = Get("synbomb");
            Assert.IsTrue(stock.TryTake(bomb, Round));

            Assert.AreEqual(1, stock.Remaining(bomb, Round + 1));
        }

        [Test]
        public void SquadStock_IgnoresUnlimitedListings()
        {
            UplinkSquadStock stock = new();
            UplinkListing c4 = Get("c4");
            for (int i = 0; i < 10; i++) Assert.IsTrue(stock.TryTake(c4, Round));
            Assert.AreEqual(UplinkListing.Unlimited, stock.Remaining(c4, Round));
        }

        // State 7b: downed with the panel open.
        [Test]
        public void Buy_WhileDowned_IsRefused_BeforeAnythingElse()
        {
            UplinkAccount account = new(startingBalance: 0);

            UplinkRefusal refusal = account.TryBuy(Get("c20r"), new UplinkSquadStock(), Round, holderDowned: true, held: false, 0);

            Assert.AreEqual(UplinkRefusal.Downed, refusal);
        }

        [Test]
        public void Buy_WithTheDeviceOutOfHand_IsRefused()
        {
            UplinkAccount account = new();
            Assert.AreEqual(UplinkRefusal.NotHeld, account.TryBuy(Get("c4"), new UplinkSquadStock(), Round, false, held: false, 0));
            Assert.AreEqual(OperativeUplinkCatalog.StartingBalance, account.Balance);
        }

        [Test]
        public void Refund_UndoesTheLastPurchase_AndReturnsStock()
        {
            UplinkSquadStock stock = new();
            UplinkAccount account = new();
            UplinkListing bomb = Get("synbomb");
            account.TryBuy(bomb, stock, Round, false, true, 5);

            account.Refund(bomb, stock, Round);

            Assert.AreEqual(OperativeUplinkCatalog.StartingBalance, account.Balance);
            Assert.IsEmpty(account.Ledger);
            Assert.AreEqual(1, stock.Remaining(bomb, Round));
        }

        [Test]
        public void Explain_SaysNoTelecrystalsWereSpent()
        {
            foreach (UplinkRefusal refusal in new[] { UplinkRefusal.Downed, UplinkRefusal.SoldOut, UplinkRefusal.NotHeld, UplinkRefusal.NotEnoughTelecrystals })
            {
                StringAssert.EndsWith("No TC spent.", UplinkAccount.Explain(refusal, Get("synbomb")));
            }

            Assert.AreEqual(string.Empty, UplinkAccount.Explain(UplinkRefusal.None, null));
        }

        [Test]
        public void FormatClock_IsHoursMinutesSeconds()
        {
            Assert.AreEqual("00:14:32", UplinkAccount.FormatClock(14 * 60 + 32));
            Assert.AreEqual("01:00:05", UplinkAccount.FormatClock(3605));
            Assert.AreEqual("00:00:00", UplinkAccount.FormatClock(-3));
        }

        [Test]
        public void Serializer_RoundTrips()
        {
            OperativeUplinkSnapshot original = new()
            {
                MachineObjectId = 42,
                InterfaceId = MachineInterfaceIds.OperativeUplink,
                ModelLabel = "SYN-UL",
                OwnerLine = "Operative Kovač · Syndicate field uplink",
                Balance = 26,
                StartingBalance = 40,
                Categories = new[] { "Weapons", "Explosives" },
                Listings = new[]
                {
                    new UplinkListingSnapshot
                    {
                        Name = "Syndicate bomb", Category = "Explosives", Price = 11, Kind = "Deployed", Description = "Big",
                        Includes = "Beacon", Restriction = "One per squad", Remaining = 1, Bundle = true,
                    },
                },
                Ledger = new[] { new UplinkLedgerSnapshot { Number = 1, RoundSeconds = 99, ItemName = "Bulldog", Cost = 14, BalanceAfter = 26 } },
                RefusalSequence = 3,
                RefusalText = "No TC spent.",
                DeliverySequence = 2,
                DeliveryName = "C4 charge",
                DeliveryListingIndex = 2,
                DeliveredToHand = true,
            };

            using PooledWriter writer = WriterPool.Retrieve();
            writer.WriteOperativeUplinkSnapshot(original);
            ArraySegment<byte> segment = writer.GetArraySegment();
            using PooledReader reader = ReaderPool.Retrieve(segment, null);
            OperativeUplinkSnapshot copy = reader.ReadOperativeUplinkSnapshot();

            Assert.AreEqual(original.MachineObjectId, copy.MachineObjectId);
            Assert.AreEqual(original.InterfaceId, copy.InterfaceId);
            Assert.AreEqual(original.OwnerLine, copy.OwnerLine);
            Assert.AreEqual(original.Balance, copy.Balance);
            Assert.AreEqual(original.StartingBalance, copy.StartingBalance);
            CollectionAssert.AreEqual(original.Categories, copy.Categories);
            Assert.AreEqual(1, copy.Listings.Length);
            Assert.AreEqual("Syndicate bomb", copy.Listings[0].Name);
            Assert.AreEqual(1, copy.Listings[0].Remaining);
            Assert.AreEqual("One per squad", copy.Listings[0].Restriction);
            Assert.IsTrue(copy.Listings[0].Bundle);
            Assert.AreEqual(1, copy.Ledger.Length);
            Assert.AreEqual(26, copy.Ledger[0].BalanceAfter);
            Assert.AreEqual(3, copy.RefusalSequence);
            Assert.AreEqual(original.RefusalText, copy.RefusalText);
            Assert.AreEqual(2, copy.DeliverySequence);
            Assert.AreEqual("C4 charge", copy.DeliveryName);
            Assert.AreEqual(2, copy.DeliveryListingIndex);
            Assert.IsTrue(copy.DeliveredToHand);
        }

        [Test]
        public void Mapper_MarksAffordability_StockAndLedger()
        {
            OperativeUplinkSnapshot snapshot = new()
            {
                InterfaceId = MachineInterfaceIds.OperativeUplink,
                Balance = 3,
                StartingBalance = 40,
                Listings = new[]
                {
                    new UplinkListingSnapshot { Name = "A", Category = "Weapons", Price = 14, Remaining = UplinkListingSnapshot.Unlimited },
                    new UplinkListingSnapshot { Name = "B", Category = "Explosives", Price = 2, Remaining = 0, Restriction = "One per squad" },
                    new UplinkListingSnapshot { Name = "C", Category = "Explosives", Price = 2, Remaining = UplinkListingSnapshot.Unlimited },
                },
                Categories = new[] { "Weapons", "Explosives" },
                Ledger = new[] { new UplinkLedgerSnapshot { Number = 1, RoundSeconds = 60, ItemName = "X", Cost = 37, BalanceAfter = 3 } },
            };

            OperativeUplinkViewModel model = OperativeUplinkSnapshotMapper.ToViewModel(snapshot);

            Assert.AreEqual(37, model.Spent);
            Assert.AreEqual(3, model.Listings.Count);
            Assert.AreEqual(11, model.Listings[0].Shortfall);
            Assert.IsFalse(model.Listings[0].CanBuy);
            Assert.IsTrue(model.Listings[1].OutOfStock);
            Assert.IsFalse(model.Listings[1].CanBuy);
            Assert.IsTrue(model.Listings[2].CanBuy);
            Assert.AreEqual(1, model.Ledger.Count);
            Assert.AreEqual("00:01:00", model.Ledger[0].Time);
        }

        /// <summary>Content: every listing's items exist as prefabs; the uplink prefab and kit are built.</summary>
        [Test]
        public void Content_ListingItemsExist_AndEveryOperativeCarriesAnUplink()
        {
            HashSet<string> prefabNames = new();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Content/WorldObjects/Items" }))
            {
                prefabNames.Add(System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)));
            }

            foreach (UplinkListing listing in OperativeUplinkCatalog.Listings)
            {
                foreach (string itemName in listing.ItemNames)
                {
                    Assert.IsTrue(prefabNames.Contains(itemName), $"{listing.Id}: no item prefab named {itemName}");
                }
            }

            GameObject uplink = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Content/WorldObjects/Items/Weapons/OperativeUplink.prefab");
            Assert.IsNotNull(uplink, "Run SS3D → Structural Damage → Run Content Prefab Recipes");
            Assert.IsNotNull(uplink.GetComponent<OperativeUplinkController>());
            Item item = uplink.GetComponent<Item>();
            Assert.IsNotNull(item.Asset, "The uplink is a registered item");

            RoleLoadout operative = AssetDatabase.LoadAssetAtPath<RoleLoadout>(
                "Assets/Content/Data/Roles/Loadouts/OperativeLoadout.asset");
            Assert.AreEqual(1, operative.EquipmentContents
                .Where(c => c.Slot == ContainerType.Bag)
                .SelectMany(c => c.Items)
                .Count(r => r == item.Asset), "Run SS3D → Roles → Apply Job Kits");

            // The uplink replaces the fixed kit (antagonist-content.md §6): nothing it sells but the sidearm and
            // the survival gear is issued.
            Assert.IsNull(operative.HandRightAsset, "No rifle in hand: the C-20r is bought");
            string[] bought = { "C20R", "C4", "SyndicateMiniBomb", "SyndicateBombBeacon" };
            foreach (ObjectAssetReference reference in operative.EquipmentContents.Concat(operative.FirstHolderContents)
                         .SelectMany(c => c.Items))
                CollectionAssert.DoesNotContain(bought, reference.name, "Run SS3D → Roles → Apply Job Kits");
        }
    }
}
