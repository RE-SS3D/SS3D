using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Networked SMES machine interface controller. Reads battery/circuit state and exposes input/output controls.
    /// </summary>
    [RequireComponent(typeof(SmesBattery))]
    public sealed class SmesController : MachineInterfaceBehaviour
    {
        private const float CriticalChargeThreshold = 0.05f;
        private const float LowChargeThreshold = 0.15f;
        private const float MinRateKw = 1f;
        private const float MaxRateKw = 50f;
        private const float RateStepKw = 1f;

        [SerializeField]
        private string _title = "SMES · ENERGY STORAGE";

        [SyncVar(OnChange = nameof(OnBoolControlChanged))]
        private bool _inputEnabled = true;

        [SyncVar(OnChange = nameof(OnBoolControlChanged))]
        private bool _outputEnabled = true;

        [SyncVar(OnChange = nameof(OnFloatControlChanged))]
        private float _inputMaxKw = 10f;

        [SyncVar(OnChange = nameof(OnFloatControlChanged))]
        private float _outputMaxKw = 10f;

        private SmesBattery _battery;

        public override string InterfaceId => MachineInterfaceIds.Smes;

        public override void OnStartServer()
        {
            _battery = GetComponent<SmesBattery>();
            base.OnStartServer();

            ElectricitySubSystem electricitySystem = SubSystems.Get<ElectricitySubSystem>();
            if (electricitySystem.IsSetUp)
            {
                RegisterWithElectricity(electricitySystem);
            }
            else
            {
                electricitySystem.OnSystemSetUp += OnElectricitySystemSetup;
            }

            ApplyOutputEnabled(_outputEnabled);
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
            switch (controlId)
            {
                case 0:
                {
                    _inputEnabled = value;
                    return true;
                }

                case 1:
                {
                    _outputEnabled = value;
                    ApplyOutputEnabled(value);
                    return true;
                }

                default:
                {
                    return false;
                }
            }
        }

        protected override bool ApplyNumericControl(byte controlId, float delta)
        {
            switch (controlId)
            {
                case 0:
                {
                    _inputMaxKw = Mathf.Clamp(_inputMaxKw + delta, MinRateKw, MaxRateKw);
                    return true;
                }

                case 1:
                {
                    _outputMaxKw = Mathf.Clamp(_outputMaxKw + delta, MinRateKw, MaxRateKw);
                    if (_battery != null)
                    {
                        _battery.Init(_outputMaxKw, _battery.MaxCapacity, _battery.StoredPower);
                    }

                    return true;
                }

                default:
                {
                    return false;
                }
            }
        }

        protected override void OnDestroyed()
        {
            if (SubSystems.TryGet(out ElectricitySubSystem electricitySystem))
            {
                electricitySystem.RemoveElectricalElement(_battery);
                electricitySystem.OnSystemSetUp -= OnElectricitySystemSetup;
            }

            base.OnDestroyed();
        }

        private static SmesPowerState DerivePowerState(float chargePct, CircuitStats stats, bool inputActive, bool outputActive)
        {
            if (chargePct <= CriticalChargeThreshold && !inputActive)
            {
                return SmesPowerState.Fault;
            }

            if (!inputActive && outputActive)
            {
                return SmesPowerState.Degraded;
            }

            if (stats.TotalDemandKw > stats.TotalSupplyKw + 0.01f && outputActive)
            {
                return SmesPowerState.Overload;
            }

            return SmesPowerState.Nominal;
        }

        private static SmesChargeTrend DeriveChargeTrend(SmesPowerState powerState, CircuitStats stats)
        {
            return powerState switch
            {
                SmesPowerState.Fault => SmesChargeTrend.Critical,
                SmesPowerState.Overload => SmesChargeTrend.DrainingFast,
                SmesPowerState.Degraded when stats.BatteryDraining => SmesChargeTrend.Draining,
                _ => SmesChargeTrend.Steady,
            };
        }

        private static string BuildConnectionStateText(SmesPowerState powerState, bool inputActive, bool outputActive)
        {
            return powerState switch
            {
                SmesPowerState.Fault => "Both links down — unit in protective shutdown.",
                SmesPowerState.Degraded => "Input link down — output still served from storage.",
                SmesPowerState.Overload => "Distribution grid drawing more than input supplies.",
                _ when inputActive && outputActive => "Grid link nominal — both connections healthy.",
                _ when inputActive => "Input connected — output idle.",
                _ when outputActive => "Output active — no upstream input detected.",
                _ => "Standing by.",
            };
        }

        private static void ApplyWarnings(
            ref SmesInterfaceSnapshot snapshot,
            SmesPowerState powerState,
            CircuitStats stats,
            float chargePct,
            bool inputActive)
        {
            List<ApcDiagnosticSnapshot> warnings = new();

            if (!inputActive && snapshot.OutputActive)
            {
                warnings.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = "!",
                    Text = "No grid connection detected.",
                    Tone = (byte)StatusTone.Danger,
                });
                snapshot.DiagnosisHint = "The SMES is not the fault — check the upstream generator or grid cable feeding this unit.";
            }

            if (powerState == SmesPowerState.Overload)
            {
                warnings.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = "!",
                    Text = "Output exceeds sustainable generation.",
                    Tone = (byte)StatusTone.Warning,
                });
                snapshot.DiagnosisHint = "The grid is overloaded, not the SMES — reduce distribution demand or bring another generator online.";
            }

            if (stats.BatteryDraining)
            {
                warnings.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = "!",
                    Text = "Battery discharge increasing.",
                    Tone = (byte)StatusTone.Warning,
                });
            }

            if (chargePct <= LowChargeThreshold)
            {
                warnings.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = "X",
                    Text = "Charge critical — connect input immediately.",
                    Tone = (byte)StatusTone.Danger,
                });
            }

            if (powerState == SmesPowerState.Fault)
            {
                warnings.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = "X",
                    Text = "Cell bank overheating — output disabled.",
                    Tone = (byte)StatusTone.Danger,
                });
                snapshot.DiagnosisHint = "The SMES itself has faulted — restore input power, then let it cool before re-enabling output.";
            }

            snapshot.WarningCount = Mathf.Min(warnings.Count, SmesInterfaceSnapshot.MaxWarnings);
            for (int i = 0; i < snapshot.WarningCount; i++)
            {
                SetWarning(ref snapshot, i, warnings[i]);
            }
        }

        private static void SetWarning(ref SmesInterfaceSnapshot snapshot, int index, ApcDiagnosticSnapshot warning)
        {
            switch (index)
            {
                case 0:
                {
                    snapshot.Warning0 = warning;
                    break;
                }

                case 1:
                {
                    snapshot.Warning1 = warning;
                    break;
                }

                case 2:
                {
                    snapshot.Warning2 = warning;
                    break;
                }

                case 3:
                {
                    snapshot.Warning3 = warning;
                    break;
                }
            }
        }

        [TargetRpc(RunLocally = true)]
        private void TargetOpenInterface(NetworkConnection conn, SmesInterfaceSnapshot snapshot)
        {
            DispatchClientOpen(snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, SmesInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(snapshot);
        }

        private SmesInterfaceSnapshot BuildSnapshot()
        {
            if (_battery == null)
            {
                _battery = GetComponent<SmesBattery>();
            }

            CircuitStats stats = default;
            if (SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem) && _battery != null)
            {
                electricitySubSystem.TryGetCircuitStats(_battery, _battery, out stats);
            }

            float chargePct = _battery != null && _battery.MaxCapacity > 0f
                ? _battery.StoredPower / _battery.MaxCapacity
                : 0f;

            float inputKw = Mathf.Min(stats.TotalSupplyKw, _inputMaxKw);
            float outputKw = Mathf.Min(stats.TotalDemandKw, _outputMaxKw);
            bool inputActive = _inputEnabled && stats.TotalSupplyKw > 0f;
            bool outputActive = _outputEnabled && _battery is { IsOn: true } && outputKw > 0f;

            SmesPowerState powerState = DerivePowerState(chargePct, stats, inputActive, outputActive);
            SmesChargeTrend chargeTrend = DeriveChargeTrend(powerState, stats);

            SmesInterfaceSnapshot snapshot = new()
            {
                MachineObjectId = NetworkObject.ObjectId,
                InterfaceId = InterfaceId,
                Title = _title,
                PowerState = (byte)powerState,
                ChargePct = chargePct,
                ChargeTrend = (byte)chargeTrend,
                InputCurrentKw = inputActive ? inputKw : 0f,
                OutputCurrentKw = outputActive ? outputKw : 0f,
                InputMaxKw = _inputMaxKw,
                OutputMaxKw = _outputMaxKw,
                InputEnabled = _inputEnabled,
                OutputEnabled = _outputEnabled,
                InputActive = inputActive,
                OutputActive = outputActive,
                ConnectionStateText = BuildConnectionStateText(powerState, inputActive, outputActive),
                DiagnosisHint = string.Empty,
            };

            ApplyWarnings(ref snapshot, powerState, stats, chargePct, inputActive);
            return snapshot;
        }

        private void OnElectricitySystemSetup()
        {
            if (SubSystems.TryGet(out ElectricitySubSystem electricitySystem))
            {
                RegisterWithElectricity(electricitySystem);
            }
        }

        private void RegisterWithElectricity(ElectricitySubSystem electricitySystem)
        {
            if (_battery == null)
            {
                _battery = GetComponent<SmesBattery>();
            }

            electricitySystem.AddElectricalElement(_battery);
        }

        private void OnBoolControlChanged(bool oldValue, bool newValue, bool asServer)
        {
            if (!asServer)
            {
                return;
            }

            if (_battery != null)
            {
                ApplyOutputEnabled(_outputEnabled);
            }

            RefreshAllViewers();
        }

        private void OnFloatControlChanged(float oldValue, float newValue, bool asServer)
        {
            if (!asServer)
            {
                return;
            }

            RefreshAllViewers();
        }

        private void ApplyOutputEnabled(bool enabled)
        {
            if (_battery != null)
            {
                _battery.IsOn = enabled;
            }
        }
    }
}
