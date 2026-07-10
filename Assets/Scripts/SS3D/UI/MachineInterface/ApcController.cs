using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Area;
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
    public sealed class ApcController : MachineInterfaceBehaviour, IApcChannelSource, IPowerStorage, IAreaApcOrigin
    {
        private const float CriticalBatteryThreshold = 0.15f;

        [SerializeField]
        private string _title = "APC · ENGINEERING BAY";

        [SerializeField]
        private float _maxCapacity = 5f;

        [SerializeField]
        private float _maxPowerRate = 5f;

        [SyncVar(OnChange = nameof(OnChannelsChanged))]
        private ApcControlFlags _channels = ApcControlFlags.All;

        [SyncVar]
        private float _storedPower;

        private bool _multipleApcsInArea;

        public override string InterfaceId => MachineInterfaceIds.Apc;

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

        public float StoredPower
        {
            get => _storedPower;
            set => _storedPower = Mathf.Clamp(value, 0f, MaxCapacity);
        }

        public float MaxCapacity => _maxCapacity;

        public float RemainingCapacity => Mathf.Max(0f, _maxCapacity - _storedPower);

        public float MaxPowerRate => _maxPowerRate;

        public float MaxRemovablePower => Mathf.Min(_storedPower, _maxPowerRate);

        public bool IsOn => true;

        public void SetMultipleApcsInArea(bool value)
        {
            _multipleApcsInArea = value;
        }

        public override void OnStartServer()
        {
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

        public float AddPower(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            float addedAmount = Mathf.Min(RemainingCapacity, amount);
            _storedPower += addedAmount;
            return addedAmount;
        }

        public float RemovePower(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            float removedAmount = Mathf.Min(_storedPower, amount);
            _storedPower -= removedAmount;
            return removedAmount;
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
            ApcControlFlags flag = controlId switch
            {
                0 => ApcControlFlags.Lighting,
                2 => ApcControlFlags.Environment,
                _ => ApcControlFlags.Equipment,
            };

            if (value)
            {
                _channels |= flag;
            }
            else
            {
                _channels &= ~flag;
            }

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

        private static ApcPowerState DerivePowerState(CircuitStats stats)
        {
            if (stats.ApcBatteryCharge <= CriticalBatteryThreshold && !stats.GridMeetsLoad)
            {
                return ApcPowerState.Critical;
            }

            if (!stats.GridMeetsLoad || stats.BatteryDraining)
            {
                return ApcPowerState.Overload;
            }

            return ApcPowerState.Nominal;
        }

        private static ApcBatteryState DeriveBatteryState(CircuitStats stats)
        {
            if (stats.ApcBatteryCharge <= CriticalBatteryThreshold)
            {
                return ApcBatteryState.Critical;
            }

            if (stats.BatteryDraining)
            {
                return ApcBatteryState.Discharging;
            }

            return ApcBatteryState.Charged;
        }

        private static void ApplyDiagnostics(ref ApcInterfaceSnapshot snapshot, CircuitStats stats, ApcPowerState powerState, ApcBatteryState batteryState)
        {
            List<ApcDiagnosticSnapshot> diagnostics = new();

            if (stats.TotalSupplyKw <= 0f)
            {
                diagnostics.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = "X",
                    Text = "No external power detected.",
                    Tone = (byte)StatusTone.Danger,
                });
            }
            else if (!stats.GridMeetsLoad)
            {
                diagnostics.Add(new ApcDiagnosticSnapshot
                {
                    Glyph = "!",
                    Text = "Grid supply below connected load.",
                    Tone = (byte)StatusTone.Warning,
                });
            }
            else
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
                electricitySubSystem.TryGetCircuitStats(this, this, out stats);
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
