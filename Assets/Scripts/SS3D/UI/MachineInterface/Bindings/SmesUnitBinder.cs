using SS3D.UI.MachineInterface.Components;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public class SmesUnitBinder : IMachineInterfaceBinder
    {
        public event Action CloseRequested;

        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        private readonly MachineWindow _window;
        private readonly SmesStatusBanner _statusBanner;
        private readonly StorageCellRow _storageCells;
        private readonly Label _chargePct;
        private readonly Label _chargeTrendText;
        private readonly SmesPowerFlowRow _powerFlow;
        private readonly RateControlSection _inputControl;
        private readonly RateControlSection _outputControl;

        public SmesUnitBinder(VisualElement root)
        {
            _window = root.Q<MachineWindow>("machine-window");
            VisualElement contentRoot = root.Q<VisualElement>("smes-root") ?? root;

            _statusBanner = contentRoot.Q<SmesStatusBanner>("status-banner");
            _storageCells = contentRoot.Q<StorageCellRow>("storage-cells");
            _chargePct = contentRoot.Q<Label>("charge-pct");
            _chargeTrendText = contentRoot.Q<Label>("charge-trend-text");
            _powerFlow = contentRoot.Q<SmesPowerFlowRow>("power-flow");
            _inputControl = contentRoot.Q<RateControlSection>("input-control");
            _outputControl = contentRoot.Q<RateControlSection>("output-control");

            if (_window != null)
            {
                _window.CloseClicked += HandleWindowCloseClicked;
            }

            if (_inputControl != null)
            {
                _inputControl.SectionTitle = "INPUT";
                _inputControl.ToggleChanged += value =>
                    BoolControlChanged?.Invoke(MachineInterfaceControlIds.Smes.Input, value);
                _inputControl.RateDeltaRequested += delta =>
                    NumericControlChanged?.Invoke(MachineInterfaceControlIds.Smes.Input, delta);
            }

            if (_outputControl != null)
            {
                _outputControl.SectionTitle = "OUTPUT";
                _outputControl.ToggleChanged += value =>
                    BoolControlChanged?.Invoke(MachineInterfaceControlIds.Smes.Output, value);
                _outputControl.RateDeltaRequested += delta =>
                    NumericControlChanged?.Invoke(MachineInterfaceControlIds.Smes.Output, delta);
            }
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not SmesInterfaceViewModel model)
            {
                return;
            }

            if (_window != null)
            {
                _window.Title = model.Title;
            }

            StatusTone tone = SmesUnitBinderHelpers.GetStatusTone(model.State);
            _statusBanner?.SetContent(
                model.ExteriorStatusWord,
                model.StatusBadgeText,
                model.ConnectionStateText,
                tone);

            StatusTone chargeTone = SmesUnitBinderHelpers.GetChargeTone(model.ChargePct);
            _storageCells?.SetCharge(model.ChargePct, chargeTone);

            if (_chargePct != null)
            {
                _chargePct.text = $"{Mathf.RoundToInt(model.ChargePct * 100f)}%";
                StatusToneUtility.ApplyTone(_chargePct, chargeTone);
            }

            if (_chargeTrendText != null)
            {
                _chargeTrendText.text = SmesUnitBinderHelpers.GetTrendText(model.ChargeTrend);
            }

            StatusTone inputTone = SmesUnitBinderHelpers.GetInputTone(model);
            StatusTone outputTone = SmesUnitBinderHelpers.GetOutputTone(model);
            _powerFlow?.SetFlow(model.InputActive, model.OutputActive, inputTone, outputTone);

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
        }

        public void Disconnect()
        {
            if (_window != null)
            {
                _window.CloseClicked -= HandleWindowCloseClicked;
            }
        }

        private void HandleWindowCloseClicked()
        {
            CloseRequested?.Invoke();
        }
    }
}
