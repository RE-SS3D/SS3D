using SS3D.Systems.Health;
using SS3D.UI.MachineInterface.Components;
using System;
using System.Collections.Generic;
using UnityEngine;
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

        /// <summary>Organs in the right-hand list, in the design's order; the brain has its own spot on the left.</summary>
        private static readonly (OrganType Type, string Icon)[] ListedOrgans =
        {
            (OrganType.Heart, "heart"),
            (OrganType.LeftLung, "lungs"),
            (OrganType.RightLung, "lungs"),
            (OrganType.Liver, "liver"),
        };

        private readonly DiegeticDeviceShell _shell;
        private readonly Label _title;
        private readonly StatusBadge _statusBadge;
        private readonly Label _subText;
        private readonly HealthScanDoll _doll;
        private readonly VisualElement _bloodFill;
        private readonly VisualElement _bloodBulb;
        private readonly Label _bloodValue;
        private readonly Label _bleedValue;
        private readonly VisualElement _brainIcon;
        private readonly Label _brainValue;
        private readonly HealthScanGauge _toxinGauge;
        private readonly Label _toxinValue;
        private readonly Label _spo2Value;
        private readonly HealthScanTrace _trace;
        private readonly Label _rhythmValue;
        private readonly VisualElement _organList;
        private readonly Label _shockValue;
        private readonly VisualElement _shockFill;
        private readonly VisualElement _shockPending;
        private readonly VisualElement[] _shockMarkers;
        private readonly VisualElement[] _shockLabels;
        private readonly VisualElement _woundList;
        private readonly Label _noWounds;
        private readonly DeviceFooter _footer;
        private readonly List<OrganRow> _organRows = new();
        private readonly List<WoundRow> _woundRows = new();

        private HealthScanViewModel _model;

        public HealthScanBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement q = _shell?.ScreenContent ?? root;

            _title = q.Q<Label>("title");
            _statusBadge = q.Q<StatusBadge>("status-badge");
            _subText = q.Q<Label>("subtext");
            _doll = q.Q<HealthScanDoll>("doll");
            _bloodFill = q.Q<VisualElement>("blood-fill");
            _bloodBulb = q.Q<VisualElement>("blood-bulb");
            _bloodValue = q.Q<Label>("blood-value");
            _bleedValue = q.Q<Label>("bleed-value");
            _brainIcon = q.Q<VisualElement>("brain-icon");
            _brainValue = q.Q<Label>("brain-value");
            _toxinGauge = q.Q<HealthScanGauge>("toxin-gauge");
            _toxinValue = q.Q<Label>("toxin-value");
            _spo2Value = q.Q<Label>("spo2-value");
            _trace = q.Q<HealthScanTrace>("spo2-trace");
            _rhythmValue = q.Q<Label>("rhythm-value");
            _organList = q.Q<VisualElement>("organ-list");
            _shockValue = q.Q<Label>("shock-value");
            _shockFill = q.Q<VisualElement>("shock-fill");
            _shockPending = q.Q<VisualElement>("shock-pending");
            _shockMarkers = new[] { q.Q<VisualElement>("shock-stand"), q.Q<VisualElement>("shock-downed"), q.Q<VisualElement>("shock-critical") };
            _shockLabels = new[] { q.Q<VisualElement>("shock-stand-label"), q.Q<VisualElement>("shock-downed-label"), q.Q<VisualElement>("shock-critical-label") };
            _woundList = q.Q<VisualElement>("wound-list");
            _noWounds = q.Q<Label>("no-wounds");
            _footer = q.Q<DeviceFooter>("footer");

            if (_shell != null)
            {
                _shell.CloseClicked += HandleCloseRequested;
            }

            if (_doll != null)
            {
                _doll.PinChanged += RefreshWoundPins;
            }
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not HealthScanViewModel model)
            {
                return;
            }

            _model = model;
            if (_shell != null)
            {
                _shell.ModelLabel = model.ModelLabel;
                _shell.PowerOk = true;
            }

            if (_title != null)
            {
                _title.text = model.TitleText.ToUpperInvariant();
            }

            if (_statusBadge != null)
            {
                _statusBadge.Text = model.StatusText.ToUpperInvariant();
                _statusBadge.Tone = model.StatusTone;
            }

            if (_subText != null)
            {
                _subText.text = model.SubText;
            }

            _doll?.Bind(model);
            BindLeft(model);
            BindRight(model);
            BindShock(model);
            BindWounds(model);

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

            if (_doll != null)
            {
                _doll.PinChanged -= RefreshWoundPins;
            }
        }

        private void BindLeft(HealthScanViewModel model)
        {
            Color blood = HealthScanSnapshotMapper.ToneColor(model.BloodTone);
            if (_bloodFill != null)
            {
                _bloodFill.style.height = new Length(model.BloodFill * 100f, LengthUnit.Percent);
                _bloodFill.style.backgroundColor = blood;
            }

            if (_bloodBulb != null)
            {
                _bloodBulb.style.backgroundColor = blood;
            }

            SetValue(_bloodValue, model.BloodText, blood);
            SetValue(_bleedValue, model.BleedText, HealthScanSnapshotMapper.ToneColor(model.BleedTone));

            HealthScanOrganView brain = model.Organs[(int)OrganType.Brain];
            if (_brainIcon != null)
            {
                _brainIcon.style.unityBackgroundImageTintColor = brain.Fill;
            }

            SetValue(_brainValue, brain.PercentText, brain.Fill);
        }

        private void BindRight(HealthScanViewModel model)
        {
            _toxinGauge?.SetValue(model.ToxinFraction);
            SetValue(_toxinValue, model.ToxinText, HealthScanSnapshotMapper.ToneColor(model.ToxinTone));
            SetValue(_spo2Value, model.SpO2Text, HealthScanSnapshotMapper.ToneColor(model.SpO2Tone));

            Color rhythm = HealthScanSnapshotMapper.ToneColor(model.RhythmTone);
            _trace?.SetTrace(model.Rhythm, rhythm);
            SetValue(_rhythmValue, model.RhythmText, rhythm);

            if (_organList == null)
            {
                return;
            }

            while (_organRows.Count < ListedOrgans.Length)
            {
                OrganRow row = new(ListedOrgans[_organRows.Count].Icon);
                _organRows.Add(row);
                _organList.Add(row.Root);
            }

            for (int i = 0; i < ListedOrgans.Length; i++)
            {
                _organRows[i].Bind(model.Organs[(int)ListedOrgans[i].Type]);
            }
        }

        private void BindShock(HealthScanViewModel model)
        {
            Color tone = HealthScanSnapshotMapper.ToneColor(model.ShockTone);
            SetValue(_shockValue, model.ShockText, tone);
            if (_shockFill != null)
            {
                _shockFill.style.width = Percent(model.ShockFraction);
                _shockFill.style.backgroundColor = tone;
            }

            if (_shockPending != null)
            {
                _shockPending.style.left = Percent(model.ShockFraction);
                _shockPending.style.width = Percent(model.IncomingFraction);
                _shockPending.style.backgroundColor = tone;
            }

            float[] markers = { model.StandMarker, model.DownedMarker, model.CriticalMarker };
            for (int i = 0; i < markers.Length; i++)
            {
                if (_shockMarkers[i] != null)
                {
                    _shockMarkers[i].style.left = Percent(markers[i]);
                }

                if (_shockLabels[i] != null)
                {
                    _shockLabels[i].style.left = Percent(markers[i]);
                }
            }
        }

        private void BindWounds(HealthScanViewModel model)
        {
            if (_woundList == null)
            {
                return;
            }

            while (_woundRows.Count < model.WoundZones.Count)
            {
                WoundRow row = new(zone => _doll?.PinZone(zone));
                _woundRows.Add(row);
                _woundList.Add(row.Root);
            }

            for (int i = 0; i < _woundRows.Count; i++)
            {
                bool used = i < model.WoundZones.Count;
                _woundRows[i].Root.style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
                if (used)
                {
                    _woundRows[i].Bind(model.WoundZones[i], model.Zones[model.WoundZones[i]]);
                }
            }

            if (_noWounds != null)
            {
                _noWounds.style.display = model.WoundZones.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            RefreshWoundPins();
        }

        private void RefreshWoundPins()
        {
            int pinned = _doll?.PinnedZone ?? -1;
            foreach (WoundRow row in _woundRows)
            {
                row.Root.EnableInClassList("health-scan-wound--pinned", row.Zone == pinned);
            }
        }

        private static StyleLength Percent(float fraction) => new Length(Mathf.Clamp01(fraction) * 100f, LengthUnit.Percent);

        private static void SetValue(Label label, string text, Color color)
        {
            if (label == null)
            {
                return;
            }

            label.text = text;
            label.style.color = color;
        }

        private void HandleCloseRequested() => CloseRequested?.Invoke();

        private sealed class OrganRow
        {
            private readonly VisualElement _icon;
            private readonly Label _label;
            private readonly Label _value;

            public OrganRow(string icon)
            {
                Root = new VisualElement();
                Root.AddToClassList("health-scan-organ-row");

                _icon = new VisualElement();
                _icon.AddToClassList("health-scan-organ-icon");
                _icon.AddToClassList($"health-scan-organ-icon--{icon}");
                Root.Add(_icon);

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
                _icon.style.unityBackgroundImageTintColor = organ.Fill;
                _label.text = organ.Label;
                _value.text = organ.PercentText;
                _value.style.color = organ.Fill;
            }
        }

        private sealed class WoundRow
        {
            private readonly Label _zone;
            private readonly Label _tier;
            private readonly Label _numbers;
            private readonly Label _flags;

            public WoundRow(Action<int> pin)
            {
                Button button = new(() => pin(Zone));
                button.AddToClassList("health-scan-wound");
                Root = button;

                _zone = AddLabel("health-scan-wound__zone");
                _tier = AddLabel("health-scan-wound__tier");
                _numbers = AddLabel("health-scan-wound__numbers");
                _flags = AddLabel("health-scan-wound__flags");
            }

            public VisualElement Root { get; }

            public int Zone { get; private set; } = -1;

            public void Bind(int zone, HealthScanZoneView view)
            {
                Zone = zone;
                _zone.text = view.Label.ToUpperInvariant();
                _tier.text = view.TierLabel.ToUpperInvariant();
                _tier.style.color = HealthScanSnapshotMapper.ToneColor(view.TierTone);
                _numbers.text = view.NumbersText;
                _flags.text = view.FlagsText;
                _flags.style.color = HealthScanSnapshotMapper.ToneColor(view.Bleeding ? StatusTone.Danger : StatusTone.Neutral);
            }

            private Label AddLabel(string className)
            {
                Label label = new();
                label.AddToClassList(className);
                label.AddToClassList("font-body");
                label.pickingMode = PickingMode.Ignore;
                Root.Add(label);
                return label;
            }
        }
    }
}
