using System;
using System.Collections.Generic;
using System.Text;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using static SS3D.Systems.Electricity.SystemsView.PowerSystemsViewCodes;

namespace SS3D.Systems.Electricity.SystemsView
{
    /// <summary>
    /// How the Power view draws and words its codes (<see cref="PowerSystemsViewCodes"/>) on the client. Cables run down
    /// the middle of their tile. A grid device is a dark tile framed in its circuit's colour with a mark for what it is:
    /// a bolt for a generator, cells for a solar panel, a battery for an SMES, and three breaker pips for an APC (lit when
    /// that channel is on, a red cross when it is broken). A consumer is a small status dot: green powered, amber its
    /// channel switched off at the APC, red unpowered. Rooms are tinted by the client's room overlay
    /// (<c>MapEditorSystemsViewClient</c>), not here. The legend (<see cref="PowerSystemsViewDomain"/>) describes the same marks.
    /// </summary>
    public sealed class PowerSystemsViewStyle : ISystemsViewStyle
    {
        public static readonly PowerSystemsViewStyle Instance = new();

        public static readonly Color MarkOn = new(0.97f, 0.97f, 0.97f, 1f);
        public static readonly Color MarkOff = new(0.42f, 0.44f, 0.48f, 1f);
        public static readonly Color DotPowered = new(0.35f, 0.9f, 0.4f, 1f);
        public static readonly Color DotChannelOff = new(1f, 0.72f, 0.15f, 1f);
        public static readonly Color DotUnpowered = new(0.95f, 0.25f, 0.25f, 1f);
        /// <summary>The stripes of a room nothing powers: pale grey, so they never read as a circuit's colour.</summary>
        public static readonly Color Hatch = new(0.85f, 0.87f, 0.9f, 0.6f);
        private static readonly Color Backing = new(0.05f, 0.06f, 0.08f, 0.95f);

        private const float OutlineHalf = 0.34f;
        private const float FrameHalf = 0.3f;
        private const float InnerHalf = 0.23f;
        private const float DotHalf = 0.085f;
        // A wall mount's dot sits toward the room it faces, off the wall's middle.
        private const float WallMountDotOffset = 0.3f;

        /// <summary>
        /// The room a floor tile is in on this client (its area id; 0 for none). Reads the synced room membership
        /// (<c>AreaSubSystem.FloorVisualCache</c>) unless a test sets its own.
        /// </summary>
        public Func<Vector2Int, ushort> AreaAt { get; set; } = DefaultAreaAt;

        private static ushort DefaultAreaAt(Vector2Int tile) =>
            SubSystems.TryGet(out AreaSubSystem areas) && areas.FloorVisualCache.TryGetAreaIdForWorldGrid(tile, out ushort id) ? id : (ushort)0;

        public static Color DotColor(byte state) => StatusOf(state) switch
        {
            Powered => DotPowered,
            ChannelOff => DotChannelOff,
            _ => DotUnpowered,
        };

        public Vector2 SlotOffset(byte slot) => Vector2.zero;

