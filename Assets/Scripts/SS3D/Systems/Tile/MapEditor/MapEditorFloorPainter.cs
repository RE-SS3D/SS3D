using SS3D.Core;
using SS3D.Systems.Inputs;
using SS3D.Systems.Tile.FloorVisuals;
using SS3D.Systems.Tile.MapEditor.Commands;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>
    /// Upper › Floor Paint: paints the squares of floor tiles (<see cref="FloorPaint"/>). Click a square, drag for a line,
    /// Shift-drag for a rectangle; the Delete tool, or Ctrl held when the stroke starts, erases; Dropper (or Select) takes
    /// a square's colour. The floor shader draws the preview (<see cref="FloorPaintView"/>), so what shows is what a click
    /// does, and a stroke is one undo step. Only floors and airlocks take paint; squares elsewhere are left out of the stroke.
    /// </summary>
    public sealed class MapEditorFloorPainter
    {
        private const int MaxSquares = MapEditorPlacementRules.MaxDragCells * 4;
        private const string TooBigMessage = "That's more than 4096 tiles — drag a smaller area";

        private readonly MapEditorSubSystem _editor;
        private readonly List<Vector2Int> _stroke = new();
        private readonly List<Vector2Int> _shown = new();
        private readonly Dictionary<Vector2Int, bool> _paintable = new();
        private Vector2Int? _start;
        private Vector2Int? _lastEnd;
        private bool _rectangle;
        private Vector2Int? _hover;
        private bool _hoverErasing;
        private byte _hoverColor;

        public MapEditorFloorPainter(MapEditorSubSystem editor) => _editor = editor;

        /// <summary>The stroke under way, or the next one, erases.</summary>
        public bool IsErasing { get; private set; }

        public void Update(byte color)
        {
            FloorPaintView.ExpirePending();
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            if (mouse == null)
                return;

            MapEditorTool tool = _editor.CurrentTool;
            bool typing = InputInterface.IsCapturingText;
            bool ctrl = keyboard != null && !typing
                        && (keyboard.ctrlKey.isPressed || keyboard.leftCommandKey.isPressed || keyboard.rightCommandKey.isPressed);
            bool shift = keyboard != null && !typing && keyboard.shiftKey.isPressed;
            bool cancel = mouse.rightButton.wasPressedThisFrame
                          || (keyboard != null && !typing && keyboard.escapeKey.wasPressedThisFrame);
            Vector2Int texel = FloorPaint.TexelAt(TileHelper.GetPointedPosition(false, _editor.PickCamera));
            bool overUi = _editor.MouseOverUI;

            if (_start.HasValue && cancel)
            {
                Cancel();
                return;
            }

            if (tool is MapEditorTool.Dropper or MapEditorTool.Select)
            {
                Cancel();
                if (mouse.leftButton.wasPressedThisFrame && !overUi)
                    _editor.PickFloorPaintColor(FloorPaintView.IndexAt(texel));
                return;
            }

            if (tool is not (MapEditorTool.Edit or MapEditorTool.Delete))
            {
                Cancel();
                return;
            }

            // A stroke keeps the mode it started in, as building does.
            if (!_start.HasValue)
                IsErasing = tool == MapEditorTool.Delete || ctrl;

            byte shown = IsErasing ? FloorPaint.ErasePreview : color;
            if (!IsErasing && !FloorPaint.IsPaint(color))
                return;

            if (mouse.leftButton.wasPressedThisFrame && !overUi)
            {
                _start = texel;
                _lastEnd = null;
            }

            if (_start.HasValue)
            {
                if (_lastEnd != texel || _rectangle != shift)
                {
                    _lastEnd = texel;
                    _rectangle = shift;
                    BuildStroke(_start.Value, texel, _rectangle);
                    FloorPaintView.SetPreview(_shown, shown);
                }

                if (!mouse.leftButton.isPressed)
                    Commit(overUi, color);
                return;
            }

            if (overUi)
            {
                ClearHover();
                return;
            }

            if (_hover != texel || _hoverErasing != IsErasing || _hoverColor != shown)
            {
                _hover = texel;
                _hoverErasing = IsErasing;
                _hoverColor = shown;
                BuildStroke(texel, texel, false);
                FloorPaintView.SetPreview(_shown, shown);
            }
        }

        /// <summary>Drop the stroke and the preview (tool or tab changed, editor closed, right click, Esc).</summary>
        public void Cancel()
        {
            _start = null;
            _lastEnd = null;
            ClearHover();
        }

        private void ClearHover()
        {
            _hover = null;
            FloorPaintView.ClearPreview();
        }

        private void BuildStroke(Vector2Int start, Vector2Int end, bool rectangle)
        {
            FloorPaint.Stroke(start, end, rectangle, MaxSquares, _stroke);
            _shown.Clear();
            _paintable.Clear();
            TileMap map = SubSystems.TryGet(out TileSubSystem tiles) ? tiles.CurrentMap : null;
            foreach (Vector2Int texel in _stroke)
            {
                Vector2Int tile = FloorPaint.TileOf(texel);
                if (!_paintable.TryGetValue(tile, out bool paintable))
                    _paintable[tile] = paintable = map.CanPaint(new Vector3(tile.x, 0f, tile.y));

                if (paintable)
                    _shown.Add(texel);
            }
        }

        private void Commit(bool overUi, byte color)
        {
            bool tooBig = _stroke.Count == 0;
            _start = null;
            _lastEnd = null;
            if (overUi || tooBig)
            {
                if (tooBig && !overUi)
                    _editor.ShowLocalToast(TooBigMessage);
                ClearHover();
                return;
            }

            int target = IsErasing ? 0 : color;
            var changed = new List<Vector2Int>(_shown.Count);
            foreach (Vector2Int texel in _shown)
            {
                if (FloorPaintView.IndexAt(texel) != target)
                    changed.Add(texel);
            }

            if (changed.Count == 0)
            {
                ClearHover();
                return;
            }

            var commands = new List<MapEditorCommandDto>();
            foreach (KeyValuePair<Vector2Int, int> tile in FloorPaint.MasksByTile(changed))
            {
                commands.Add(new MapEditorCommandDto
                {
                    Kind = MapEditorCommandKind.SetFloorPaint,
                    Position = new Vector3(tile.Key.x, 0f, tile.Key.y),
                    PaintMask = (byte)tile.Value,
                    PaintColor = (byte)target,
                });
            }

            _editor.SubmitCommands(commands.ToArray());
            // Keep the stroke drawn until the server's paint arrives, so it doesn't flicker back.
            FloorPaintView.HoldPreview();
            _hover = null;
        }
    }
}
