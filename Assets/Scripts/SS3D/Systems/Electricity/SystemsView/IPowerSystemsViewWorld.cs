using System.Collections.Generic;
using SS3D.Systems.Tile;

namespace SS3D.Systems.Electricity.SystemsView
{
    /// <summary>
    /// What the Power view's source reads (Documents/architecture/2026-11_map-editor-systems-views.md Q1, Phase 3): the
    /// server's electricity state, never a client's own channel (every peer ticks its own, tech-debt P6.1).
    /// <c>ElectricitySubSystem</c> implements it on the server; EditMode tests give a hand-built one.
    /// </summary>
    public interface IPowerSystemsViewWorld
    {
        TileMap Map { get; }

        /// <summary>
        /// Moves whenever cable runs, circuits or room → APC links may differ: a cable placed or cleared, a circuit graph
        /// rebuild, a world suspension ending (a template load, <c>Resumed</c>), an area membership change. Device state
        /// needs no bump: the source polls it.
        /// </summary>
        int TopologyVersion { get; }

        /// <summary>
        /// Moves when consumer, APC or SMES state may have changed (<c>PowerStateVersion</c>). With neither version moved,
        /// the source reads only producers again.
        /// </summary>
        int StateVersion { get; }

        /// <summary>The server's electricity channel is paused, so traced numbers hold still on purpose.</summary>
        bool Paused { get; }

        /// <summary>Electricity ticks so far on the server channel, and their length, for SMES rates between two reads.</summary>
        long TickCount { get; }

        float TickSeconds { get; }

        /// <summary>Every registered electric device: grid devices and consumers alike.</summary>
        IReadOnlyList<IElectricDevice> Devices { get; }

        /// <summary>The circuit the electricity graph put <paramref name="device"/> in, as an identity token; null when none.</summary>
        object CircuitOf(IElectricDevice device);

        /// <summary>The APC a consumer draws from (its room's), from the electricity per-APC index.</summary>
        bool TryGetApc(IPowerConsumer consumer, out IApcChannelSource apc);

        /// <summary>The room a device is in (the tile in front of a wall mount), as its area id value.</summary>
        bool TryGetRoomOf(IElectricDevice device, out ushort areaId);

        /// <summary>Every room now, with its name and APC (null when it has none).</summary>
        void CollectRooms(List<PowerSystemsViewRoomInfo> into);

        /// <summary>An APC's room load and grid draw (<c>ElectricitySubSystem.TryGetApcCircuitStats</c>).</summary>
        bool TryGetApcStats(IApcChannelSource apc, out CircuitStats stats);
    }

    /// <summary>One room as the world reports it.</summary>
    public readonly struct PowerSystemsViewRoomInfo
    {
        public readonly ushort AreaId;
        public readonly string Name;
        public readonly IApcChannelSource Apc;

        public PowerSystemsViewRoomInfo(ushort areaId, string name, IApcChannelSource apc)
        {
            AreaId = areaId;
            Name = name;
            Apc = apc;
        }
    }
}
