using SS3D.UI.MachineInterface.Components;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public sealed class IdConsoleBinder : IMachineInterfaceBinder
    {
        public event Action CloseRequested;

        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        public event Action<byte, int> ActionControlChanged;

        private readonly DiegeticDeviceShell _shell;
        private readonly DeviceIdentityBlock _identity;
        private readonly Label _promptLabel;
        private readonly Label _targetLabel;
        private readonly VisualElement _departmentLevels;
        private readonly VisualElement _crossCuttingLevels;
        private readonly ActionLog _editLog;
        private readonly DeviceFooter _footer;
        private readonly List<TogglePill> _togglePills = new();

        public IdConsoleBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement contentRoot = _shell ?? root;

            _identity = contentRoot.Q<DeviceIdentityBlock>("identity");
            _promptLabel = contentRoot.Q<Label>("prompt-label");
            _targetLabel = contentRoot.Q<Label>("target-label");
            _departmentLevels = contentRoot.Q<VisualElement>("department-levels");
            _crossCuttingLevels = contentRoot.Q<VisualElement>("cross-cutting-levels");
            _editLog = contentRoot.Q<ActionLog>("edit-log");
            _footer = contentRoot.Q<DeviceFooter>("footer");

            if (_shell != null)
            {
                _shell.CloseClicked += HandleCloseRequested;
            }
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not IdConsoleInterfaceViewModel model)
            {
                return;
            }

            if (_shell != null)
            {
                _shell.ModelLabel = model.ModelLabel;
                _shell.PowerOk = true;
            }

            if (_identity != null)
            {
                _identity.Title = model.Title;
                _identity.Subtitle = model.Subtitle;
            }

            if (_promptLabel != null)
            {
                _promptLabel.text = model.PromptText;
            }

            if (_targetLabel != null)
            {
                _targetLabel.text = model.HasTargetCard
                    ? $"{model.TargetName} — {model.TargetJob}"
                    : "No target card inserted.";
            }

            RebuildAccessToggles(model);
            BindEditLog(model);
        }

        public void Disconnect()
        {
            if (_shell != null)
            {
                _shell.CloseClicked -= HandleCloseRequested;
            }

            ClearToggles();
        }

        private static int PackToggle(byte levelIndex, bool enabled)
        {
            return (levelIndex << 1) | (enabled ? 1 : 0);
        }

        private void RebuildAccessToggles(IdConsoleInterfaceViewModel model)
        {
            ClearToggles();

            if (!model.EditorUnlocked || !model.HasTargetCard)
            {
                return;
            }

            foreach (IdConsoleAccessLevelViewData level in model.Levels)
            {
                VisualElement parent = level.IsDepartment ? _departmentLevels : _crossCuttingLevels;
                if (parent == null)
                {
                    continue;
                }

                VisualElement row = new();
                row.AddToClassList("id-console__level-row");

                Label nameLabel = new(level.Name);
                nameLabel.AddToClassList("id-console__level-name");
                nameLabel.AddToClassList("font-titling");

                TogglePill toggle = new()
                {
                    IsOn = level.Enabled,
                    OnLabel = "GRANTED",
                    OffLabel = "REVOKED",
                };
                toggle.SetEnabled(model.EditorUnlocked && model.HasTargetCard);

                byte index = level.Index;
                toggle.ValueChanged += enabled =>
                    ActionControlChanged?.Invoke(
                        MachineInterfaceControlIds.IdConsole.ToggleAccessLevel,
                        PackToggle(index, enabled));

                row.Add(nameLabel);
                row.Add(toggle);
                parent.Add(row);
                _togglePills.Add(toggle);
            }
        }

        private void BindEditLog(IdConsoleInterfaceViewModel model)
        {
            if (_editLog == null)
            {
                return;
            }

            _editLog.SetEntries(model.EditLog);
        }

        private void ClearToggles()
        {
            _togglePills.Clear();
            _departmentLevels?.Clear();
            _crossCuttingLevels?.Clear();
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }
    }
}
