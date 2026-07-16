using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Electricity;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Networked SMES machine interface controller. Reads battery/circuit state and exposes input/output controls.
    /// </summary>
    [RequireComponent(typeof(SmesBattery))]
    [RequireComponent(typeof(AuthLogDeviceBehaviour))]
    public sealed class SmesController : AccessGatedMachineInterfaceBehaviour
    {
        private const float CriticalChargeThreshold = 0.05f;
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

        protected override byte ReadIdControlId => MachineInterfaceControlIds.Smes.ReadId;

        public override void OnStartServer()
        {
            _battery = GetComponent<SmesBattery>();
            base.OnStartServer();
            ApplyOutputEnabled(_outputEnabled);
            ApplyInputSettings();
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
            if (!AccessGranted)
            {
                return false;
            }

            switch (controlId)
            {
                case MachineInterfaceControlIds.Smes.Input:
                {
                    _inputEnabled = value;
                    return true;
                }

                case MachineInterfaceControlIds.Smes.Output:
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
            if (!AccessGranted)
            {
                return false;
            }

            switch (controlId)
            {
                case MachineInterfaceControlIds.Smes.Input:
                {
                    _inputMaxKw = Mathf.Clamp(_inputMaxKw + delta, MinRateKw, MaxRateKw);
                    return true;
                }

                case MachineInterfaceControlIds.Smes.Output:
                {
                    _outputMaxKw = Mathf.Clamp(_outputMaxKw + delta, MinRateKw, MaxRateKw);
                    if (_battery != null)
                    {
                        _battery.Init(_outputMaxKw, _battery.MaxCapacityKwh, _battery.StoredEnergyKwh, _inputEnabled ? _inputMaxKw : 0f);
                    }

                    return true;
                }

                default:
                {
                    return false;
                }
            }
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

            float chargePct = _battery != null && _battery.MaxCapacityKwh > 0f
                ? _battery.StoredEnergyKwh / _battery.MaxCapacityKwh
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
                AccessGranted = AccessGranted,
                AccessScanning = AccessScanning,
                AccessDenied = AccessDenied,
            };

            return snapshot;
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
                ApplyInputSettings();
            }

            RefreshAllViewers();
        }

        private void OnFloatControlChanged(float oldValue, float newValue, bool asServer)
        {
            if (!asServer)
            {
                return;
            }

            if (_battery != null)
            {
                _battery.Init(_outputMaxKw, _battery.MaxCapacityKwh, _battery.StoredEnergyKwh, _inputEnabled ? _inputMaxKw : 0f);
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

        private void ApplyInputSettings()
        {
            if (_battery == null)
            {
                return;
            }

            _battery.MaxChargeRateKw = _inputEnabled ? _inputMaxKw : 0f;
        }
    }
}