        public void DrawBadge(SystemsViewMeshBuilder mesh, Vector3 centre, SystemsViewBadge badge, Func<int, Color> colorOf)
        {
            if (IsConsumer(badge.Kind))
            {
                Vector3 at = centre;
                TileLayer layer = LayerOfSlot(badge.Key.Slot);
                if (layer is TileLayer.WallMountHigh or TileLayer.WallMountLow)
                    at += Step(badge.Facing) * WallMountDotOffset;
                mesh.AddSquare(at, DotHalf + 0.03f, Backing);
                mesh.AddSquare(at, DotHalf, DotColor(badge.State));
                return;
            }

            mesh.AddSquare(centre, OutlineHalf, Backing);
            mesh.AddSquare(centre, FrameHalf, colorOf(badge.ViewId));
            mesh.AddSquare(centre, InnerHalf, Backing);
            Color mark = (badge.State & Running) != 0 ? MarkOn : MarkOff;
            switch (badge.Kind)
            {
                case KindGenerator:
                    // A bolt: two triangles stepping down.
                    mesh.AddTriangle(centre + new Vector3(0.05f, 0f, 0.17f), centre + new Vector3(-0.1f, 0f, -0.02f), centre + new Vector3(0.04f, 0f, -0.02f), mark);
                    mesh.AddTriangle(centre + new Vector3(-0.04f, 0f, 0.02f), centre + new Vector3(0.1f, 0f, 0.02f), centre + new Vector3(-0.05f, 0f, -0.17f), mark);
                    break;
                case KindSolar:
                    for (int x = -1; x <= 1; x += 2)
                    for (int z = -1; z <= 1; z += 2)
                        mesh.AddSquare(centre + new Vector3(x * 0.075f, 0f, z * 0.075f), 0.06f, mark);
                    break;
                case KindSmes:
                    // A battery standing up: its cap, its body.
                    mesh.AddRect(centre + new Vector3(0f, 0f, 0.15f), 0.04f, 0.025f, mark);
                    mesh.AddRect(centre + new Vector3(0f, 0f, -0.02f), 0.09f, 0.15f, mark);
                    mesh.AddRect(centre + new Vector3(0f, 0f, -0.02f), 0.05f, 0.11f, Backing);
                    if ((badge.State & Running) != 0)
                        mesh.AddRect(centre + new Vector3(0f, 0f, -0.06f), 0.05f, 0.07f, mark);
                    break;
                case KindApc:
                    if ((badge.State & ApcBroken) != 0)
                    {
                        mesh.AddBand(centre + new Vector3(-0.15f, 0f, -0.15f), centre + new Vector3(0.15f, 0f, 0.15f), 0.07f, DotUnpowered);
                        mesh.AddBand(centre + new Vector3(-0.15f, 0f, 0.15f), centre + new Vector3(0.15f, 0f, -0.15f), 0.07f, DotUnpowered);
                        break;
                    }

                    // Lighting, equipment, environment, west to east: lit when that breaker is on.
                    ApcControlFlags channels = ApcChannelsOf(badge.State);
                    ApcControlFlags[] pips = { ApcControlFlags.Lighting, ApcControlFlags.Equipment, ApcControlFlags.Environment };
                    for (int i = 0; i < pips.Length; i++)
                        mesh.AddSquare(centre + new Vector3((i - 1) * 0.13f, 0f, 0f), 0.05f, (channels & pips[i]) != 0 ? MarkOn : MarkOff);
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
            ForEachBadge(structure, tile, null, badge =>
            {
                SystemsViewText.NewRow(text).Append(KindName(badge.Kind)).Append(": ").Append(StateWords(badge.Kind, badge.State));
                if (badge.OtherViewId > 0)
                    text.Append(" · ").Append(LabelOf(structure, badge.OtherViewId));
                text.Append(" · ").Append(badge.ViewId > 0 ? LabelOf(structure, badge.ViewId)
                    : IsConsumer(badge.Kind) ? "no APC" : "on no cable");
            });

            if (structure.Segments.TryGetValue(new SystemsViewKey(tile, CableSlot), out SystemsViewSegment cable))
                SystemsViewText.NewRow(text).Append("Cable, part of ").Append(LabelOf(structure, cable.ViewId));

            if (TryGetRoomAt(structure, tile, out SystemsViewRoom room))
                SystemsViewText.NewRow(text).Append("Room: ").Append(LabelOf(structure, room.ViewId)).Append(" · ").Append(RoomPowerWords(structure, room));

            return text.ToString();
        }

        /// <summary>A room's APC in plain words: "APC on Circuit 3", "no APC", "APC on no cable", "APC broken".</summary>
        public static string RoomPowerWords(SystemsViewStructure structure, SystemsViewRoom room)
        {
            if ((room.Flags & RoomHasApc) == 0)
                return "no APC";
            if ((room.Flags & RoomApcBroken) != 0)
                return "APC broken";
            return room.CircuitViewId > 0 ? "APC on " + LabelOf(structure, room.CircuitViewId) : "APC on no cable";
        }

        /// <summary>
        /// A cable traces its circuit; a grid device its circuit (an APC its room too); a consumer its room. A floor tile
        /// traces the cable under it if there is one (Select usually picks the dimmed floor over a cable), else its room.
        /// </summary>
        public void TraceTargets(SystemsViewStructure structure, Vector2Int tile, TileLayer layer, List<SystemsViewTraceSide> into)
        {
            if (structure == null || into == null)
                return;

            bool hasCable = structure.Segments.TryGetValue(new SystemsViewKey(tile, CableSlot), out SystemsViewSegment cable);
            switch (layer)
            {
                case TileLayer.Wire:
                    if (hasCable)
                        SystemsViewText.AddTarget(into, cable.ViewId, null);
                    return;
                case TileLayer.Turf:
                case TileLayer.Plenum:
                    if (hasCable)
                        SystemsViewText.AddTarget(into, cable.ViewId, null);
                    else if (TryGetRoomAt(structure, tile, out SystemsViewRoom room))
                        SystemsViewText.AddTarget(into, room.ViewId, null);
                    return;
            }

            ForEachBadge(structure, tile, layer, badge =>
            {
                if (badge.Kind == KindApc)
                {
                    SystemsViewText.AddTarget(into, badge.ViewId, "Circuit");
                    SystemsViewText.AddTarget(into, badge.OtherViewId, "Room");
                }
                else if (IsConsumer(badge.Kind))
                {
                    SystemsViewText.AddTarget(into, badge.OtherViewId, null);
                }
                else
                {
                    SystemsViewText.AddTarget(into, badge.ViewId, null);
                }
            });
        }

        private bool TryGetRoomAt(SystemsViewStructure structure, Vector2Int tile, out SystemsViewRoom room)
        {
            room = default;
            ushort areaId = structure.Rooms.Count > 0 ? AreaAt?.Invoke(tile) ?? 0 : (ushort)0;
            return areaId != 0 && structure.Rooms.TryGetValue(areaId, out room);
        }

        /// <summary>Every badge on <paramref name="tile"/> (on <paramref name="layer"/> only, when given), in slot order.</summary>
        private static void ForEachBadge(SystemsViewStructure structure, Vector2Int tile, TileLayer? layer, Action<SystemsViewBadge> visit)
        {
            if (structure.Badges.Count == 0)
                return;

            int from = layer.HasValue ? (int)layer.Value << 3 : 0;
            int to = layer.HasValue ? from + 8 : 256;
            for (int slot = from; slot < to && slot < 256; slot++)
            {
                if (structure.Badges.TryGetValue(new SystemsViewKey(tile, (byte)slot), out SystemsViewBadge badge))
                    visit(badge);
            }
        }

        private static string LabelOf(SystemsViewStructure structure, int viewId) =>
            structure.Networks.TryGetValue(viewId, out SystemsViewNetwork network) && !string.IsNullOrEmpty(network.Label)
                ? network.Label : IsRoomViewId(viewId) ? "a room" : $"Circuit {viewId}";

        private static Vector3 Step(Direction direction)
        {
            Vector2Int step = SystemsViewLinks.Step(direction);
            return new Vector3(step.x, 0f, step.y).normalized;
        }
    }
}
