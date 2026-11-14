using System;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// One line of a traced network's numbers, worded and formatted by its domain on the server ("Pressure",
    /// "101.3 kPa"; "Supply", "12.0 kW"), so the panel stays domain-free and a change below the shown precision is no
    /// change at all.
    /// </summary>
    public struct SystemsViewLiveRow : IEquatable<SystemsViewLiveRow>
    {
        public string Label;
        public string Value;

        public SystemsViewLiveRow(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public bool Equals(SystemsViewLiveRow other) =>
            string.Equals(Label, other.Label, StringComparison.Ordinal) && string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SystemsViewLiveRow other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Label, Value);
    }

    /// <summary>
    /// A device attached to a traced network: where it is (for Locate), what it is and what it is doing there
    /// ("Pump", "on, to a pressure · inlet"; "SMES", "64 % · in 20 kW, out 5 kW").
    /// </summary>
    public struct SystemsViewLiveDevice : IEquatable<SystemsViewLiveDevice>
    {
        public SystemsViewKey Key;
        public string Label;
        public string Detail;

        public bool Equals(SystemsViewLiveDevice other) => Key.Equals(other.Key)
            && string.Equals(Label, other.Label, StringComparison.Ordinal) && string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SystemsViewLiveDevice other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Key, Label, Detail);
    }

    /// <summary>One traced network's live numbers (<see cref="ISystemsViewSource.ReadLive"/>). Arrays are never null.</summary>
    public struct SystemsViewLiveNetwork : IEquatable<SystemsViewLiveNetwork>
    {
        public int ViewId;
        public SystemsViewLiveRow[] Rows;
        public SystemsViewLiveDevice[] Devices;

        /// <summary>The row with this label's value, or null; for tests and automation.</summary>
        public string ValueOf(string label)
        {
            if (Rows == null)
                return null;

            foreach (SystemsViewLiveRow row in Rows)
            {
                if (string.Equals(row.Label, label, StringComparison.Ordinal))
                    return row.Value;
            }

            return null;
        }

        public bool Equals(SystemsViewLiveNetwork other) =>
            ViewId == other.ViewId && SystemsViewLive.SameItems(Rows, other.Rows) && SystemsViewLive.SameItems(Devices, other.Devices);
        public override bool Equals(object obj) => obj is SystemsViewLiveNetwork other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ViewId, Rows?.Length ?? 0, Devices?.Length ?? 0);
    }

    /// <summary>
    /// The live stream of one client's trace (Documents/architecture/2026-11_map-editor-systems-views.md Q2): the numbers
    /// of every traced network that still exists, in the order the client asked for them. Sent only to the tracing
    /// client and only when it differs from what that client was last sent. A traced network that is gone is left out;
    /// an empty <see cref="Networks"/> means the whole trace is gone and the server has dropped it.
    /// </summary>
    public struct SystemsViewLive : IEquatable<SystemsViewLive>
    {
        public SystemsViewKind View;
        /// <summary>The client's own number for the trace this answers, so a reply to an older trace is ignored.</summary>
        public int TraceId;
        /// <summary>
        /// The simulation behind the view is paused (<see cref="ISystemsViewSource.SimulationPaused"/>), so the numbers
        /// hold still on purpose and the panel says so rather than looking frozen.
        /// </summary>
        public bool Paused;
        public SystemsViewLiveNetwork[] Networks;

        /// <summary>The numbers of network <paramref name="viewId"/> in this message; false when it isn't in it.</summary>
        public bool TryFind(int viewId, out SystemsViewLiveNetwork network)
        {
            if (Networks != null)
            {
                foreach (SystemsViewLiveNetwork candidate in Networks)
                {
                    if (candidate.ViewId == viewId)
                    {
                        network = candidate;
                        return true;
                    }
                }
            }

            network = default;
            return false;
        }

        public bool Equals(SystemsViewLive other) =>
            View == other.View && TraceId == other.TraceId && Paused == other.Paused && SameItems(Networks, other.Networks);
        public override bool Equals(object obj) => obj is SystemsViewLive other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(View, TraceId, Paused, Networks?.Length ?? 0);

        /// <summary>Element-wise equality; null and empty are the same.</summary>
        public static bool SameItems<T>(T[] a, T[] b) where T : IEquatable<T>
        {
            int count = a?.Length ?? 0;
            if (count != (b?.Length ?? 0))
                return false;

            for (int i = 0; i < count; i++)
            {
                if (!a[i].Equals(b[i]))
                    return false;
            }

            return true;
        }
    }

    /// <summary>One network a Select traces, and which side of the clicked device it is on ("Inlet"); Side is null for a pipe.</summary>
    public struct SystemsViewTraceSide : IEquatable<SystemsViewTraceSide>
    {
        public int ViewId;
        public string Side;

        public SystemsViewTraceSide(int viewId, string side = null)
        {
            ViewId = viewId;
            Side = side;
        }

        public bool Equals(SystemsViewTraceSide other) => ViewId == other.ViewId && string.Equals(Side, other.Side, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SystemsViewTraceSide other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ViewId, Side);
    }
}
