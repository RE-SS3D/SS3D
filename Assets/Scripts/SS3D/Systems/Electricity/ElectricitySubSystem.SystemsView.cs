using System.Collections.Generic;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Electricity.SystemsView;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor.SystemsView;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Offers the map editor's Power view its source (Documents/architecture/2026-11_map-editor-systems-views.md Q10), read
    /// from this server's circuits, devices and rooms (never a client's own electricity channel, tech-debt P6.1). The
    /// editor hub is spawned on another object with no fixed start order, so the server retries from each electricity
    /// tick until it finds one, and offers again if that hub is replaced, as the atmos source does.
    /// </summary>
    public partial class ElectricitySubSystem
    {
        private PowerSystemsViewSource _systemsViewSource;
        private readonly SystemsViewRegistration _systemsViewRegistration = new();
        // Moves on a cable edit, a circuit rebuild, a world resumption and an area membership change.
        private int _systemsViewTopology;

        /// <summary>Server: the Power view's structure source; null on a pure client.</summary>
        public PowerSystemsViewSource SystemsViewSource => _systemsViewSource;

        /// <summary>
        /// Whether the server's electricity channel is paused. Phase 5's admin pause sets the channel's <c>Paused</c>
        /// (with a step to come, <c>StepOnce</c>); the Power view's trace reads it here to say "Paused".
        /// </summary>
        public bool SimulationPaused => _channel != null && _channel.Paused;

        /// <summary>What the Power view's source compares to know cable runs, circuits or room links may have changed.</summary>
        public int SystemsViewTopologyVersion => _systemsViewTopology;

        private void BumpSystemsViewTopology() => _systemsViewTopology++;

        /// <summary>Server: a fresh Power view source over this subsystem's state, for budget measurements (the visual pilot).</summary>
        public PowerSystemsViewSource CreateSystemsViewSource() => new(new SystemsViewWorld(this));

        private void EnsureSystemsViewRegistered()
        {
            if (!IsServer)
                return;

            _systemsViewSource ??= CreateSystemsViewSource();
            _systemsViewRegistration.Ensure(_systemsViewSource);
        }

        private void UnregisterSystemsView() => _systemsViewRegistration.Release(_systemsViewSource);

        private SystemsViewRoomPower _systemsViewRooms;

        /// <summary>
        /// Server: each device's room and whether it has power, as the Power view hatches rooms (none, or a broken APC),
        /// for the Atmos view's "device in a room with no power" (Phase 4); the atmos side reads only this interface.
        /// </summary>
        public ISystemsViewRooms SystemsViewRooms => _systemsViewRooms ??= new SystemsViewRoomPower(this);

        private sealed class SystemsViewRoomPower : ISystemsViewRooms
        {
            private readonly ElectricitySubSystem _electricity;

            public SystemsViewRoomPower(ElectricitySubSystem electricity) => _electricity = electricity;

            // Room membership and APC links move the topology; an APC registered, removed, broken or repaired moves the state.
            public int Version => unchecked(_electricity.SystemsViewTopologyVersion + PowerStateVersion.Current);

            public bool TryGetRoom(PlacedTileObject device, out ushort areaId, out string name)
            {
                areaId = 0;
                name = null;
                if (device == null || !SubSystems.TryGet(out AreaSubSystem areas)
                    || !areas.TryGetAreaForDevice(device, out AreaRecord record) || record == null || record.Id.IsNone)
                    return false;

                areaId = record.Id.Value;
                name = record.DisplayName;
                return true;
            }

            public bool HasPower(ushort areaId)
            {
                if (areaId == 0 || !SubSystems.TryGet(out AreaSubSystem areas) || !areas.TryGetAreaApc(new AreaId(areaId), out IApcChannelSource apc)
                    || apc is not UnityEngine.Component component || component == null)
                    return false;

                return !component.TryGetComponent(out BreakableDevice breakable) || !breakable.IsBroken;
            }
        }

        /// <summary>The server's electricity state as the Power view's source reads it.</summary>
        private sealed class SystemsViewWorld : IPowerSystemsViewWorld
        {
            private readonly ElectricitySubSystem _electricity;

            public SystemsViewWorld(ElectricitySubSystem electricity) => _electricity = electricity;

            public TileMap Map => SubSystems.TryGet(out TileSubSystem tiles) ? tiles.CurrentMap : null;

            public int TopologyVersion => _electricity.SystemsViewTopologyVersion;

            public bool Paused => _electricity.SimulationPaused;

            public bool CircuitsCurrent => !_electricity._graphIsDirty;

            public int StateVersion => PowerStateVersion.Current;

            // The server channel's clock, for the trace's SMES rates.
            public long TickCount => _electricity._channel?.TickCount ?? 0;

            public float TickSeconds => _electricity._tickRate;

            public IReadOnlyList<IElectricDevice> Devices => _electricity._registeredDevices;

            public object CircuitOf(IElectricDevice device) => _electricity.TryGetCircuitForDevice(device);

            public bool TryGetApc(IPowerConsumer consumer, out IApcChannelSource apc) =>
                _electricity.TryGetIndexedApcForConsumer(consumer, out apc);

            public bool TryGetRoomOf(IElectricDevice device, out ushort areaId)
            {
                areaId = 0;
                if (!SubSystems.TryGet(out AreaSubSystem areas) || device?.TileObject is not { } placed
                    || !areas.TryGetAreaForDevice(placed, out AreaRecord record) || record == null)
                    return false;

                areaId = record.Id.Value;
                return areaId != AreaId.None;
            }

            public void CollectRooms(List<PowerSystemsViewRoomInfo> into)
            {
                if (!SubSystems.TryGet(out AreaSubSystem areas))
                    return;

                IReadOnlyList<AreaRecord> records = areas.GetAllAreas();
                for (int i = 0; i < records.Count; i++)
                {
                    AreaRecord record = records[i];
                    into.Add(new PowerSystemsViewRoomInfo(record.Id.Value, record.DisplayName, record.Apc as IApcChannelSource));
                }
            }

            public bool TryGetApcStats(IApcChannelSource apc, out CircuitStats stats) =>
                _electricity.TryGetApcCircuitStats(apc, apc as IPowerStorage, out stats);
        }
    }
}
