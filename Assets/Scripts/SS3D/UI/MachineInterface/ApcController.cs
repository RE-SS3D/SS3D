using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using System.Electricity;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Area Power Controller with machine interface, local cell storage, and circuit channel gating.
    /// </summary>
    [RequireComponent(typeof(ElectricDeviceAdjacencyConnector))]
    [RequireComponent(typeof(AuthLogDeviceBehaviour))]
    public sealed class ApcController : AccessGatedMachineInterfaceBehaviour, IApcChannelSource, IPowerStorage, IAreaApcOrigin
    {
        [SerializeField]
        private string _title = "APC · ENGINEERING BAY";

        [SerializeField]
        private float _maxCapacityKwh = 5f;

        [SerializeField]
        private float _maxDischargeRateKw = 10f;

        [SerializeField]
        private float _maxChargeRateKw = 10f;

        [SyncVar(OnChange = nameof(OnChannelsChanged))]
        private ApcControlFlags _channels = ApcControlFlags.All;

        [SyncVar]
        private float _storedEnergyKwh;

        private bool _multipleApcsInArea;

        public override string InterfaceId => MachineInterfaceIds.Apc;

        protected override byte ReadIdControlId => MachineInterfaceControlIds.Apc.ReadId;

        public ApcControlFlags Channels => _channels;

        public PlacedTileObject TileObject => GetComponent<PlacedTileObject>();

        public TileCoord OriginTile
        {
            get
            {
                PlacedTileObject tileObject = TileObject;
                Vector2Int origin = tileObject != null ? tileObject.WorldOrigin : Vector2Int.zero;
                int mapId = tileObject != null ? tileObject.MapId : 0;
                return new TileCoord(mapId, origin);
            }
        }

        public Direction FacingDirection => TileObject != null ? TileObject.Direction : Direction.North;

        public string DisplayName => _title;

        public float StoredEnergyKwh
        {
            get => _storedEnergyKwh;
            set => _storedEnergyKwh = Mathf.Clamp(value, 0f, MaxCapacityKwh);
        }

        public float MaxCapacityKwh => _maxCapacityKwh;

        public float RemainingCapacityKwh => Mathf.Max(0f, _maxCapacityKwh - _storedEnergyKwh);

        public float MaxDischargeRateKw => _maxDischargeRateKw;

        public float MaxChargeRateKw => _maxChargeRateKw;

        public bool IsOn => true;

        public float MaxDeliverableKw(float tickSeconds)
        {
            if (_storedEnergyKwh <= 0f)
            {
                return 0f;
            }

            return Mathf.Min(_maxDischargeRateKw, ElectricityUnits.KwhToKw(_storedEnergyKwh, tickSeconds));
        }

        public void SetMultipleApcsInArea(bool value)
        {
            _multipleApcsInArea = value;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _storedEnergyKwh = _maxCapacityKwh;

            ElectricitySubSystem electricitySystem = SubSystems.Get<ElectricitySubSystem>();
            if (electricitySystem.IsSetUp)
            {
                RegisterWithElectricity(electricitySystem);
            }
            else
            {
                electricitySystem.OnSystemSetUp += OnElectricitySystemSetup;
            }

            if (SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                if (areaSubSystem.IsSetUp)
                {
                    areaSubSystem.RegisterApc(this);
                }
                else
                {
                    areaSubSystem.OnSystemSetUp += OnAreaSystemSetup;
                }
            }
        }

        public float AddPowerKw(float requestedKw, float tickSeconds)
        {
            if (requestedKw <= 0f || RemainingCapacityKwh <= 0f || _maxChargeRateKw <= 0f)
            {
                return 0f;
            }

            float absorbedKw = Mathf.Min(requestedKw, _maxChargeRateKw);
            float energyToAdd = ElectricityUnits.KwToKwh(absorbedKw, tickSeconds);
            float addedEnergy = Mathf.Min(RemainingCapacityKwh, energyToAdd);
            _storedEnergyKwh += addedEnergy;
            return ElectricityUnits.KwhToKw(addedEnergy, tickSeconds);
        }

        public float RemovePowerKw(float requestedKw, float tickSeconds)
        {
            if (requestedKw <= 0f || _storedEnergyKwh <= 0f)
            {
                return 0f;
            }

            float deliverableKw = MaxDeliverableKw(tickSeconds);
            float deliveredKw = Mathf.Min(requestedKw, deliverableKw);
            float removedEnergy = ElectricityUnits.KwToKwh(deliveredKw, tickSeconds);
            _storedEnergyKwh -= removedEnergy;
            return deliveredKw;
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

            ApcControlFlags flag = controlId switch
            {
                MachineInterfaceControlIds.Apc.Lighting => ApcControlFlags.Lighting,
                MachineInterfaceControlIds.Apc.Environment => ApcControlFlags.Environment,
                MachineInterfaceControlIds.Apc.Equipment => ApcControlFlags.Equipment,
                _ => ApcControlFlags.None,
            };

            if (flag == ApcControlFlags.None)
            {
                return false;
            }

            _channels = value ? _channels | flag : _channels & ~flag;
            RefreshAllViewers();
            return true;
        }

        protected override void OnDestroyed()
        {
            if (SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                areaSubSystem.OnSystemSetUp -= OnAreaSystemSetup;
                areaSubSystem.UnregisterApc(this);
            }

            if (SubSystems.TryGet(out ElectricitySubSystem electricitySystem))
            {
                electricitySystem.RemoveElectricalElement(this);
                electricitySystem.OnSystemSetUp -= OnElectricitySystemSetup;
            }

            base.OnDestroyed();
        }

        private static ApcPowerState DerivePowerState(CircuitStats stats) =>
            ApcStatusDeriver.DerivePowerState(stats);

        private static ApcBatteryState DeriveBatteryState(CircuitStats stats) =>
            ApcStatusDeriver.DeriveBatteryState(stats);

        private static void ApplyDiagnostics(ref ApcInterfaceSnapshot snapshot, CircuitStats stats, ApcPowerState powerState, ApcBatteryState batteryState)
        {
            List<ApcDiagnosticSnapshot> diagnostics = new();

            if (stats.GridAvailableKw <= 0f && stats.TotalDemandKw > 0f)
            {
                diagnostics.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = "X",
                    Text = "No external power detected.",
                    Tone = (byte)StatusTone.Danger,
                });
            }
            else if (stats.TotalDemandKw > 0f && !stats.GridMeetsLoad)
            {
                diagnostics.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = "!",
                    Text = "Grid supply below connected load.",
                    Tone = (byte)StatusTone.Warning,
                });
            }
            else if (stats.TotalDemandKw > 0f || stats.GridAvailableKw > 0f)
            {
                diagnostics.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = ">",
                    Text = "External power available.",
                    Tone = (byte)StatusTone.Success,
                });
            }

            if (batteryState == ApcBatteryState.Critical)
            {
                diagnostics.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = "!",
                    Text = "Cell charge critical.",
                    Tone = (byte)StatusTone.Danger,
                });
            }
            else if (batteryState == ApcBatteryState.Discharging)
            {
                diagnostics.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = ">",
                    Text = "Cell compensating deficit.",
                    Tone = (byte)StatusTone.Warning,
                });
            }

            if (powerState == ApcPowerState.Critical)
            {
                diagnostics.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = ">",
                    Text = "Consider shedding non-critical channels.",
                    Tone = (byte)StatusTone.Info,
                });
            }

            if (snapshot.MultipleApcsInArea)
            {
                diagnostics.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = "!",
                    Text = "Multiple APCs share this flood-filled region.",
                    Tone = (byte)StatusTone.Warning,
                });
            }

            snapshot.DiagnosticCount = Mathf.Min(diagnostics.Count, ApcInterfaceSnapshot.MaxDiagnostics);
            for (int i = 0; i < snapshot.DiagnosticCount; i++)
            {
                SetDiagnostic(ref snapshot, i, diagnostics[i]);
            }
        }

        private static void SetDiagnostic(ref ApcInterfaceSnapshot snapshot, int index, ApcDiagnosticSnapshot diagnostic)
        {
            switch (index)
            {
                case 0:
                {
                    snapshot.Diagnostic0 = diagnostic;
                    break;
                }

                case 1:
                {
                    snapshot.Diagnostic1 = diagnostic;
                    break;
                }

                case 2:
                {
                    snapshot.Diagnostic2 = diagnostic;
                    break;
                }

                case 3:
                {
                    snapshot.Diagnostic3 = diagnostic;
                    break;
                }

                case 4:
                {
                    snapshot.Diagnostic4 = diagnostic;
                    break;
                }

                case 5:
                {
                    snapshot.Diagnostic5 = diagnostic;
                    break;
                }
            }
        }

        [TargetRpc(RunLocally = true)]
        private void TargetOpenInterface(NetworkConnection conn, ApcInterfaceSnapshot snapshot)
        {
            DispatchClientOpen(snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, ApcInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(snapshot);
        }

        private ApcInterfaceSnapshot BuildSnapshot()
        {
            CircuitStats stats = default;
            if (SubSystems.TryGet(out ElectricitySubSystem electricitySubSystem))
            {
                electricitySubSystem.TryGetApcCircuitStats(this, this, out stats);
            }

            ApcPowerState powerState = DerivePowerState(stats);
            ApcBatteryState batteryState = DeriveBatteryState(stats);

            ApcInterfaceSnapshot snapshot = new()
            {
                MachineObjectId = NetworkObject.ObjectId,
                InterfaceId = InterfaceId,
                Title = _title,
                Channels = _channels,
                PowerState = (byte)powerState,
                GridInputKw = stats.TotalSupplyKw,
                LoadOutputKw = stats.TotalDemandKw,
                BatteryCharge = stats.ApcBatteryCharge,
                BatteryState = (byte)batteryState,
                LightingLoadKw = stats.LightingLoadKw,
                EquipmentLoadKw = stats.EquipmentLoadKw,
                EnvironmentLoadKw = stats.EnvironmentLoadKw,
                MultipleApcsInArea = _multipleApcsInArea,
                AccessGranted = AccessGranted,
                AccessScanning = AccessScanning,
            };

            ApplyDiagnostics(ref snapshot, stats, powerState, batteryState);
            return snapshot;
        }

        private void OnAreaSystemSetup()
        {
            if (SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                areaSubSystem.RegisterApc(this);
                areaSubSystem.OnSystemSetUp -= OnAreaSystemSetup;
            }
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
            electricitySystem.AddElectricalElement(this);
        }

        private void OnChannelsChanged(ApcControlFlags oldValue, ApcControlFlags newValue, bool asServer)
        {
            if (!asServer)
            {
                return;
            }

            RefreshAllViewers();
        }
    }
}
