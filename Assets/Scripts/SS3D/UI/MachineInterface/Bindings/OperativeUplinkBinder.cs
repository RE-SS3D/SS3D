using System;
using System.Text;
using SS3D.UI.MachineInterface.Components;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    /// <summary>
    /// Operative uplink panel (Claude Design "Operative Uplink", list layout). Tab (a category or the pinned Ledger),
    /// selection and banner dismissal are local to this viewer; the server owns balance, stock, ledger, refusals and
    /// deliveries. The snapshot refreshes on every electricity tick, so each section rebuilds only when its content
    /// changed (a rebuild mid-click drops the click, see <see cref="ProductGrid"/>).
    /// </summary>
    public sealed class OperativeUplinkBinder : IMachineInterfaceBinder
    {
        public event Action CloseRequested;

        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        public event Action<byte, int> ActionControlChanged;

        private const long DeltaVisibleMs = 2000;

        /// <summary>The tab id of the pinned ledger tab; categories use their own names.</summary>
        private const string LedgerTab = "\u0001ledger";

        private readonly DiegeticDeviceShell _shell;
        private readonly Label _ownerLine;
        private readonly Label _spentLine;
        private readonly Label _balance;
        private readonly Label _delta;
        private readonly VisualElement _banner;
        private readonly Label _bannerKicker;
        private readonly Label _bannerText;
        private readonly Button _bannerDismiss;
        private readonly VisualElement _tabs;
        private readonly Button _ledgerTab;
        private readonly Label _ledgerCount;
        private readonly Label _listTitle;
        private readonly Label _listReadout;
        private readonly VisualElement _rows;
        private readonly Label _ledgerSummary;
        private readonly VisualElement _ledgerRows;
        private readonly VisualElement _detail;
        private readonly Label _detailName;
        private readonly VisualElement _detailBadges;
        private readonly Label _detailKind;
        private readonly Label _detailDesc;
        private readonly InventorySlot _detailSlot;
        private readonly VisualElement _detailFacts;
        private readonly SteelButton _buyButton;
        private readonly Label _buyReason;
        private readonly VisualElement _delivery;
        private readonly InventorySlot _deliverySlot;
        private readonly Label _deliveryName;
        private readonly Label _deliveryWhere;
        private readonly Label _deliveryNote;

        private OperativeUplinkViewModel _model;
        private string _tab;
        private int _selected = -1;
        private int _dismissedRefusal;
        private int _lastBalance = int.MinValue;
        private IVisualElementScheduledItem _deltaHide;
        private string _tabsSignature;
        private string _rowsSignature;
        private string _detailSignature;
        private string _ledgerSignature;

        public OperativeUplinkBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement content = _shell ?? root;

            _ownerLine = content.Q<Label>("owner-line");
            _spentLine = content.Q<Label>("spent-line");
            _balance = content.Q<Label>("balance");
            _delta = content.Q<Label>("delta");
            _banner = content.Q<VisualElement>("banner");
            _bannerKicker = content.Q<Label>("banner-kicker");
            _bannerText = content.Q<Label>("banner-text");
            _bannerDismiss = content.Q<Button>("banner-dismiss");
            _tabs = content.Q<VisualElement>("tabs");
            _ledgerTab = content.Q<Button>("ledger-tab");
            _ledgerCount = content.Q<Label>("ledger-count");
            _listTitle = content.Q<Label>("list-title");
            _listReadout = content.Q<Label>("list-readout");
            _rows = content.Q<VisualElement>("rows");
            _ledgerSummary = content.Q<Label>("ledger-summary");
            _ledgerRows = content.Q<VisualElement>("ledger-rows");
            _detail = content.Q<VisualElement>("detail");
            _detailName = content.Q<Label>("detail-name");
            _detailBadges = content.Q<VisualElement>("detail-badges");
            _detailKind = content.Q<Label>("detail-kind");
            _detailDesc = content.Q<Label>("detail-desc");
            _detailSlot = content.Q<InventorySlot>("detail-slot");
            _detailFacts = content.Q<VisualElement>("detail-facts");
            _buyButton = content.Q<SteelButton>("buy-button");
            _buyReason = content.Q<Label>("buy-reason");
            _delivery = content.Q<VisualElement>("delivery");
            _deliverySlot = content.Q<InventorySlot>("delivery-slot");
            _deliveryName = content.Q<Label>("delivery-name");
            _deliveryWhere = content.Q<Label>("delivery-where");
            _deliveryNote = content.Q<Label>("delivery-note");

            if (_shell != null)
            {
                _shell.CloseClicked += HandleCloseRequested;
            }

            if (_bannerDismiss != null)
            {
                _bannerDismiss.clicked += DismissBanner;
            }

            if (_ledgerTab != null)
            {
                _ledgerTab.clicked += PickLedger;
            }

            if (_buyButton != null)
            {
                _buyButton.Clicked += Buy;

                // SteelButton.uss's 12px wins over the template rule; the design's Buy is a full-width 44px button.
                Button control = _buyButton.Q<Button>();
                if (control != null)
                {
                    control.style.fontSize = 20;
                }
            }
        }

        private bool OnLedger => _tab == LedgerTab;

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not OperativeUplinkViewModel model)
            {
                return;
            }

            _model = model;
            if (_tab == null || (!OnLedger && !model.Categories.Contains(_tab)))
            {
                _tab = model.Categories.Count > 0 ? model.Categories[0] : LedgerTab;
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

            if (_ownerLine != null)
            {
                _ownerLine.text = model.OwnerLine;
            }

            BindBalance(model);
            BindBanner(model);
            BindTabs(model);
            BindRows(model);
            BindLedger(model);
            BindDetail(model);
            BindDelivery(model);
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

            if (_ledgerTab != null)
            {
                _ledgerTab.clicked -= PickLedger;
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
            _shell?.EnableInClassList("uplink-interface--ledger", OnLedger);
            _ledgerTab?.EnableInClassList("uplink__tab--active", OnLedger);
            if (_ledgerCount != null)
            {
                _ledgerCount.text = model.Ledger.Count.ToString();
            }

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

        private void BindRows(OperativeUplinkViewModel model)
        {
            if (_rows == null || OnLedger)
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

            if (_listTitle != null)
            {
                _listTitle.text = (_tab ?? string.Empty).ToUpperInvariant();
            }

            if (_listReadout != null)
            {
                _listReadout.text = inTab > 0 ? $"{affordable} of {inTab} affordable" : string.Empty;
            }

            string key = signature.ToString();
            if (key == _rowsSignature)
            {
                return;
            }

            _rowsSignature = key;
            _rows.Clear();

            if (inTab == 0)
            {
                Label empty = new("No listings in this category yet.");
                empty.AddToClassList("uplink__list-empty");
                empty.AddToClassList("font-terminal");
                _rows.Add(empty);
                return;
            }

            foreach (UplinkListingViewData listing in model.Listings)
            {
                if (listing.Category == _tab)
                {
                    _rows.Add(BuildRow(listing));
                }
            }
        }

        private VisualElement BuildRow(UplinkListingViewData listing)
        {
            bool unaffordable = !listing.OutOfStock && listing.Shortfall > 0;

            Button row = new();
            row.AddToClassList("uplink__row");
            row.EnableInClassList("uplink__row--selected", listing.Index == _selected);
            row.EnableInClassList("uplink__row--out", listing.OutOfStock);
            row.EnableInClassList("uplink__row--dim", listing.OutOfStock || unaffordable);
            row.tooltip = listing.Name + ", " + listing.Price + " TC"
                + (listing.OutOfStock ? ", out of stock" : string.Empty)
                + (unaffordable ? $", need {listing.Shortfall} more TC" : string.Empty);

            VisualElement iconCell = new();
            iconCell.AddToClassList("uplink__col-icon");
            InventorySlot slot = new() { Unknown = true, Size = 40 };
            OperativeUplinkIcons.Apply(slot, listing.IconItemId);
            iconCell.Add(slot);
            row.Add(iconCell);

            VisualElement nameCell = new();
            nameCell.AddToClassList("uplink__col-name");
            nameCell.AddToClassList("uplink__row-name-wrap");
            VisualElement nameBox = new();
            Label name = new(listing.Name.ToUpperInvariant());
            name.AddToClassList("uplink__row-name");
            name.AddToClassList("font-terminal");
            nameBox.Add(name);
            if (listing.OutOfStock)
            {
                VisualElement strike = new() { pickingMode = PickingMode.Ignore };
                strike.AddToClassList("uplink__strike");
                nameBox.Add(strike);
            }

            nameCell.Add(nameBox);
            row.Add(nameCell);

            VisualElement badges = new();
            badges.AddToClassList("uplink__col-badges");
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

            row.Add(badges);

            VisualElement costCell = new();
            costCell.AddToClassList("uplink__col-cost");
            Label price = new(listing.Price + " TC");
            price.AddToClassList("uplink__row-price");
            price.AddToClassList("font-terminal");
            costCell.Add(price);
            if (unaffordable)
            {
                Label need = new($"Need {listing.Shortfall} more");
                need.AddToClassList("uplink__row-need");
                need.AddToClassList("font-terminal");
                costCell.Add(need);
            }

            row.Add(costCell);

            int index = listing.Index;
            row.clicked += () => Select(index);
            return row;
        }

        private static Label Badge(string text, string tone)
        {
            Label badge = new(text);
            badge.AddToClassList("uplink__badge");
            badge.AddToClassList("uplink__badge--" + tone);
            badge.AddToClassList("font-arcade");
            return badge;
        }

        private void BindLedger(OperativeUplinkViewModel model)
        {
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
                row.Add(Cell("−" + entry.Cost + " TC", "uplink__col-lcost", "uplink__col-lcost--value"));
                row.Add(Cell(entry.BalanceAfter + " TC", "uplink__col-after", "uplink__col-after--value"));
                _ledgerRows.Add(row);
            }
        }

        private static Label Cell(string text, string column, string extra = null)
        {
            Label cell = new(text);
            cell.AddToClassList(column);
            cell.AddToClassList("uplink__ledger-cell");
            cell.AddToClassList("font-terminal");
            if (extra != null)
            {
                cell.AddToClassList(extra);
            }

            return cell;
        }

        /// <summary>The selection stays put across tabs, Ledger included.</summary>
        private void BindDetail(OperativeUplinkViewModel model)
        {
            if (_detail == null)
            {
                return;
            }

            UplinkListingViewData listing = _selected >= 0 ? model.Listings[_selected] : null;
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
            if (_detailBadges != null)
            {
                _detailBadges.Clear();
                if (!string.IsNullOrEmpty(listing.Restriction))
                {
                    _detailBadges.Add(Badge(listing.Restriction.ToUpperInvariant(), "restrict"));
                }
            }

            if (_detailSlot != null)
            {
                _detailSlot.style.opacity = listing.OutOfStock ? 0.35f : 1f;
                OperativeUplinkIcons.Apply(_detailSlot, listing.IconItemId);
            }

            if (_detailFacts != null)
            {
                _detailFacts.Clear();
                AddFact("Price", listing.Price + " TC", false);
                if (listing.HasStock)
                {
                    AddFact("Stock (squad)", listing.OutOfStock ? "Out of stock" : listing.Remaining + " left", listing.OutOfStock);
                }

                if (!string.IsNullOrEmpty(listing.Includes)) AddFact("Includes", listing.Includes, false);
                AddFact("After purchase", listing.CanBuy ? (model.Balance - listing.Price) + " TC left" : "—", false);
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

        private void AddFact(string key, string value, bool warn)
        {
            VisualElement row = new();
            row.AddToClassList("uplink__fact");
            Label k = new(key);
            k.AddToClassList("uplink__fact-key");
            k.AddToClassList("font-terminal");
            Label v = new(value);
            v.AddToClassList("uplink__fact-value");
            v.AddToClassList("font-terminal");
            v.EnableInClassList("uplink__fact-value--warn", warn);
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
            OperativeUplinkIcons.Apply(_deliverySlot,
                model.DeliveryListingIndex >= 0 && model.DeliveryListingIndex < model.Listings.Count
                    ? model.Listings[model.DeliveryListingIndex].IconItemId
                    : null);
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

        private void PickTab(string category)
        {
            if (category == _tab)
            {
                return;
            }

            _tab = category;
            Rebind();
        }

        private void PickLedger() => PickTab(LedgerTab);

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

        private void Rebind()
        {
            if (_model == null)
            {
                return;
            }

            BindTabs(_model);
            BindRows(_model);
            BindDetail(_model);
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }
    }
}
