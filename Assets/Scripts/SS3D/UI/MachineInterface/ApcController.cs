using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Audio;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Electricity;
using SS3D.Systems.StructuralDamage;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Area Power Controller with machine interface, local cell storage, and circuit channel gating.
    /// </summary>
    [RequireComponent(typeof(ElectricDeviceAdjacencyConnector))]
    [RequireComponent(typeof(AuthLogDeviceBehaviour))]
    public sealed class ApcController : AccessGatedMachineInterfaceBehaviour, IApcChannelSource, IPowerStorage, IAreaApcOrigin, IAmbientLoopState
    {
        [SerializeField]
        [Tooltip("Title prefix; the panel shows \"APC · {ROOM NAME}\" once the APC has a room.")]
        private string _title = "APC";

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

        private BreakableDevice _breakable;

        public override string InterfaceId => MachineInterfaceIds.Apc;

        protected override byte ReadIdControlId => MachineInterfaceControlIds.Apc.ReadId;

        /// <summary>A broken APC feeds nothing: every channel reads off, whatever its breakers say.</summary>
        public ApcControlFlags Channels => IsBroken ? ApcControlFlags.None : _channels;

        /// <summary>Smashed by rounds, swings or a blast (<see cref="BreakableDevice"/>); it stays on the wall, dead.</summary>
        public bool IsBroken => Breakable != null && Breakable.IsBroken;

        // Looked up lazily as well as in OnAwake: EditMode instances never run Awake.
        private BreakableDevice Breakable => _breakable != null ? _breakable : _breakable = GetComponent<BreakableDevice>();

        public PlacedTileObject TileObject => this ? GetComponent<PlacedTileObject>() : null;

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

        public bool IsLoopRunning => !IsBroken && _storedEnergyKwh > 0f;

        public float MaxDeliverableKw(float tickSeconds) => IsBroken
            ? 0f
            : PowerStorageMath.MaxDeliverableKw(_storedEnergyKwh, _maxDischargeRateKw, tickSeconds);

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            // Nothing to open on a dead panel.
            return IsBroken ? System.Array.Empty<IInteraction>() : base.CreateTargetInteractions(interactionEvent);
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            if (TryGetComponent(out _breakable))
            {
                _breakable.OnBrokenChanged += HandleBrokenChanged;
            }
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
            if (electricitySystem.IsReady)
            {
                RegisterWithElectricity(electricitySystem);
            }
            else
            {
                electricitySystem.WhenReady += OnElectricitySystemSetup;
            }

            if (SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                if (areaSubSystem.IsReady)
                {
                    areaSubSystem.RegisterApc(this);
                }
                else
                {
                    areaSubSystem.WhenReady += OnAreaSystemSetup;
                }
            }
        }

        public float AddPowerKw(float requestedKw, float tickSeconds) => IsBroken
            ? 0f
            : PowerStorageMath.AddPowerKw(
                ref _storedEnergyKwh,
                _maxCapacityKwh,
                _maxChargeRateKw,
                requestedKw,
                tickSeconds);

        public float RemovePowerKw(float requestedKw, float tickSeconds) => IsBroken
            ? 0f
            : PowerStorageMath.RemovePowerKw(
                ref _storedEnergyKwh,
                _maxDischargeRateKw,
                requestedKw,
                tickSeconds);

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
            if (!AccessGranted || IsBroken)
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

        // Unregister here, not in OnDestroyed: FishNet clears IsServer during despawn, before Unity's
        // OnDestroy, so an IsServer-gated unregister there never ran and a deleted APC kept powering
        // its room (and blocked a replacement APC with "A room can have only one APC").
        public override void OnStopServer()
        {
            if (SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                areaSubSystem.WhenReady -= OnAreaSystemSetup;
                areaSubSystem.UnregisterApc(this);
            }

            if (SubSystems.TryGet(out ElectricitySubSystem electricitySystem))
            {
                electricitySystem.WhenReady -= OnElectricitySystemSetup;
                electricitySystem.RemoveElectricalElement(this);
            }

            base.OnStopServer();
        }

        protected override void OnDestroyed()
        {
            if (_breakable != null)
                _breakable.OnBrokenChanged -= HandleBrokenChanged;

            if (SubSystems.TryGet(out AreaSubSystem areaSubSystem))
                areaSubSystem.WhenReady -= OnAreaSystemSetup;

            if (SubSystems.TryGet(out ElectricitySubSystem electricitySystem))
                electricitySystem.WhenReady -= OnElectricitySystemSetup;

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
            DispatchClientOpen(conn, snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, ApcInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(conn, snapshot);
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

            string areaName = MachineAreaLabel.GetAreaNameOrNull(this);
            ApcInterfaceSnapshot snapshot = new()
            {
                MachineObjectId = NetworkObject.ObjectId,
                InterfaceId = InterfaceId,
                Title = ComposeTitle(areaName),
                AreaName = areaName ?? string.Empty,
                Channels = Channels,
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
                AccessDenied = AccessDenied,
            };

            ApplyDiagnostics(ref snapshot, stats, powerState, batteryState);
            return snapshot;
        }

        private string ComposeTitle(string areaName) =>
            string.IsNullOrEmpty(areaName) ? _title : $"{_title} · {areaName.ToUpperInvariant()}";

        private void OnAreaSystemSetup()
        {
            if (SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                areaSubSystem.WhenReady -= OnAreaSystemSetup;
                areaSubSystem.RegisterApc(this);
            }
        }

        private void OnElectricitySystemSetup()
        {
            if (SubSystems.TryGet(out ElectricitySubSystem electricitySystem))
            {
                electricitySystem.WhenReady -= OnElectricitySystemSetup;
                RegisterWithElectricity(electricitySystem);
            }
        }

        private void RegisterWithElectricity(ElectricitySubSystem electricitySystem)
        {
            electricitySystem.AddElectricalElement(this);
        }

        private void HandleBrokenChanged(bool broken)
        {
            if (IsServer)
            {
                RefreshAfterChannelsChange();
            }
        }

        private void OnChannelsChanged(ApcControlFlags oldValue, ApcControlFlags newValue, bool asServer)
        {
            if (asServer)
            {
                RefreshAfterChannelsChange();
            }
        }

        private void RefreshAfterChannelsChange()
        {
            if (SubSystems.TryGet(out AreaSubSystem areaSubSystem))
            {
                areaSubSystem.RefreshAreaLightingStates();
            }

            RefreshAllViewers();
            // Server recomputes fixture visuals and SyncVars them to clients.
            LightPower.RefreshAllFixtures();
        }
    }
}
