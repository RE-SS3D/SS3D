using SS3D.Systems.Tile.FloorVisuals;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor.Commands
{
    /// <summary>
    /// Paints (or, with colour 0, erases) some of one tile's floor squares (<see cref="FloorPaint"/>). A paint stroke
    /// sends one per tile it touches, all in the gesture's compound, so one undo takes the whole stroke.
    /// </summary>
    public sealed class SetFloorPaintCommand : IMapEditorCommand, IReportsChange
    {
        private readonly Vector3 _position;
        private readonly int _mask;
        private readonly int _color;
        private ushort _previous;

        /// <param name="mask">A bit per square: 1 south-west, 2 south-east, 4 north-west, 8 north-east.</param>
        /// <param name="color">A <see cref="FloorPaint.Palette"/> index; 0 erases.</param>
        public SetFloorPaintCommand(Vector3 position, int mask, int color)
        {
            _position = position;
            _mask = mask & 0xF;
            _color = color;
        }

        public bool Changed { get; private set; }

        public MapEditorCommandDto ToDto() =>
            new()
            {
                Kind = MapEditorCommandKind.SetFloorPaint,
                Position = _position,
                PaintMask = (byte)_mask,
                PaintColor = (byte)_color,
            };

        public void Apply(MapEditorCommandContext ctx)
        {
            Changed = false;
            if (!ctx.Map.TryGetFloorPaint(_position, out _previous))
                return;

            ushort next = FloorPaint.Apply(_previous, _mask, _color);
            if (next == _previous || !ctx.Map.TrySetFloorPaint(_position, next))
                return;

            Changed = true;
            ctx.NotifyFloorDecalsChanged();
        }

        public void Revert(MapEditorCommandContext ctx)
        {
            if (!Changed)
                return;

            ctx.Map.TrySetFloorPaint(_position, _previous);
            ctx.NotifyFloorDecalsChanged();
        }
    }
}
