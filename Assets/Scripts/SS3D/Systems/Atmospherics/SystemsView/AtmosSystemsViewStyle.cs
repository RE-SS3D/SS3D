using System;
using System.Collections.Generic;
using System.Text;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using static SS3D.Systems.Atmospherics.SystemsView.AtmosSystemsViewCodes;

namespace SS3D.Systems.Atmospherics.SystemsView
{
    /// <summary>
    /// How the Atmos view draws and words its codes (<see cref="AtmosSystemsViewCodes"/>) on the client. Lines that share a
    /// tile sit side by side. Every badge is a dark tile with a frame in its network's colour; a device between two
    /// networks has its back half framed in one and its front half in the other. The mark inside says what it is and its
    /// state colour whether it runs. The legend (<c>SystemsViewLegend</c>) describes the same marks.
    /// </summary>
    public sealed class AtmosSystemsViewStyle : ISystemsViewStyle
    {
        public static readonly AtmosSystemsViewStyle Instance = new();

        public static readonly Color MarkOn = new(0.97f, 0.97f, 0.97f, 1f);
        public static readonly Color MarkOff = new(0.42f, 0.44f, 0.48f, 1f);
        public static readonly Color MarkNoPower = new(1f, 0.72f, 0.15f, 1f);
        private static readonly Color Backing = new(0.05f, 0.06f, 0.08f, 0.95f);

        private const float OutlineHalf = 0.34f;
        private const float FrameHalf = 0.3f;
        private const float InnerHalf = 0.23f;

        // Supply and Waste, the two lines a station runs everywhere, sit furthest apart; Mixed and Surface between.
        private static readonly float[] LineOffsets = { -0.22f, -0.075f, 0.22f, 0.075f };

        private static readonly byte[] DeviceSlots = BuildDeviceSlots();

