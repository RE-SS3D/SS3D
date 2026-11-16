using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SS3D.Systems.Tile.MapEditor.SystemsView
{
    /// <summary>
    /// Wording and ordering every domain's source and style share: numbers at the panel's precision, devices west to
    /// east, hover rows, and trace targets listed once each.
    /// </summary>
    public static class SystemsViewText
    {
        /// <summary>A value at a fixed precision, culture-free, so a change below it reads the same (no live send).</summary>
        public static string Number(float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

        /// <summary>West to east, then south to north, then by slot, so a device list keeps its order between reads.</summary>
        public static void SortWestToEast(List<SystemsViewLiveDevice> devices) =>
            devices.Sort((a, b) => a.Key.Tile.x != b.Key.Tile.x ? a.Key.Tile.x.CompareTo(b.Key.Tile.x)
                : a.Key.Tile.y != b.Key.Tile.y ? a.Key.Tile.y.CompareTo(b.Key.Tile.y) : a.Key.Slot.CompareTo(b.Key.Slot));

        /// <summary>Starts a new hover row (a line break unless it is the first).</summary>
        public static StringBuilder NewRow(StringBuilder text) => text.Length > 0 ? text.Append('\n') : text;

        /// <summary>
        /// Adds a network a Select traces: none for id 0 (a port on nothing), one entry per network (a loop through one
        /// device is listed by its first side), and never more than <see cref="SystemsViewPublisher.MaxTracedNetworks"/>.
        /// </summary>
        public static void AddTarget(List<SystemsViewTraceSide> into, int viewId, string side)
        {
            if (viewId <= 0 || into.Count >= SystemsViewPublisher.MaxTracedNetworks)
                return;

            foreach (SystemsViewTraceSide existing in into)
            {
                if (existing.ViewId == viewId)
                    return;
            }

            into.Add(new SystemsViewTraceSide(viewId, side));
        }
    }
}
