using System;
using FishNet.Serializing;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// Custom FishNet wire format for <see cref="SystemsViewDelta"/> (Write/Read + type name, FishNet's custom-serializer
    /// convention, as <c>AtmosChunkPatchSerializer</c>). Tiles, ids and counts are packed integers, so a station's
    /// structure costs a few bytes per pipe tile. <see cref="SizeOf"/> and <see cref="Limit"/> measure with the same
    /// writer, so the send budget (<see cref="SystemsViewPublisher.MaxBytesPerSend"/>) is counted in real wire bytes.
    /// </summary>
    public static class SystemsViewWire
    {
        // View and Reset, plus eight packed counts of up to three bytes each (a count under 2,097,152).
        private const int HeaderReserve = 2 + 8 * 3;

        public static void WriteSystemsViewDelta(this Writer writer, SystemsViewDelta delta)
        {
            writer.WriteByte((byte)delta.View);
            writer.WriteBoolean(delta.Reset);
            WriteArray(writer, delta.SegmentsSet, WriteSegment);
            WriteArray(writer, delta.SegmentsRemoved, WriteKey);
            WriteArray(writer, delta.NetworksSet, WriteNetwork);
            WriteArray(writer, delta.NetworksRemoved, (w, id) => w.WriteInt32(id));
            WriteArray(writer, delta.BadgesSet, WriteBadge);
            WriteArray(writer, delta.BadgesRemoved, WriteKey);
            WriteArray(writer, delta.MarkersSet, WriteMarker);
            WriteArray(writer, delta.MarkersRemoved, WriteKey);
        }

        public static SystemsViewDelta ReadSystemsViewDelta(this Reader reader) => new()
        {
            View = (SystemsViewKind)reader.ReadByte(),
            Reset = reader.ReadBoolean(),
            SegmentsSet = ReadArray(reader, ReadSegment),
            SegmentsRemoved = ReadArray(reader, ReadKey),
            NetworksSet = ReadArray(reader, ReadNetwork),
            NetworksRemoved = ReadArray(reader, r => r.ReadInt32()),
            BadgesSet = ReadArray(reader, ReadBadge),
            BadgesRemoved = ReadArray(reader, ReadKey),
            MarkersSet = ReadArray(reader, ReadMarker),
            MarkersRemoved = ReadArray(reader, ReadKey),
        };

        /// <summary>Bytes <paramref name="delta"/> takes on the wire, before FishNet's RPC header.</summary>
        public static int SizeOf(SystemsViewDelta delta)
        {
            PooledWriter writer = WriterPool.Retrieve();
            try
            {
                writer.WriteSystemsViewDelta(delta);
                return writer.Length;
            }
            finally
            {
                writer.Store();
            }
        }

        /// <summary>
        /// Cuts <paramref name="delta"/> down to what fits in <paramref name="maxBytes"/>: removals first, then labels,
        /// segments, badges and markers, in their array order. Returns whether anything was left out; the caller sends
        /// the rest on a later poll by diffing again against what it did send. Always keeps at least one entry, so a
        /// cut delta still makes progress.
        /// </summary>
        public static bool Limit(ref SystemsViewDelta delta, int maxBytes)
        {
            if (SizeOf(delta) <= maxBytes)
                return false;

            PooledWriter scratch = WriterPool.Retrieve();
            try
            {
                Budget budget = new(scratch, maxBytes - HeaderReserve);
                delta.SegmentsRemoved = budget.Take(delta.SegmentsRemoved, WriteKey);
                delta.NetworksRemoved = budget.Take(delta.NetworksRemoved, (w, id) => w.WriteInt32(id));
                delta.BadgesRemoved = budget.Take(delta.BadgesRemoved, WriteKey);
                delta.MarkersRemoved = budget.Take(delta.MarkersRemoved, WriteKey);
                delta.NetworksSet = budget.Take(delta.NetworksSet, WriteNetwork);
                delta.SegmentsSet = budget.Take(delta.SegmentsSet, WriteSegment);
                delta.BadgesSet = budget.Take(delta.BadgesSet, WriteBadge);
                delta.MarkersSet = budget.Take(delta.MarkersSet, WriteMarker);
                return true;
            }
            finally
            {
                scratch.Store();
            }
        }

        private sealed class Budget
        {
            private readonly PooledWriter _scratch;
            private int _left;
            private bool _tookAny;

            public Budget(PooledWriter scratch, int bytes)
            {
                _scratch = scratch;
                _left = bytes;
            }

            public T[] Take<T>(T[] items, Action<Writer, T> write)
            {
                if (items == null || items.Length == 0)
                    return Array.Empty<T>();

                int count = 0;
                for (; count < items.Length; count++)
                {
                    _scratch.Reset();
                    write(_scratch, items[count]);
                    if (_scratch.Length > _left && _tookAny)
                        break;

                    _left -= _scratch.Length;
                    _tookAny = true;
                }

                if (count == items.Length)
                    return items;

                T[] taken = new T[count];
                Array.Copy(items, taken, count);
                _left = 0;
                return taken;
            }
        }

        private static void WriteArray<T>(Writer writer, T[] items, Action<Writer, T> write)
        {
            int count = items?.Length ?? 0;
            writer.WriteInt32(count);
            for (int i = 0; i < count; i++)
                write(writer, items[i]);
        }

        private static T[] ReadArray<T>(Reader reader, Func<Reader, T> read)
        {
            int count = reader.ReadInt32();
            if (count <= 0)
                return Array.Empty<T>();

            T[] items = new T[count];
            for (int i = 0; i < count; i++)
                items[i] = read(reader);
            return items;
        }

        private static void WriteKey(Writer writer, SystemsViewKey key)
        {
            writer.WriteInt32(key.Tile.x);
            writer.WriteInt32(key.Tile.y);
            writer.WriteByte(key.Slot);
        }

        private static SystemsViewKey ReadKey(Reader reader)
        {
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            return new SystemsViewKey(new Vector2Int(x, y), reader.ReadByte());
        }

        private static void WriteSegment(Writer writer, SystemsViewSegment segment)
        {
            WriteKey(writer, segment.Key);
            writer.WriteInt32(segment.ViewId);
            writer.WriteByte(segment.Links);
        }

        private static SystemsViewSegment ReadSegment(Reader reader) => new()
        {
            Key = ReadKey(reader),
            ViewId = reader.ReadInt32(),
            Links = reader.ReadByte(),
        };

        private static void WriteNetwork(Writer writer, SystemsViewNetwork network)
        {
            writer.WriteInt32(network.ViewId);
            writer.WriteString(network.Label);
        }

        private static SystemsViewNetwork ReadNetwork(Reader reader) => new()
        {
            ViewId = reader.ReadInt32(),
            Label = reader.ReadString(),
        };

        private static void WriteBadge(Writer writer, SystemsViewBadge badge)
        {
            WriteKey(writer, badge.Key);
            writer.WriteByte(badge.Kind);
            writer.WriteByte(badge.State);
            writer.WriteByte((byte)badge.Facing);
            writer.WriteInt32(badge.ViewId);
            writer.WriteInt32(badge.OtherViewId);
            writer.WriteInt32(badge.SideViewId);
        }

        private static SystemsViewBadge ReadBadge(Reader reader) => new()
        {
            Key = ReadKey(reader),
            Kind = reader.ReadByte(),
            State = reader.ReadByte(),
            Facing = (Direction)reader.ReadByte(),
            ViewId = reader.ReadInt32(),
            OtherViewId = reader.ReadInt32(),
            SideViewId = reader.ReadInt32(),
        };

        private static void WriteMarker(Writer writer, SystemsViewMarker marker)
        {
            WriteKey(writer, marker.Key);
            writer.WriteString(marker.IssueId);
        }

        private static SystemsViewMarker ReadMarker(Reader reader) => new()
        {
            Key = ReadKey(reader),
            IssueId = reader.ReadString(),
        };
    }
}