        private static byte[] BuildDeviceSlots()
        {
            Array layers = Enum.GetValues(typeof(TileLayer));
            byte[] slots = new byte[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                slots[i] = (byte)(TileLayer)layers.GetValue(i);
            return slots;
        }

        public static Color MarkColor(byte state) => SwitchOf(state) switch
        {
            Off => MarkOff,
            NoPower => MarkNoPower,
            _ => MarkOn,
        };

        /// <summary>
        /// One offset along the tile's diagonal: a run along x sits that far north or south, a run along z that far east or
        /// west, so parallel lines never overlap and a corner stays on its line.
        /// </summary>
        public Vector2 SlotOffset(byte slot)
        {
            byte line = LineOf(slot, out _);
            float t = line < LineOffsets.Length ? LineOffsets[line] : 0f;
            return new Vector2(t, -t);
        }

        public void DrawBadge(SystemsViewMeshBuilder mesh, Vector3 centre, SystemsViewBadge badge, Func<int, Color> colorOf)
        {
            Vector3 forward = Step(badge.Facing);
            Vector3 right = new(forward.z, 0f, -forward.x);
            mesh.AddSquare(centre, OutlineHalf, Backing);
            if (IsTwoNetwork(badge.Kind))
            {
                // Back half in the network gas comes from, front half in the one it goes to.
                DrawHalf(mesh, centre, -forward, right, colorOf(badge.ViewId));
                DrawHalf(mesh, centre, forward, right, colorOf(badge.OtherViewId));
            }
            else
            {
                mesh.AddSquare(centre, FrameHalf, colorOf(badge.ViewId));
            }

            Vector3 side = IsSideOnLeft(badge.State) ? -right : right;
            if (badge.Kind is KindFilter or KindMixer)
            {
                // A notch on the side port's edge, in the side network's colour.
                Vector3 edge = centre + side * FrameHalf;
                mesh.AddQuad(edge - forward * 0.12f, edge - forward * 0.12f + side * (OutlineHalf - FrameHalf + 0.02f),
                    edge + forward * 0.12f + side * (OutlineHalf - FrameHalf + 0.02f), edge + forward * 0.12f,
                    colorOf(badge.SideViewId));
            }

            mesh.AddSquare(centre, InnerHalf, Backing);
            Color mark = MarkColor(badge.State);
            switch (badge.Kind)
            {
                case KindVent:
                    foreach (Vector3 d in Cardinals(forward, right))
                        Arrowhead(mesh, centre + d * 0.07f, d, 0.12f, 0.08f, mark);
                    break;
                case KindScrubber:
                    foreach (Vector3 d in Cardinals(forward, right))
                        Arrowhead(mesh, centre + d * 0.19f, -d, 0.12f, 0.08f, mark);
                    if (ModeOf(badge.State) == ModeSiphon)
                        mesh.AddSquare(centre, 0.045f, mark);
                    break;
                case KindIntake:
                    // Marks point in from the corners, so it never reads as a room scrubber.
                    foreach (Vector3 d in Diagonals(forward, right))
                        Arrowhead(mesh, centre + d * 0.22f, -d, 0.12f, 0.08f, mark);
                    if (ModeOf(badge.State) == ModeSiphon)
                        mesh.AddSquare(centre, 0.045f, mark);
                    break;
                case KindPump:
                    Arrow(mesh, centre, forward, right, mark);
                    break;
                case KindFilter:
                    // The chosen gases leave by the side: a mark pointing out of it.
                    Arrow(mesh, centre, forward, right, mark);
                    Arrowhead(mesh, centre + side * 0.13f, side, 0.08f, 0.06f, mark);
                    break;
                case KindMixer:
                    // The second inlet is the side: a mark pointing into it.
                    Arrow(mesh, centre, forward, right, mark);
                    Arrowhead(mesh, centre + side * 0.21f, -side, 0.08f, 0.06f, mark);
                    break;
                case KindValve:
                    // Open: a bar along the pipe, gas goes through. Closed: a bar across it.
                    Vector3 bar = SwitchOf(badge.State) == Off ? right : forward;
                    mesh.AddBand(centre - bar * 0.18f, centre + bar * 0.18f, 0.09f, mark);
                    mesh.AddSquare(centre, 0.07f, mark);
                    break;
                default:
                    mesh.AddSquare(centre, 0.08f, mark);
                    break;
            }
        }

        public string Describe(SystemsViewStructure structure, Vector2Int tile)
        {
            if (structure == null)
                return string.Empty;

            StringBuilder text = new();
            foreach (byte slot in DeviceSlots)
            {
                if (!structure.Badges.TryGetValue(new SystemsViewKey(tile, slot), out SystemsViewBadge badge))
                    continue;

                SystemsViewText.NewRow(text).Append(KindName(badge.Kind)).Append(": ").Append(StateWords(badge.Kind, badge.State)).Append(" · ");
                if (IsTwoNetwork(badge.Kind))
                    text.Append(LabelOf(structure, badge.ViewId)).Append(" → ").Append(LabelOf(structure, badge.OtherViewId));
                else
                    text.Append(LabelOf(structure, badge.ViewId));
                if (badge.Kind is KindFilter or KindMixer)
                    text.Append(badge.Kind == KindFilter ? ", side out to " : ", side in from ").Append(LabelOf(structure, badge.SideViewId));
            }

            for (byte slot = 0; slot < ManifoldSlotBase * 2; slot++)
            {
                if (!structure.Segments.TryGetValue(new SystemsViewKey(tile, slot), out SystemsViewSegment segment))
                    continue;

                string line = LineName(LineOf(slot, out bool manifold));
                string label = LabelOf(structure, segment.ViewId);
                SystemsViewText.NewRow(text);
                if (label.StartsWith(line, StringComparison.Ordinal))
                    text.Append(label);
                else
                    text.Append(line).Append(" line, part of ").Append(label);
                if (manifold)
                    text.Append(" (manifold)");
            }

            return text.ToString();
        }

        /// <summary>
        /// A device traces every network it touches, each named by its side; a pipe its run's network; a manifold each
        /// line it spans, named by line.
        /// </summary>
        public void TraceTargets(SystemsViewStructure structure, Vector2Int tile, TileLayer layer, List<SystemsViewTraceSide> into)
        {
            if (structure == null || into == null)
                return;

            if (structure.Badges.TryGetValue(new SystemsViewKey(tile, (byte)layer), out SystemsViewBadge badge))
            {
                if (!IsTwoNetwork(badge.Kind))
                {
                    SystemsViewText.AddTarget(into, badge.ViewId, null);
                    return;
                }

                // Back, front, then a filter's or mixer's side; a pump or valve has no side network (0, skipped).
                for (byte port = PortBack; port < SystemsViewBadge.PortCount; port++)
                    SystemsViewText.AddTarget(into, badge.ViewIdAt(port), PortName(badge.Kind, port));
                return;
            }

            if (layer == TileLayer.PipeManifold)
            {
                for (byte line = Supply; line <= Surface; line++)
                {
                    if (structure.Segments.TryGetValue(new SystemsViewKey(tile, (byte)(ManifoldSlotBase + line)), out SystemsViewSegment run))
                        SystemsViewText.AddTarget(into, run.ViewId, LineName(line) + " line");
                }

                return;
            }

            if (TryGetLineSlot(layer, out byte slot) && structure.Segments.TryGetValue(new SystemsViewKey(tile, slot), out SystemsViewSegment segment))
                SystemsViewText.AddTarget(into, segment.ViewId, null);
        }

        private static string LabelOf(SystemsViewStructure structure, int viewId) =>
            viewId <= 0 ? "no pipe" : structure.Networks.TryGetValue(viewId, out SystemsViewNetwork network) ? network.Label : $"network {viewId}";

        private static Vector3 Step(Direction direction)
        {
            Tuple<int, int> step = TileHelper.ToCardinalVector(direction);
            Vector3 v = new(step.Item1, 0f, step.Item2);
            return v.sqrMagnitude > 0f ? v.normalized : Vector3.forward;
        }

        private static Vector3[] Cardinals(Vector3 forward, Vector3 right) => new[] { forward, right, -forward, -right };

        private static Vector3[] Diagonals(Vector3 forward, Vector3 right)
        {
            Vector3 a = (forward + right).normalized;
            Vector3 b = (forward - right).normalized;
            return new[] { a, b, -a, -b };
        }

        private static void DrawHalf(SystemsViewMeshBuilder mesh, Vector3 centre, Vector3 toward, Vector3 right, Color color)
        {
            Vector3 edge = centre + toward * FrameHalf;
            mesh.AddQuad(centre - right * FrameHalf, edge - right * FrameHalf, edge + right * FrameHalf, centre + right * FrameHalf, color);
        }

        /// <summary>A triangle whose base is centred on <paramref name="baseCentre"/> and whose tip points along <paramref name="direction"/>.</summary>
        private static void Arrowhead(SystemsViewMeshBuilder mesh, Vector3 baseCentre, Vector3 direction, float length, float halfWidth, Color color)
        {
            Vector3 across = new Vector3(direction.z, 0f, -direction.x) * halfWidth;
            mesh.AddTriangle(baseCentre - across, baseCentre + direction * length, baseCentre + across, color);
        }

        /// <summary>The flow arrow of a pump, filter or mixer: from its back to its front.</summary>
        private static void Arrow(SystemsViewMeshBuilder mesh, Vector3 centre, Vector3 forward, Vector3 right, Color color)
        {
            mesh.AddBand(centre - forward * 0.17f, centre + forward * 0.02f, 0.07f, color);
            Arrowhead(mesh, centre, forward, 0.18f, 0.12f, color);
        }
    }
}
