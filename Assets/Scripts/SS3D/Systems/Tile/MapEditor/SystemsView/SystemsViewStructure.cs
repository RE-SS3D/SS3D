using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>Which systems view the map editor shows (creative-mode §4). Values go on the wire: never renumber.</summary>
    public enum SystemsViewKind : byte
    {
        Off = 0,
        Atmos = 1,
        Power = 2,
    }

    /// <summary>
    /// Where a structure entry sits: a tile plus a slot the domain picks (a pipe line, the cable layer, a device's
    /// layer, a problem kind), so two entries on one tile don't collide. One entry per key in each collection.
    /// </summary>
    public struct SystemsViewKey : IEquatable<SystemsViewKey>
    {
        public Vector2Int Tile;
        public byte Slot;

        public SystemsViewKey(Vector2Int tile, byte slot = 0)
        {
            Tile = tile;
            Slot = slot;
        }

        public bool Equals(SystemsViewKey other) => Tile == other.Tile && Slot == other.Slot;
        public override bool Equals(object obj) => obj is SystemsViewKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Tile, Slot);
        public override string ToString() => $"{Tile}/{Slot}";
    }

    /// <summary>One pipe or cable tile and the network it belongs to.</summary>
    public struct SystemsViewSegment : IEquatable<SystemsViewSegment>
    {
        public SystemsViewKey Key;
        /// <summary>From <see cref="NetworkIdentityTracker{TSegment}"/>; also the palette index (<see cref="SystemsViewPalette"/>).</summary>
        public int ViewId;
        /// <summary>
        /// Which sides of its tile the run leaves by, as <see cref="SystemsViewLinks"/> bits: the real connections,
        /// including into a device port or a manifold, so the overlay draws what is joined rather than what is adjacent.
        /// </summary>
        public byte Links;

        public bool Equals(SystemsViewSegment other) => Key.Equals(other.Key) && ViewId == other.ViewId && Links == other.Links;
        public override bool Equals(object obj) => obj is SystemsViewSegment other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Key, ViewId, Links);
    }

    /// <summary>The four sides of a tile as bits of <see cref="SystemsViewSegment.Links"/>.</summary>
    public static class SystemsViewLinks
    {
        /// <summary>The four cardinal directions, in bit order.</summary>
        public static readonly Direction[] Sides = { Direction.North, Direction.East, Direction.South, Direction.West };

        public const byte North = 1;
        public const byte East = 2;
        public const byte South = 4;
        public const byte West = 8;

        /// <summary>The bit for a cardinal direction; 0 for a diagonal.</summary>
        public static byte Bit(Direction direction) => direction switch
        {
            Direction.North => North,
            Direction.East => East,
            Direction.South => South,
            Direction.West => West,
            _ => 0,
        };

        public static bool Has(byte links, Direction direction) => (links & Bit(direction)) != 0;
    }

    /// <summary>A network's name label ("Supply 3", "Circuit 2"), so nothing depends on colour alone.</summary>
    public struct SystemsViewNetwork : IEquatable<SystemsViewNetwork>
    {
        public int ViewId;
        public string Label;

        public bool Equals(SystemsViewNetwork other) => ViewId == other.ViewId && string.Equals(Label, other.Label, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SystemsViewNetwork other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ViewId, Label);
    }

    /// <summary>
    /// A device's state marking. <see cref="Kind"/> and <see cref="State"/> are domain codes (the atmos and power
    /// sources define them); a device between two networks carries both view ids, <see cref="ViewId"/> at its back
    /// and <see cref="OtherViewId"/> at its front, with <see cref="Facing"/> giving which way that is.
    /// </summary>
    public struct SystemsViewBadge : IEquatable<SystemsViewBadge>
    {
        public SystemsViewKey Key;
        public byte Kind;
        public byte State;
        public Direction Facing;
        public int ViewId;
        /// <summary>0 when the device sits on one network.</summary>
        public int OtherViewId;
        /// <summary>A three-port device's third network (a filter's or mixer's side port); 0 for every other device.</summary>
        public int SideViewId;

        /// <summary>Ports in <see cref="ViewIdAt"/>: back, front, side.</summary>
        public const int PortCount = 3;

        /// <summary>The network at one port: 0 back (<see cref="ViewId"/>), 1 front, 2 side; 0 when none.</summary>
        public int ViewIdAt(int port) => port switch
        {
            0 => ViewId,
            1 => OtherViewId,
            2 => SideViewId,
            _ => 0,
        };

        /// <summary>The first port on network <paramref name="viewId"/>, or -1 when the device doesn't touch it.</summary>
        public int PortOf(int viewId)
        {
            for (int port = 0; viewId > 0 && port < PortCount; port++)
            {
                if (ViewIdAt(port) == viewId)
                    return port;
            }

            return -1;
        }

        /// <summary>Whether any port is on one of <paramref name="viewIds"/>.</summary>
        public bool TouchesAny(ICollection<int> viewIds)
        {
            for (int port = 0; viewIds != null && port < PortCount; port++)
            {
                int viewId = ViewIdAt(port);
                if (viewId > 0 && viewIds.Contains(viewId))
                    return true;
            }

            return false;
        }

        public bool Equals(SystemsViewBadge other) => Key.Equals(other.Key) && Kind == other.Kind && State == other.State
            && Facing == other.Facing && ViewId == other.ViewId && OtherViewId == other.OtherViewId && SideViewId == other.SideViewId;
        public override bool Equals(object obj) => obj is SystemsViewBadge other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Key, Kind, State, Facing, ViewId, OtherViewId, SideViewId);
    }

    /// <summary>A problem marker. <see cref="IssueId"/> is the same id Map Check reports, so both share one wording.</summary>
    public struct SystemsViewMarker : IEquatable<SystemsViewMarker>
    {
        public SystemsViewKey Key;
        public string IssueId;

        public bool Equals(SystemsViewMarker other) => Key.Equals(other.Key) && string.Equals(IssueId, other.IssueId, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SystemsViewMarker other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Key, IssueId);
    }

    /// <summary>
    /// What changed in one view since the last delta a client was sent (Documents/architecture/2026-11_map-editor-systems-views.md Q2).
    /// <see cref="Reset"/> means "clear, then apply": the first send of a subscription. Arrays are never null.
    /// </summary>
    public struct SystemsViewDelta
    {
        public SystemsViewKind View;
        public bool Reset;
        public SystemsViewSegment[] SegmentsSet;
        public SystemsViewKey[] SegmentsRemoved;
        public SystemsViewNetwork[] NetworksSet;
        public int[] NetworksRemoved;
        public SystemsViewBadge[] BadgesSet;
        public SystemsViewKey[] BadgesRemoved;
        public SystemsViewMarker[] MarkersSet;
        public SystemsViewKey[] MarkersRemoved;

        /// <summary>Nothing to send: not a reset and no entry set or removed.</summary>
        public bool IsEmpty => !Reset
            && Length(SegmentsSet) + Length(SegmentsRemoved) + Length(NetworksSet) + Length(NetworksRemoved)
            + Length(BadgesSet) + Length(BadgesRemoved) + Length(MarkersSet) + Length(MarkersRemoved) == 0;

        private static int Length(Array array) => array?.Length ?? 0;
    }

    /// <summary>
    /// The structure stream of one view (Q2): segment → view id, view id → label, device badges and problem markers.
    /// A source builds a fresh one per <see cref="ISystemsViewSource.BuildStructure"/>; the server keeps one per
    /// subscriber as "what that client was sent", and the client keeps the one it has applied.
    /// </summary>
    public sealed class SystemsViewStructure
    {
        public SystemsViewStructure(SystemsViewKind view) => View = view;

        public SystemsViewKind View { get; private set; }
        public Dictionary<SystemsViewKey, SystemsViewSegment> Segments { get; } = new();
        public Dictionary<int, SystemsViewNetwork> Networks { get; } = new();
        public Dictionary<SystemsViewKey, SystemsViewBadge> Badges { get; } = new();
        public Dictionary<SystemsViewKey, SystemsViewMarker> Markers { get; } = new();

        public void Set(SystemsViewSegment segment) => Segments[segment.Key] = segment;
        public void Set(SystemsViewNetwork network) => Networks[network.ViewId] = network;
        public void Set(SystemsViewBadge badge) => Badges[badge.Key] = badge;
        public void Set(SystemsViewMarker marker) => Markers[marker.Key] = marker;

        public void Clear()
        {
            Segments.Clear();
            Networks.Clear();
            Badges.Clear();
            Markers.Clear();
        }

        /// <summary>
        /// What turns <paramref name="sent"/> into <paramref name="current"/>. A null <paramref name="sent"/>, or one
        /// of another view, gives a <see cref="SystemsViewDelta.Reset"/> carrying everything in <paramref name="current"/>.
        /// </summary>
        public static SystemsViewDelta Diff(SystemsViewStructure sent, SystemsViewStructure current)
        {
            if (current == null)
                throw new ArgumentNullException(nameof(current));

            bool reset = sent == null || sent.View != current.View;
            SystemsViewDelta delta = new() { View = current.View, Reset = reset };
            DiffInto(reset ? null : sent.Segments, current.Segments, out delta.SegmentsSet, out delta.SegmentsRemoved);
            DiffInto(reset ? null : sent.Networks, current.Networks, out delta.NetworksSet, out delta.NetworksRemoved);
            DiffInto(reset ? null : sent.Badges, current.Badges, out delta.BadgesSet, out delta.BadgesRemoved);
            DiffInto(reset ? null : sent.Markers, current.Markers, out delta.MarkersSet, out delta.MarkersRemoved);
            return delta;
        }

        /// <summary>Applies a delta from <see cref="Diff"/>; a reset clears first and takes the delta's view.</summary>
        public void Apply(SystemsViewDelta delta)
        {
            if (delta.Reset)
            {
                Clear();
                View = delta.View;
            }

            ApplyInto(Segments, delta.SegmentsSet, delta.SegmentsRemoved, segment => segment.Key);
            ApplyInto(Networks, delta.NetworksSet, delta.NetworksRemoved, network => network.ViewId);
            ApplyInto(Badges, delta.BadgesSet, delta.BadgesRemoved, badge => badge.Key);
            ApplyInto(Markers, delta.MarkersSet, delta.MarkersRemoved, marker => marker.Key);
        }

        private static void DiffInto<TKey, TValue>(Dictionary<TKey, TValue> before, Dictionary<TKey, TValue> after,
            out TValue[] set, out TKey[] removed)
            where TValue : IEquatable<TValue>
        {
            List<TValue> changed = null;
            foreach (KeyValuePair<TKey, TValue> pair in after)
            {
                if (before != null && before.TryGetValue(pair.Key, out TValue old) && old.Equals(pair.Value))
                    continue;

                (changed ??= new List<TValue>()).Add(pair.Value);
            }

            List<TKey> gone = null;
            if (before != null)
            {
                foreach (TKey key in before.Keys)
                {
                    if (!after.ContainsKey(key))
                        (gone ??= new List<TKey>()).Add(key);
                }
            }

            set = changed?.ToArray() ?? Array.Empty<TValue>();
            removed = gone?.ToArray() ?? Array.Empty<TKey>();
        }

        private static void ApplyInto<TKey, TValue>(Dictionary<TKey, TValue> target, TValue[] set, TKey[] removed,
            Func<TValue, TKey> keyOf)
        {
            if (removed != null)
            {
                foreach (TKey key in removed)
                    target.Remove(key);
            }

            if (set != null)
            {
                foreach (TValue value in set)
                    target[keyOf(value)] = value;
            }
        }
    }
}
