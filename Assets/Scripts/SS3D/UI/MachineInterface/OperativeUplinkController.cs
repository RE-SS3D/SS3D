using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Combat;
using SS3D.Systems.Entities;
using SS3D.Systems.Gamemodes.Uplink;
using SS3D.Systems.Health;
using SS3D.Systems.IngameConsoleSystem;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Rounds;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// The Nuclear Operative's handheld telecrystal uplink (antagonist-content.md §4, §6 later pass; Claude Design
    /// "Operative Uplink"). Use it in hand to open the panel. The balance and ledger live on the device and are
    /// not shared with the squad; limited listings draw from the squad-wide <see cref="UplinkSquadStock"/>.
    /// Purchases materialize the item at once (§4's one deliberate exception): into the holder's free hand, else
    /// at their feet. Whoever holds the device can use it ("physical possession is the gate", §4).
    /// </summary>
    [RequireComponent(typeof(Item))]
    public sealed class OperativeUplinkController : MachineInterfaceBehaviour
    {
        [SerializeField]
        private string _modelLabel = "SYN-UL · encrypted telecrystal uplink";

        [SerializeField]
        [Tooltip("Telecrystals the device starts with.")]
        private int _startingBalance = OperativeUplinkCatalog.StartingBalance;

        private UplinkAccount _account;
        private string _ownerName;
        private int _refusalSequence;
        private string _refusalText = string.Empty;
        private int _deliverySequence;
        private string _deliveryName = string.Empty;
        private bool _deliveredToHand;

        private static readonly Dictionary<string, string> ResolvedItemIds = new();

        public override string InterfaceId => MachineInterfaceIds.OperativeUplink;

        /// <summary>Server: the device's balance and ledger.</summary>
        public UplinkAccount Account => _account ??= new UplinkAccount(_startingBalance);

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new OpenUplinkInteraction(this) };
        }

        protected override void SendOpenToViewer(NetworkConnection conn)
        {
            TargetOpenInterface(conn, BuildSnapshot());
        }

        protected override void SendRefreshToViewer(NetworkConnection conn)
        {
            TargetRefreshInterface(conn, BuildSnapshot());
        }

        protected override bool ApplyControl(byte controlId, bool value)
        {
            return false;
        }

        protected override bool ApplyActionControl(byte controlId, int value)
        {
            if (controlId != MachineInterfaceControlIds.Uplink.Buy)
            {
                return false;
            }

            ServerBuy(value, LastControlConnection);
            return true;
        }

        /// <summary>
        /// Server: buys catalog listing <paramref name="listingIndex"/> for <paramref name="buyer"/>. Returns the
        /// refusal (None on success). Every viewer is refreshed either way, so a stale card corrects itself.
        /// </summary>
        [Server]
        public UplinkRefusal ServerBuy(int listingIndex, NetworkConnection buyer)
        {
            UplinkListing listing = OperativeUplinkCatalog.At(listingIndex);
            Hand hand = HolderHand();
            bool held = hand != null && (buyer == null || hand.HandsController == null || hand.HandsController.Owner == buyer);
            int roundId = CurrentRoundId(out int roundSeconds);

            UplinkRefusal refusal = Account.TryBuy(
                listing,
                UplinkSquadStock.Shared,
                roundId,
                IsDowned(hand),
                held,
                roundSeconds);

            if (refusal == UplinkRefusal.None && !Deliver(listing, hand))
            {
                Account.Refund(listing, UplinkSquadStock.Shared, roundId);
                refusal = UplinkRefusal.DeliveryFailed;
            }

            if (refusal != UplinkRefusal.None)
            {
                _refusalSequence++;
                _refusalText = UplinkAccount.Explain(refusal, listing);
            }
            else
            {
                // A purchase clears an earlier refusal.
                _refusalText = string.Empty;
            }

            RefreshAllViewers();
            return refusal;
        }

        [TargetRpc(RunLocally = true)]
        private void TargetOpenInterface(NetworkConnection conn, OperativeUplinkSnapshot snapshot)
        {
            DispatchClientOpen(conn, snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, OperativeUplinkSnapshot snapshot)
        {
            DispatchClientRefresh(conn, snapshot);
        }

        /// <summary>The hand holding this device, or null.</summary>
        private Hand HolderHand()
        {
            AttachedContainer container = GetComponent<Item>().Container;
            if (container == null || container.ContainerType != ContainerType.Hand)
            {
                return null;
            }

            Hand hand = container.GetComponent<Hand>();
            return hand != null ? hand : container.GetComponentInParent<Hand>();
        }

        private static bool IsDowned(Hand hand)
        {
            if (hand == null)
            {
                return false;
            }

            HumanHealthController health = hand.GetComponentInParent<HumanHealthController>();
            if (health == null)
            {
                return false;
            }

            HealthSnapshot snapshot = health.Snapshot;
            return snapshot.State != HealthState.Healthy || !snapshot.IsConscious;
        }

        private static int CurrentRoundId(out int roundSeconds)
        {
            if (SubSystems.TryGet(out RoundSubSystem rounds))
            {
                roundSeconds = rounds.CurrentTimerSeconds;
                return rounds.RoundId;
            }

            roundSeconds = 0;
            return 0;
        }

        /// <summary>Spawns every item of <paramref name="listing"/>: the free hand first, then the holder's feet.</summary>
        private bool Deliver(UplinkListing listing, Hand holder)
        {
            if (!SubSystems.TryGet(out ItemSubSystem items) || holder == null)
            {
                return false;
            }

            Transform body = holder.HandsController != null ? holder.HandsController.transform : holder.transform;
            Entity entity = holder.GetComponentInParent<Entity>();
            if (entity != null)
            {
                body = entity.transform;
            }

            bool any = false;
            bool first = true;
            foreach (string itemName in listing.ItemNames)
            {
                if (!TryResolveItem(itemName, out Item prefab, out string assetId))
                {
                    Debug.LogWarning($"Uplink listing {listing.Id}: no item named {itemName}", this);
                    continue;
                }

                bool inHand = TryPutInFreeHand(items, holder, prefab);
                if (!inHand)
                {
                    items.SpawnItemAtRest(assetId, body.position, body.eulerAngles.y);
                }

                if (first)
                {
                    _deliveredToHand = inHand;
                    first = false;
                }

                any = true;
            }

            if (any)
            {
                _deliverySequence++;
                _deliveryName = listing.Name;
            }

            return any;
        }

        private static bool TryPutInFreeHand(ItemSubSystem items, Hand holder, Item prefab)
        {
            Hands hands = holder.HandsController;
            if (hands == null || hands.PlayerHands == null)
            {
                return false;
            }

            foreach (Hand hand in hands.PlayerHands)
            {
                if (hand == null || hand == holder || !hand.IsEmpty())
                {
                    continue;
                }

                if (TwoHandedWeaponRules.TryResolveHandForItem(hands, prefab, hand, out Hand target)
                    && items.SpawnItemInContainer(prefab.GameObject, target.Container) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryResolveItem(string itemName, out Item prefab, out string assetId)
        {
            if (ResolvedItemIds.TryGetValue(itemName, out assetId)
                && CommandResolver.TryResolveItemAsset(assetId, out prefab, out _))
            {
                return true;
            }

            if (CommandResolver.TryResolveItemAsset(itemName, out prefab, out assetId))
            {
                ResolvedItemIds[itemName] = assetId;
                return true;
            }

            return false;
        }

        private OperativeUplinkSnapshot BuildSnapshot()
        {
            int roundId = CurrentRoundId(out _);
            UplinkAccount account = Account;
            IReadOnlyList<UplinkListing> catalog = OperativeUplinkCatalog.Listings;

            UplinkListingSnapshot[] listings = new UplinkListingSnapshot[catalog.Count];
            for (int i = 0; i < catalog.Count; i++)
            {
                UplinkListing listing = catalog[i];
                listings[i] = new UplinkListingSnapshot
                {
                    Name = listing.Name,
                    Category = listing.Category,
                    Price = listing.Price,
                    Kind = listing.Kind,
                    Description = listing.Description,
                    Includes = listing.Includes,
                    Restriction = listing.Restriction,
                    Remaining = listing.HasSquadStock
                        ? UplinkSquadStock.Shared.Remaining(listing, roundId)
                        : UplinkListingSnapshot.Unlimited,
                    Bundle = listing.Bundle,
                };
            }

            UplinkLedgerSnapshot[] ledger = new UplinkLedgerSnapshot[account.Ledger.Count];
            for (int i = 0; i < ledger.Length; i++)
            {
                UplinkLedgerEntry entry = account.Ledger[i];
                ledger[i] = new UplinkLedgerSnapshot
                {
                    Number = entry.Number,
                    RoundSeconds = entry.RoundSeconds,
                    ItemName = entry.ItemName,
                    Cost = entry.Cost,
                    BalanceAfter = entry.BalanceAfter,
                };
            }

            string[] categories = new string[OperativeUplinkCatalog.Categories.Count];
            for (int i = 0; i < categories.Length; i++)
            {
                categories[i] = OperativeUplinkCatalog.Categories[i];
            }

            return new OperativeUplinkSnapshot
            {
                MachineObjectId = NetworkObject.ObjectId,
                InterfaceId = InterfaceId,
                ModelLabel = _modelLabel,
                OwnerLine = string.IsNullOrEmpty(_ownerName)
                    ? "Syndicate field uplink"
                    : $"Operative {_ownerName} · Syndicate field uplink",
                Balance = account.Balance,
                StartingBalance = account.StartingBalance,
                Categories = categories,
                Listings = listings,
                Ledger = ledger,
                RefusalSequence = string.IsNullOrEmpty(_refusalText) ? 0 : _refusalSequence,
                RefusalText = _refusalText,
                DeliverySequence = _deliverySequence,
                DeliveryName = _deliveryName,
                DeliveredToHand = _deliveredToHand,
            };
        }

        /// <summary>Server: the first character to open the device is the operative it is issued to.</summary>
        private void ServerBindOwner(Hand hand)
        {
            if (!string.IsNullOrEmpty(_ownerName) || hand == null)
            {
                return;
            }

            Entity entity = hand.GetComponentInParent<Entity>();
            if (entity != null && entity.Mind != null)
            {
                _ownerName = entity.Mind.DisplayName;
            }
        }

        private Hand HeldBy(InteractionEvent e)
        {
            Hand hand = e?.Source?.GetRootSource() as Hand;
            return hand != null && hand.ItemInHand == GetComponent<Item>() ? hand : null;
        }

        /// <summary>Use in hand: open the panel. Only offered while this device is in the acting hand.</summary>
        private sealed class OpenUplinkInteraction : IInteraction
        {
            private readonly OperativeUplinkController _uplink;

            public OpenUplinkInteraction(OperativeUplinkController uplink) => _uplink = uplink;

            public int Priority => 60;

            public string GetGenericName() => "OpenUplink";

            public string GetName(InteractionEvent interactionEvent) => "Open uplink";

            public Sprite GetIcon(InteractionEvent interactionEvent) => InteractionIconLookup.MachineInterface;

            public bool CanInteract(InteractionEvent interactionEvent) =>
                _uplink != null && _uplink.HeldBy(interactionEvent) != null;

            public bool Start(InteractionEvent interactionEvent, InteractionReference reference)
            {
                if (!CanInteract(interactionEvent) || !_uplink.IsServer)
                {
                    return false;
                }

                _uplink.ServerBindOwner(_uplink.HeldBy(interactionEvent));
                _uplink.ServerHandleOpenRequest(interactionEvent);
                return false;
            }
        }
    }
}
