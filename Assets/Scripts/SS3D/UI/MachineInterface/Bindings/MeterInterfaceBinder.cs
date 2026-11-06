using SS3D.UI.MachineInterface.Components;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public sealed class MeterInterfaceBinder : IMachineInterfaceBinder
    {
#pragma warning disable CS0067 // A meter has no controls; the interface asks for these events.
        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        public event Action<byte, int> ActionControlChanged;
#pragma warning restore CS0067

        public event Action CloseRequested;

        private readonly DiegeticDeviceShell _shell;
        private readonly ConnectionStatusRow _connectionRow;
        private readonly DeviceIdentityBlock _identity;
        private readonly GlanceableStatusChip _statusChip;
        private readonly Label _pressureValue;
        private readonly VisualElement _pressureCard;
        private readonly VisualElement _pressureSafe;
        private readonly VisualElement _pressureFill;
        private readonly Label _scaleMid;
        private readonly Label _scaleMax;
        private readonly GasHistoryBars _history;
        private readonly Label _trend;
        private readonly Label _temperature;
        private readonly Label _moles;
        private readonly Label _volume;
        private readonly Label _registryText;
        private readonly GasMixBar _mixBar;
        private readonly VisualElement _gasList;
        private readonly DeviceFooter _footer;
        private readonly List<GasCompositionRow> _gasRows = new();

        public MeterInterfaceBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement queryRoot = _shell?.ScreenContent ?? root;

            _connectionRow = queryRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = queryRoot.Q<DeviceIdentityBlock>("identity");
            _statusChip = queryRoot.Q<GlanceableStatusChip>("status-chip");
            _pressureValue = queryRoot.Q<Label>("pressure-value");
            _pressureCard = queryRoot.Q<VisualElement>("pressure-card");
            _pressureSafe = queryRoot.Q<VisualElement>("pressure-safe");
            _pressureFill = queryRoot.Q<VisualElement>("pressure-fill");
            _scaleMid = queryRoot.Q<Label>("scale-mid");
            _scaleMax = queryRoot.Q<Label>("scale-max");
            _history = queryRoot.Q<GasHistoryBars>("history");
            _trend = queryRoot.Q<Label>("trend");
            _temperature = queryRoot.Q<Label>("stat-temperature");
            _moles = queryRoot.Q<Label>("stat-moles");
            _volume = queryRoot.Q<Label>("stat-volume");
            _registryText = queryRoot.Q<Label>("registry-text");
            _mixBar = queryRoot.Q<GasMixBar>("mix-bar");
            _gasList = queryRoot.Q<VisualElement>("gas-list");
            _footer = queryRoot.Q<DeviceFooter>("footer");

            if (_shell != null)
            {
                _shell.CloseClicked += HandleCloseRequested;
            }
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not MeterInterfaceViewModel model)
            {
                return;
            }

            if (_shell != null)
            {
                _shell.ModelLabel = model.ModelLabel;
                _shell.PowerOk = model.Scenario is MeterScenario.Nominal or MeterScenario.Vacuum;
            }

            if (_connectionRow != null)
            {
                _connectionRow.StatusText = model.LineLabel;
                _connectionRow.ReadoutText = string.Empty;
                _connectionRow.DotTone = model.ConnectionTone;
            }

            if (_identity != null)
            {
                _identity.Title = model.DeviceTitle;
                _identity.Subtitle = model.Subtitle;
            }

            _statusChip?.SetContent(model.StatusHeadline, model.StatusBadgeText, model.StatusTone, model.StatusSubline);
            BindPressure(model);
            BindStats(model);

            if (_registryText != null)
            {
                _registryText.text = model.RegistryText;
            }

            _mixBar?.SetSegments(model.Segments);
            BindGases(model.Gases);

            if (_footer != null)
            {
                _footer.Text = model.FooterText;
            }
        }

        public void Disconnect()
        {
            if (_shell != null)
            {
                _shell.CloseClicked -= HandleCloseRequested;
            }
        }

        private void BindPressure(MeterInterfaceViewModel model)
        {
            if (_pressureValue != null)
            {
                _pressureValue.text = model.PressureValue;
                StatusToneUtility.ApplyTone(_pressureValue, model.PressureTone);
            }

            _pressureCard?.EnableInClassList("gas-meter-pressure--over", model.Scenario == MeterScenario.Overpressure);

            if (_pressureSafe != null)
            {
                _pressureSafe.style.left = new Length(model.SafeStartPercent, LengthUnit.Percent);
                _pressureSafe.style.width = new Length(model.SafeWidthPercent, LengthUnit.Percent);
            }

            if (_pressureFill != null)
            {
                _pressureFill.style.width = new Length(model.PressurePercent, LengthUnit.Percent);
                StatusToneUtility.ApplyTone(_pressureFill, model.PressureTone);
            }

            if (_scaleMid != null)
            {
                _scaleMid.text = model.ScaleMidText;
            }

            if (_scaleMax != null)
            {
                _scaleMax.text = model.ScaleMaxText;
            }

            _history?.SetBars(model.HistoryBars, model.PressureTone);

            if (_trend != null)
            {
                _trend.text = model.TrendText;
            }
        }

        private void BindStats(MeterInterfaceViewModel model)
        {
            if (_temperature != null)
            {
                _temperature.text = model.TemperatureText;
                StatusToneUtility.ApplyTone(_temperature, model.TemperatureTone);
            }

            if (_moles != null)
            {
                _moles.text = model.MolesText;
            }

            if (_volume != null)
            {
                _volume.text = model.VolumeText;
            }
        }

        private void BindGases(IReadOnlyList<GasCompositionRowView> gases)
        {
            if (_gasList == null)
            {
                return;
            }

            while (_gasRows.Count < gases.Count)
            {
                GasCompositionRow row = new();
                _gasRows.Add(row);
                _gasList.Add(row);
            }

            for (int i = 0; i < gases.Count; i++)
            {
                _gasRows[i].Bind(gases[i]);
            }
        }

        private void HandleCloseRequested() => CloseRequested?.Invoke();
    }
}
