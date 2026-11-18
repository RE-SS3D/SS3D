using SS3D.Systems.Health;
using SS3D.UI.MachineInterface.Components;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    /// <summary>The VSU-5R vitals screen. Read-only: its one action is closing.</summary>
    public sealed class HealthScanBinder : IMachineInterfaceBinder
    {
#pragma warning disable CS0067 // A scan has no controls; the interface asks for these events.
        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        public event Action<byte, int> ActionControlChanged;
#pragma warning restore CS0067

        public event Action CloseRequested;

        private readonly DiegeticDeviceShell _shell;
        private readonly ConnectionStatusRow _connectionRow;
        private readonly DeviceIdentityBlock _identity;
        private readonly GlanceableStatusChip _statusChip;
        private readonly HealthScanDoll _doll;
        private readonly VisualElement _bloodFill;
        private readonly VisualElement _bloodBulb;
        private readonly Label _bloodValue;
        private readonly Label _brainValue;
        private readonly Label _shockValue;
        private readonly HealthScanGauge _toxinGauge;
        private readonly Label _toxinValue;
        private readonly Label _spo2Value;
        private readonly HealthScanTrace _spo2Trace;
        private readonly Label _rhythmValue;
        private readonly VisualElement _organList;
        private readonly DeviceFooter _footer;
        private readonly List<OrganRow> _organRows = new();

        public HealthScanBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement queryRoot = _shell?.ScreenContent ?? root;

            _connectionRow = queryRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = queryRoot.Q<DeviceIdentityBlock>("identity");
            _statusChip = queryRoot.Q<GlanceableStatusChip>("status-chip");
            _doll = queryRoot.Q<HealthScanDoll>("doll");
            _bloodFill = queryRoot.Q<VisualElement>("blood-fill");
            _bloodBulb = queryRoot.Q<VisualElement>("blood-bulb");
            _bloodValue = queryRoot.Q<Label>("blood-value");
            _brainValue = queryRoot.Q<Label>("brain-value");
            _shockValue = queryRoot.Q<Label>("shock-value");
            _toxinGauge = queryRoot.Q<HealthScanGauge>("toxin-gauge");
            _toxinValue = queryRoot.Q<Label>("toxin-value");
            _spo2Value = queryRoot.Q<Label>("spo2-value");
            _spo2Trace = queryRoot.Q<HealthScanTrace>("spo2-trace");
            _rhythmValue = queryRoot.Q<Label>("rhythm-value");
            _organList = queryRoot.Q<VisualElement>("organ-list");
            _footer = queryRoot.Q<DeviceFooter>("footer");

            if (_shell != null)
            {
                _shell.CloseClicked += HandleCloseRequested;
            }
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not HealthScanViewModel model)
            {
                return;
            }

            if (_shell != null)
            {
                _shell.ModelLabel = model.ModelLabel;
                _shell.PowerOk = true;
            }

            if (_connectionRow != null)
            {
                _connectionRow.StatusText = "SCAN COMPLETE";
                _connectionRow.ReadoutText = model.DeviceTitle;
                _connectionRow.DotTone = StatusTone.Success;
            }

            if (_identity != null)
            {
                _identity.Title = model.SubjectLine;
                _identity.Subtitle = model.ScanMetaText;
            }

            _statusChip?.SetContent(model.StatusHeadline, model.StatusBadgeText, model.StatusTone, model.StatusSubline);
            _doll?.Bind(model);
            BindSystemic(model);
            BindOrgans(model.Organs);

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

        private void BindSystemic(HealthScanViewModel model)
        {
            UnityEngine.Color bloodColor = HealthScanSnapshotMapper.ToneColor(model.BloodTone);
            if (_bloodFill != null)
            {
                _bloodFill.style.height = new Length(model.BloodFill * 100f, LengthUnit.Percent);
                _bloodFill.style.backgroundColor = bloodColor;
            }

            if (_bloodBulb != null)
            {
                _bloodBulb.style.backgroundColor = bloodColor;
            }

            SetValue(_bloodValue, model.BloodText, model.BloodTone);

            HealthScanOrganView brain = model.Organs[(int)OrganType.Brain];
            SetValue(_brainValue, brain.PercentText, brain.Tone);
            SetValue(_shockValue, model.ShockText, model.ShockTone);

            _toxinGauge?.SetValue((model.ToxinNeedleDegrees + 90f) / 180f);
            SetValue(_toxinValue, model.ToxinText, model.ToxinTone);
            SetValue(_spo2Value, model.SpO2Text, model.SpO2Tone);

            bool flat = model.NoCardiacOutput;
            _spo2Trace?.SetTrace(
                HealthScanSnapshotMapper.ToneColor(flat ? StatusTone.Danger : model.SpO2Tone == StatusTone.Neutral ? StatusTone.Success : model.SpO2Tone),
                flat);
            SetValue(_rhythmValue, model.HeartRhythmText, flat ? StatusTone.Danger : StatusTone.Neutral);
        }

        private void BindOrgans(HealthScanOrganView[] organs)
        {
            if (_organList == null)
            {
                return;
            }

            while (_organRows.Count < organs.Length)
            {
                OrganRow row = new();
                _organRows.Add(row);
                _organList.Add(row.Root);
            }

            for (int i = 0; i < organs.Length; i++)
            {
                _organRows[i].Bind(organs[i]);
            }
        }

        private static void SetValue(Label label, string text, StatusTone tone)
        {
            if (label == null)
            {
                return;
            }

            label.text = text;
            StatusToneUtility.ApplyTone(label, tone);
        }

        private void HandleCloseRequested() => CloseRequested?.Invoke();

        private sealed class OrganRow
        {
            private readonly VisualElement _dot;
            private readonly Label _label;
            private readonly Label _value;

            public OrganRow()
            {
                Root = new VisualElement();
                Root.AddToClassList("health-scan-organ-row");

                _dot = new VisualElement();
                _dot.AddToClassList("health-scan-organ-row__dot");
                Root.Add(_dot);

                _label = new Label();
                _label.AddToClassList("health-scan-organ-row__label");
                _label.AddToClassList("font-terminal");
                Root.Add(_label);

                _value = new Label();
                _value.AddToClassList("health-scan-organ-row__value");
                _value.AddToClassList("font-terminal");
                Root.Add(_value);
            }

            public VisualElement Root { get; }

            public void Bind(HealthScanOrganView organ)
            {
                _dot.style.backgroundColor = organ.Fill;
                _label.text = organ.Label;
                _value.text = organ.PercentText;
                _value.style.color = organ.Fill;
            }
        }
    }
}
