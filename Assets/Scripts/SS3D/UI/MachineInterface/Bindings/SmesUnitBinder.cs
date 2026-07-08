using SS3D.UI.MachineInterface.Components;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public class SmesUnitBinder
    {
        public event Action CloseRequested;

        public event Action<bool> InputToggled;

        public event Action<bool> OutputToggled;

        public event Action<float> InputRateDeltaRequested;

        public event Action<float> OutputRateDeltaRequested;

        private readonly MachineWindow _window;
        private readonly VisualElement _exteriorView;
        private readonly VisualElement _engineerView;
        private readonly VisualElement _exteriorStatus;
        private readonly Label _exteriorStatusWord;
        private readonly StatusBadge _exteriorBadge;
        private readonly Label _exteriorChargePct;
        private readonly VisualElement _exteriorChargeFill;
        private readonly VisualElement _exteriorInputCard;
        private readonly VisualElement _exteriorOutputCard;
        private readonly Label _exteriorInputWord;
        private readonly Label _exteriorOutputWord;
        private readonly VisualElement _connectionDot;
        private readonly Label _connectionText;
        private readonly StatusBadge _engineerBadge;
        private readonly WarningPanel _warningPanel;
        private readonly StorageCellRow _storageCells;
        private readonly Label _engineerChargePct;
        private readonly Label _chargeTrendText;
        private readonly VerticalPowerFlow _powerFlow;
        private readonly RateControlSection _inputControl;
        private readonly RateControlSection _outputControl;
        private readonly HoldRevealButton _holdReveal;
        private readonly AdvancedDiagnosticsPanel _advancedPanel;
        private bool _advancedRevealed;

        public SmesUnitBinder(VisualElement root)
        {
            _window = root.Q<MachineWindow>("machine-window");
            VisualElement contentRoot = root.Q<VisualElement>("smes-root") ?? root;

            _exteriorView = contentRoot.Q<VisualElement>("exterior-view");
            _engineerView = contentRoot.Q<VisualElement>("engineer-view");
            _exteriorStatus = contentRoot.Q<VisualElement>("exterior-status");
            _exteriorStatusWord = contentRoot.Q<Label>("exterior-status-word");
            _exteriorBadge = contentRoot.Q<StatusBadge>("exterior-badge");
            _exteriorChargePct = contentRoot.Q<Label>("exterior-charge-pct");
            _exteriorChargeFill = contentRoot.Q<VisualElement>("exterior-charge-fill");
            _exteriorInputCard = contentRoot.Q<VisualElement>("exterior-input-card");
            _exteriorOutputCard = contentRoot.Q<VisualElement>("exterior-output-card");
            _exteriorInputWord = contentRoot.Q<Label>("exterior-input-word");
            _exteriorOutputWord = contentRoot.Q<Label>("exterior-output-word");
            _connectionDot = contentRoot.Q<VisualElement>("connection-dot");
            _connectionText = contentRoot.Q<Label>("connection-text");
            _engineerBadge = contentRoot.Q<StatusBadge>("engineer-badge");
            _warningPanel = contentRoot.Q<WarningPanel>("warning-panel");
            _storageCells = contentRoot.Q<StorageCellRow>("storage-cells");
            _engineerChargePct = contentRoot.Q<Label>("engineer-charge-pct");
            _chargeTrendText = contentRoot.Q<Label>("charge-trend-text");
            _powerFlow = contentRoot.Q<VerticalPowerFlow>("power-flow");
            _inputControl = contentRoot.Q<RateControlSection>("input-control");
            _outputControl = contentRoot.Q<RateControlSection>("output-control");
            _holdReveal = contentRoot.Q<HoldRevealButton>("hold-reveal");
            _advancedPanel = contentRoot.Q<AdvancedDiagnosticsPanel>("advanced-panel");

            if (_window != null)
            {
                _window.CloseClicked += () => CloseRequested?.Invoke();
            }

            Button openEngineerButton = contentRoot.Q<Button>("open-engineer-btn");
            if (openEngineerButton != null)
            {
                openEngineerButton.clicked += ShowEngineerView;
            }

            Button closeEngineerButton = contentRoot.Q<Button>("close-engineer-btn");
            if (closeEngineerButton != null)
            {
                closeEngineerButton.clicked += ShowExteriorView;
            }

            if (_inputControl != null)
            {
                _inputControl.SectionTitle = "INPUT";
                _inputControl.ToggleChanged += value => InputToggled?.Invoke(value);
                _inputControl.RateDeltaRequested += delta => InputRateDeltaRequested?.Invoke(delta);
            }

            if (_outputControl != null)
            {
                _outputControl.SectionTitle = "OUTPUT";
                _outputControl.ToggleChanged += value => OutputToggled?.Invoke(value);
                _outputControl.RateDeltaRequested += delta => OutputRateDeltaRequested?.Invoke(delta);
            }

            if (_holdReveal != null)
            {
                _holdReveal.Revealed += () =>
                {
                    _advancedRevealed = true;
                    UpdateAdvancedVisibility();
                };
            }

            if (_advancedPanel != null)
            {
                _advancedPanel.HideRequested += () =>
                {
                    _advancedRevealed = false;
                    UpdateAdvancedVisibility();
                };
            }

            ShowExteriorView();
        }

        public void Bind(SmesInterfaceViewModel model)
        {
            if (_window != null)
            {
                _window.Title = model.Title;
            }

            StatusTone tone = SmesUnitBinderHelpers.GetStatusTone(model.State);
            BindExterior(model, tone);
            BindEngineer(model, tone);
        }

        private void BindExterior(SmesInterfaceViewModel model, StatusTone tone)
        {
            _exteriorStatusWord.text = model.ExteriorStatusWord.ToUpperInvariant();
            _exteriorBadge.Text = model.StatusBadgeText;
            _exteriorBadge.Tone = tone;
            StatusToneUtility.ApplyTone(_exteriorStatus, tone, background: true);
            StatusToneUtility.ApplyTone(_exteriorStatusWord, tone);

            int chargePercent = Mathf.RoundToInt(model.ChargePct * 100f);
            _exteriorChargePct.text = $"{chargePercent}%";
            StatusTone chargeTone = SmesUnitBinderHelpers.GetChargeTone(model.ChargePct);
            StatusToneUtility.ApplyTone(_exteriorChargePct, chargeTone);
            SmesUnitBinderHelpers.ApplyFillTone(_exteriorChargeFill, chargeTone);
            _exteriorChargeFill.style.width = new Length(model.ChargePct * 100f, LengthUnit.Percent);

            _exteriorInputWord.text = model.ExteriorInputWord;
            _exteriorOutputWord.text = model.ExteriorOutputWord;
            SmesUnitBinderHelpers.ApplyIoCardTone(_exteriorInputCard, SmesUnitBinderHelpers.GetIoTone(model.ExteriorInputWord));
            SmesUnitBinderHelpers.ApplyIoCardTone(_exteriorOutputCard, SmesUnitBinderHelpers.GetIoTone(model.ExteriorOutputWord));

            StatusToneUtility.ApplyTone(_connectionDot, tone);
            _connectionText.text = model.ConnectionStateText;
        }

        private void BindEngineer(SmesInterfaceViewModel model, StatusTone tone)
        {
            _engineerBadge.Text = model.StatusBadgeText;
            _engineerBadge.Tone = tone;
            _warningPanel.SetWarnings(model.Warnings, model.DiagnosisHint);

            StatusTone chargeTone = SmesUnitBinderHelpers.GetChargeTone(model.ChargePct);
            _storageCells.SetCharge(model.ChargePct, chargeTone);
            _engineerChargePct.text = $"{Mathf.RoundToInt(model.ChargePct * 100f)}%";
            StatusToneUtility.ApplyTone(_engineerChargePct, chargeTone);
            _chargeTrendText.text = SmesUnitBinderHelpers.GetTrendText(model.ChargeTrend);

            StatusTone inputTone = SmesUnitBinderHelpers.GetInputTone(model);
            StatusTone outputTone = SmesUnitBinderHelpers.GetOutputTone(model);
            _powerFlow.SetFlow(model.InputActive, model.OutputActive, inputTone, outputTone);

            if (_inputControl != null)
            {
                _inputControl.IsEnabled = model.InputEnabled;
                _inputControl.CurrentKw = model.InputCurrentKw;
                _inputControl.MaxKw = model.InputMaxKw;
                _inputControl.SetAccentTone(inputTone);
            }

            if (_outputControl != null)
            {
                _outputControl.IsEnabled = model.OutputEnabled;
                _outputControl.CurrentKw = model.OutputCurrentKw;
                _outputControl.MaxKw = model.OutputMaxKw;
                _outputControl.SetAccentTone(outputTone);
            }

            _advancedPanel.SetMetrics(model.AdvancedMetrics, model.MaintenanceText, model.MaintenanceTone);
        }

        private void ShowEngineerView()
        {
            _exteriorView?.AddToClassList("smes-view--hidden");
            _engineerView?.RemoveFromClassList("smes-view--hidden");
        }

        private void ShowExteriorView()
        {
            _engineerView?.AddToClassList("smes-view--hidden");
            _exteriorView?.RemoveFromClassList("smes-view--hidden");
            _advancedRevealed = false;
            UpdateAdvancedVisibility();
        }

        private void UpdateAdvancedVisibility()
        {
            if (_holdReveal != null)
            {
                _holdReveal.style.display = _advancedRevealed ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (_advancedPanel != null)
            {
                _advancedPanel.EnableInClassList("smes-view--hidden", !_advancedRevealed);
            }
        }
    }
}
