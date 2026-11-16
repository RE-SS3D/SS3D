using System;
using System.Collections.Generic;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;

namespace SS3D.Systems.Electricity.SystemsView
{
    // What PowerSystemsViewSource keeps between polls. These were nested types of the source; ClassSizeRatchetTests counts
    // a nested type's lines toward the class that holds it (a file counts toward the first type it declares), so as
    // top-level types in their own file they keep the source under the 900-line cap once Phase 4's analysis input
    // building joined it.

    internal readonly struct PowerSeen : IEquatable<PowerSeen>
    {
        public readonly SystemsViewKey Key;
        public readonly byte Kind;
        public readonly byte State;
        public readonly Direction Facing;
        public readonly ushort Room;
        /// <summary>A consumer's APC, compared by reference: the badge's circuit follows it.</summary>
        public readonly IApcChannelSource Apc;
        public readonly string Name;
        /// <summary>Its tile object, kept so a poll doesn't look it up again; not part of the comparison.</summary>
        public readonly PlacedTileObject Placed;

        public PowerSeen(SystemsViewKey key, byte kind, byte state, Direction facing, ushort room, IApcChannelSource apc, string name,
            PlacedTileObject placed)
        {
            Placed = placed;
            Key = key;
            Kind = kind;
            State = state;
            Facing = facing;
            Room = room;
            Apc = apc;
            Name = name;
        }

        public bool Equals(PowerSeen other) => Key.Equals(other.Key) && Kind == other.Kind && State == other.State
            && Facing == other.Facing && Room == other.Room && ReferenceEquals(Apc, other.Apc);
    }

    internal readonly struct PowerSeenRoom : IEquatable<PowerSeenRoom>
    {
        public readonly ushort AreaId;
        public readonly string Name;
        public readonly IApcChannelSource Apc;
        public readonly bool ApcBroken;

        public PowerSeenRoom(ushort areaId, string name, IApcChannelSource apc, bool apcBroken)
        {
            AreaId = areaId;
            Name = name;
            Apc = apc;
            ApcBroken = apcBroken;
        }

        public bool Equals(PowerSeenRoom other) => AreaId == other.AreaId && string.Equals(Name, other.Name, StringComparison.Ordinal)
            && ReferenceEquals(Apc, other.Apc) && ApcBroken == other.ApcBroken;
    }

    internal struct PowerRate
    {
        public long Tick;
        public float Energy;
        public float Kw;
    }

    internal sealed class PowerCircuitNetwork
    {
        public readonly List<SystemsViewKey> Segments = new();
        public readonly List<IElectricDevice> Devices = new();
        public Vector2Int First;
        public int CableTiles;
        public int ViewId;
        private bool _hasFirst;

        public void Add(SystemsViewKey key, Vector2Int tile, bool cable)
        {
            Segments.Add(key);
            if (cable)
                CableTiles++;
            if (!_hasFirst || SystemsViewProblems.CompareTiles(tile, First) < 0)
            {
                First = tile;
                _hasFirst = true;
            }
        }
    }
}
