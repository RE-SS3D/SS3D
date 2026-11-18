using System;
using System.Collections.Generic;
using System.Text;
using SS3D.UI.MachineInterface.Components;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    /// <summary>
    /// Operative uplink panel. Tab, selection, ledger fold and banner dismissal are local to this viewer; the
    /// server owns balance, stock, ledger, refusals and deliveries. The snapshot refreshes on every electricity
    /// tick, so each section rebuilds only when its content changed (a rebuild mid-click drops the click, see
    /// <see cref="ProductGrid"/>).
    /// </summary>
    public sealed class OperativeUplinkBinder : IMachineInterfaceBinder
    {
        public event Action CloseRequested;

        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        public event Action<byte, int> ActionControlChanged;

        private const long DeltaVisibleMs = 2000;

        private readonly DiegeticDeviceShell _shell;
        private readonly DeviceIdentityBlock _identity;
        private readonly Label _spentLine;
        private readonly Label _balance;
        private readonly Label _delta;
        private readonly VisualElement _banner;
        private readonly Label _bannerKicker;
        private readonly Label _bannerText;
        private readonly Button _bannerDismiss;
        private readonly VisualElement _tabs;
        private readonly Label _gridTitle;
        private readonly Label _gridReadout;
        private readonly VisualElement _grid;
        private readonly VisualElement _detail;
        private readonly Label _detailName;
        private readonly Label _detailKind;
        private readonly Label _detailDesc;
        private readonly InventorySlot _detailSlot;
        private readonly VisualElement _detailFacts;
        private readonly SteelButton _buyButton;
        private readonly Label _buyReason;
        private readonly VisualElement _delivery;
        private readonly Label _deliveryName;
        private readonly InventorySlot _deliverySlot;
        private readonly Label _deliveryWhere;
        private readonly Label _deliveryNote;
        private readonly VisualElement _ledger;
        private readonly Label _ledgerChevron;
        private readonly Label _ledgerSummary;
        private readonly VisualElement _ledgerRows;

        private OperativeUplinkViewModel _model;
        private string _tab;
        private int _selected = -1;
        private bool _ledgerOpen = true;
        private int _dismissedRefusal;
        private int _lastBalance = int.MinValue;
        private IVisualElementScheduledItem _deltaHide;
        private string _tabsSignature;
        private string _gridSignature;
        private string _detailSignature;
        private string _ledgerSignature;

        public OperativeUplinkBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement content = _shell ?? root;

            _identity = content.Q<DeviceIdentityBlock>("identity");
            _spentLine = content.Q<Label>("spent-line");
            _balance = content.Q<Label>("balance");
            _delta = content.Q<Label>("delta");
            _banner = content.Q<VisualElement>("banner");
            _bannerKicker = content.Q<Label>("banner-kicker");
            _bannerText = content.Q<Label>("banner-text");
            _bannerDismiss = content.Q<Button>("banner-dismiss");
            _tabs = content.Q<VisualElement>("tabs");
            _gridTitle = content.Q<Label>("grid-title");
            _gridReadout = content.Q<Label>("grid-readout");
            _grid = content.Q<VisualElement>("grid");
            _detail = content.Q<VisualElement>("detail");
            _detailName = content.Q<Label>("detail-name");
            _detailKind = content.Q<Label>("detail-kind");
            _detailDesc = content.Q<Label>("detail-desc");
            _detailSlot = content.Q<InventorySlot>("detail-slot");
            _detailFacts = content.Q<VisualElement>("detail-facts");
            _buyButton = content.Q<SteelButton>("buy-button");
            _buyReason = content.Q<Label>("buy-reason");
            _delivery = content.Q<VisualElement>("delivery");
            _deliveryName = content.Q<Label>("delivery-name");
            _deliverySlot = content.Q<InventorySlot>("delivery-slot");
            _deliveryWhere = content.Q<Label>("delivery-where");
            _deliveryNote = content.Q<Label>("delivery-note");
            _ledger = content.Q<Button>("ledger-toggle")?.parent;
            _ledgerChevron = content.Q<Label>("ledger-chevron");
            _ledgerSummary = content.Q<Label>("ledger-summary");
            _ledgerRows = content.Q<VisualElement>("ledger-rows");

            if (_shell != null)
            {
                _shell.CloseClicked += HandleCloseRequested;
            }

            if (_bannerDismiss != null)
            {
                _bannerDismiss.clicked += DismissBanner;
            }

            if (_buyButton != null)
            {
                _buyButton.Clicked += Buy;

                // SteelButton.uss's 12px wins over the template rule; the design's Buy is a full-width 40px button.
                Button control = _buyButton.Q<Button>();
                if (control != null)
                {
                    control.style.fontSize = 16;
                }
            }

            Button ledgerToggle = content.Q<Button>("ledger-toggle");
            if (ledgerToggle != null)
            {
                ledgerToggle.clicked += ToggleLedger;
            }
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not OperativeUplinkViewModel model)
            {
                return;
            }

            _model = model;
            if (_tab == null || !model.Categories.Contains(_tab))
            {
                _tab = model.Categories.Count > 0 ? model.Categories[0] : string.Empty;
            }

            if (_selected >= model.Listings.Count)
            {
                _selected = -1;
            }

            if (_shell != null)
            {
                _shell.ModelLabel = model.ModelLabel;
                _shell.PowerOk = true;
            }

            if (_identity != null)
            {
                _identity.Title = "UPLINK";
                _identity.Subtitle = model.OwnerLine;
            }

            BindBalance(model);
            BindBanner(model);
            BindTabs(model);
            BindGrid(model);
            BindDetail(model);
            BindDelivery(model);
            BindLedger(model);
        }

        public void Disconnect()
        {
            if (_shell != null)
            {
                _shell.CloseClicked -= HandleCloseRequested;
            }

            if (_bannerDismiss != null)
            {
                _bannerDismiss.clicked -= DismissBanner;
            }

            if (_buyButton != null)
            {
                _buyButton.Clicked -= Buy;
            }

            _deltaHide?.Pause();
        }

        private void BindBalance(OperativeUplinkViewModel model)
        {
            if (_balance != null)
            {
                _balance.text = model.Balance.ToString();
                _balance.EnableInClassList("uplink__balance-number--zero", model.Balance == 0);
            }

            if (_spentLine != null)
            {
                _spentLine.text = model.Spent > 0
                    ? $"{model.Spent} of {model.StartingBalance} TC spent"
                    : "Nothing spent yet";
            }

            if (_lastBalance != int.MinValue && model.Balance < _lastBalance && _delta != null)
            {
                _delta.text = $"−{_lastBalance - model.Balance} TC";
                _delta.AddToClassList("uplink__delta--visible");
                _deltaHide?.Pause();
                _deltaHide = _delta.schedule
                    .Execute(() => _delta.RemoveFromClassList("uplink__delta--visible"))
                    .StartingIn(DeltaVisibleMs);
            }

            _lastBalance = model.Balance;
        }

        /// <summary>A refusal takes the slot; otherwise a zero balance shows a sticky info line.</summary>
        private void BindBanner(OperativeUplinkViewModel model)
        {
            if (_banner == null)
            {
                return;
            }

            bool refusal = model.RefusalSequence > 0
                && model.RefusalSequence != _dismissedRefusal
                && !string.IsNullOrEmpty(model.RefusalText);
            bool zero = !refusal && model.Balance == 0;

            _banner.EnableInClassList("uplink__banner--visible", refusal || zero);
            _banner.EnableInClassList("uplink__banner--danger", refusal);
            _banner.EnableInClassList("uplink__banner--info", zero);

            if (_bannerKicker != null)
            {
                _bannerKicker.text = refusal ? "PURCHASE REFUSED" : "BALANCE 0 TC";
            }

            if (_bannerText != null)
            {
                _bannerText.text = refusal
                    ? model.RefusalText
                    : "All telecrystals spent. Nothing more can be bought on this uplink.";
            }

            if (_bannerDismiss != null)
            {
                _bannerDismiss.style.display = refusal ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void BindTabs(OperativeUplinkViewModel model)
        {
            if (_tabs == null)
            {
                return;
            }

            StringBuilder signature = new(_tab);
            foreach (string category in model.Categories)
            {
                signature.Append('|').Append(category);
            }

            string key = signature.ToString();
            if (key == _tabsSignature)
            {
                return;
            }

            _tabsSignature = key;
            _tabs.Clear();
            foreach (string category in model.Categories)
            {
                int count = 0;
                foreach (UplinkListingViewData listing in model.Listings)
                {
                    if (listing.Category == category)
                    {
                        count++;
                    }
                }

                Button tab = new();
                tab.AddToClassList("uplink__tab");
                tab.EnableInClassList("uplink__tab--active", category == _tab);
                Label label = new(category.ToUpperInvariant());
                label.AddToClassList("uplink__tab-label");
                label.AddToClassList("font-titling");
                Label countLabel = new(count.ToString());
                countLabel.AddToClassList("uplink__tab-count");
                countLabel.AddToClassList("font-terminal");
                tab.Add(label);
                tab.Add(countLabel);
                string picked = category;
                tab.clicked += () => PickTab(picked);
                _tabs.Add(tab);
            }
        }

        private void BindGrid(OperativeUplinkViewModel model)
        {
            if (_grid == null)
            {
                return;
            }

            int inTab = 0;
            int affordable = 0;
            StringBuilder signature = new();
            signature.Append(_tab).Append('#').Append(_selected).Append('#').Append(model.Balance);
            foreach (UplinkListingViewData listing in model.Listings)
            {
                if (listing.Category != _tab)
                {
                    continue;
                }

                inTab++;
                if (listing.CanBuy)
                {
                    affordable++;
                }

                signature.Append('|').Append(listing.Index).Append(':').Append(listing.Remaining).Append(':').Append(listing.Price);
            }

            if (_gridTitle != null)
            {
                _gridTitle.text = (_tab ?? string.Empty).ToUpperInvariant();
            }

            if (_gridReadout != null)
            {
                _gridReadout.text = inTab > 0 ? $"{affordable} of {inTab} affordable" : string.Empty;
            }

            string key = signature.ToString();
            if (key == _gridSignature)
            {
                return;
            }

            _gridSignature = key;
            _grid.Clear();

            if (inTab == 0)
            {
                Label empty = new("No listings in this category yet.");
                empty.AddToClassList("uplink__grid-empty");
                empty.AddToClassList("font-terminal");
                _grid.Add(empty);
                return;
            }

            foreach (UplinkListingViewData listing in model.Listings)
            {
                if (listing.Category == _tab)
                {
                    _grid.Add(BuildCard(listing));
                }
            }
        }

        private VisualElement BuildCard(UplinkListingViewData listing)
        {
            bool unaffordable = !listing.OutOfStock && listing.Shortfall > 0;

            Button card = new();
            card.AddToClassList("uplink__card");
            card.EnableInClassList("uplink__card--selected", listing.Index == _selected);
            card.EnableInClassList("uplink__card--out", listing.OutOfStock);
            card.EnableInClassList("uplink__card--unaffordable", unaffordable);
            card.tooltip = listing.Name + ", " + listing.Price + " TC"
                + (listing.OutOfStock ? ", out of stock" : string.Empty)
                + (unaffordable ? $", need {listing.Shortfall} more TC" : string.Empty);

            InventorySlot slot = new() { Unknown = true, Size = 44 };
            OperativeUplinkIcons.Apply(slot, listing.Index);
            card.Add(slot);

            Label name = new(listing.Name);
            name.AddToClassList("uplink__card-name");
            name.AddToClassList("font-body");
            card.Add(name);

            Label price = new(listing.Price + " TC");
            price.AddToClassList("uplink__card-price");
            price.AddToClassList("font-terminal");
            card.Add(price);

            if (unaffordable)
            {
                Label need = new($"Need {listing.Shortfall} more TC");
                need.AddToClassList("uplink__card-need");
                need.AddToClassList("font-terminal");
                card.Add(need);
            }

            VisualElement badges = new();
            badges.AddToClassList("uplink__badges");
            if (listing.HasStock)
            {
                badges.Add(Badge(listing.OutOfStock ? "OUT OF STOCK" : listing.Remaining + " LEFT",
                    listing.OutOfStock ? "out" : "stock"));
            }

            if (!string.IsNullOrEmpty(listing.Restriction))
            {
                badges.Add(Badge(listing.Restriction.ToUpperInvariant(), "restrict"));
            }

            if (listing.Bundle)
            {
                badges.Add(Badge("BUNDLE", "bundle"));
            }

            if (badges.childCount > 0)
            {
                card.Add(badges);
            }

            int index = listing.Index;
            card.clicked += () => Select(index);
            return card;
        }

        private static Label Badge(string text, string tone)
        {
            Label badge = new(text);
            badge.AddToClassList("uplink__badge");
            badge.AddToClassList("uplink__badge--" + tone);
            badge.AddToClassList("font-arcade");
            return badge;
        }

        private void BindDetail(OperativeUplinkViewModel model)
        {
            if (_detail == null)
            {
                return;
            }

            UplinkListingViewData listing = _selected >= 0 ? model.Listings[_selected] : null;
            if (listing != null && listing.Category != _tab)
            {
                listing = null;
            }

            _detail.EnableInClassList("uplink__detail-panel--selected", listing != null);
            if (listing == null)
            {
                _detailSignature = null;
                return;
            }

            string key = $"{listing.Index}:{listing.Remaining}:{model.Balance}";
            if (key == _detailSignature)
            {
                return;
            }

            _detailSignature = key;

            if (_detailName != null) _detailName.text = listing.Name;
            if (_detailKind != null) _detailKind.text = listing.Kind.ToUpperInvariant();
            if (_detailDesc != null) _detailDesc.text = listing.Description;
            if (_detailSlot != null)
            {
                _detailSlot.style.opacity = listing.OutOfStock ? 0.35f : 1f;
                OperativeUplinkIcons.Apply(_detailSlot, listing.Index);
            }

            if (_detailFacts != null)
            {
                _detailFacts.Clear();
                AddFact("Price", listing.Price + " TC", null);
                if (listing.HasStock)
                {
                    AddFact("Stock (squad-wide)", listing.OutOfStock ? "Out of stock" : listing.Remaining + " left",
                        listing.OutOfStock ? "warn" : null);
                }

                if (!string.IsNullOrEmpty(listing.Restriction)) AddFact("Restriction", listing.Restriction, "amber");
                if (!string.IsNullOrEmpty(listing.Includes)) AddFact("Includes", listing.Includes, null);
                AddFact("After purchase", listing.CanBuy ? (model.Balance - listing.Price) + " TC left" : "—", null);
            }

            string label = "Buy · " + listing.Price + " TC";
            string reason = "One click buys. The ledger keeps a record.";
            string tone = null;
            if (listing.OutOfStock)
            {
                label = "Out of stock";
                reason = "None left for the squad this round.";
                tone = "warn";
            }
            else if (listing.Shortfall > 0)
            {
                label = $"Need {listing.Shortfall} more TC";
                reason = $"You have {model.Balance} TC.";
                tone = "amber";
            }

            if (_buyButton != null)
            {
                _buyButton.Text = label;
                _buyButton.Disabled = !listing.CanBuy;
            }

            if (_buyReason != null)
            {
                _buyReason.text = reason;
                _buyReason.EnableInClassList("uplink__buy-reason--warn", tone == "warn");
                _buyReason.EnableInClassList("uplink__buy-reason--amber", tone == "amber");
            }
        }

        private void AddFact(string key, string value, string tone)
        {
            VisualElement row = new();
            row.AddToClassList("uplink__fact");
            Label k = new(key);
            k.AddToClassList("uplink__fact-key");
            k.AddToClassList("font-terminal");
            Label v = new(value);
            v.AddToClassList("uplink__fact-value");
            v.AddToClassList("font-terminal");
            if (tone != null)
            {
                v.AddToClassList("uplink__fact-value--" + tone);
            }

            row.Add(k);
            row.Add(v);
            _detailFacts.Add(row);
        }

        private void BindDelivery(OperativeUplinkViewModel model)
        {
            if (_delivery == null)
            {
                return;
            }

            bool has = model.DeliverySequence > 0 && !string.IsNullOrEmpty(model.DeliveryName);
            _delivery.EnableInClassList("uplink__delivery--has", has);
            if (!has)
            {
                return;
            }

            if (_deliveryName != null) _deliveryName.text = model.DeliveryName;
            OperativeUplinkIcons.Apply(_deliverySlot, model.DeliveryListingIndex);
            if (_deliveryWhere != null)
            {
                _deliveryWhere.text = model.DeliveredToHand ? "IN YOUR HAND" : "AT YOUR FEET";
                _deliveryWhere.EnableInClassList("uplink__delivery-where--feet", !model.DeliveredToHand);
            }

            if (_deliveryNote != null)
            {
                _deliveryNote.text = model.DeliveredToHand ? "Put in your free hand" : "Both hands were full";
            }
        }

        private void BindLedger(OperativeUplinkViewModel model)
        {
            _ledger?.EnableInClassList("uplink__ledger--collapsed", !_ledgerOpen);
            if (_ledgerChevron != null)
            {
                // Body font: the titling and terminal fonts have no triangle glyphs.
                _ledgerChevron.text = _ledgerOpen ? "▼" : "►";
            }

            if (_ledgerSummary != null)
            {
                int count = model.Ledger.Count;
                _ledgerSummary.text = count + (count == 1 ? " purchase" : " purchases") + " · " + model.Spent + " TC spent";
            }

            if (_ledgerRows == null)
            {
                return;
            }

            string key = model.Ledger.Count + ":" + model.Balance + ":" + model.StartingBalance;
            if (key == _ledgerSignature)
            {
                return;
            }

            _ledgerSignature = key;
            _ledgerRows.Clear();
            if (model.Ledger.Count == 0)
            {
                Label empty = new($"No purchases yet. Starting balance {model.StartingBalance} TC.");
                empty.AddToClassList("uplink__ledger-empty");
                empty.AddToClassList("font-terminal");
                _ledgerRows.Add(empty);
                return;
            }

            foreach (UplinkLedgerViewData entry in model.Ledger)
            {
                VisualElement row = new();
                row.AddToClassList("uplink__ledger-row");
                row.Add(Cell(entry.Number.ToString(), "uplink__col-n"));
                row.Add(Cell(entry.Time, "uplink__col-time"));
                row.Add(Cell(entry.ItemName, "uplink__col-item", "uplink__col-item--value"));
                row.Add(Cell("−" + entry.Cost + " TC", "uplink__col-cost", "uplink__col-cost--value"));
                row.Add(Cell(entry.BalanceAfter + " TC", "uplink__col-after", "uplink__col-after--value"));
                _ledgerRows.Add(row);
            }
        }

        private static Label Cell(string text, string column, string extra = null)
        {
            Label cell = new(text);
            cell.AddToClassList(column);
            cell.AddToClassList("font-terminal");
            if (extra != null)
            {
                cell.AddToClassList(extra);
            }

            return cell;
        }

        private void PickTab(string category)
        {
            if (category == _tab)
            {
                return;
            }

            _tab = category;
            _selected = -1;
            Rebind();
        }

        private void Select(int index)
        {
            _selected = index;
            Rebind();
        }

        private void Buy()
        {
            if (_model == null || _selected < 0 || _selected >= _model.Listings.Count || !_model.Listings[_selected].CanBuy)
            {
                return;
            }

            ActionControlChanged?.Invoke(MachineInterfaceControlIds.Uplink.Buy, _selected);
        }

        private void DismissBanner()
        {
            if (_model != null)
            {
                _dismissedRefusal = _model.RefusalSequence;
                BindBanner(_model);
            }
        }

        private void ToggleLedger()
        {
            _ledgerOpen = !_ledgerOpen;
            if (_model != null)
            {
                BindLedger(_model);
            }
        }

        private void Rebind()
        {
            if (_model == null)
            {
                return;
            }

            BindTabs(_model);
            BindGrid(_model);
            BindDetail(_model);
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }
    }
}
