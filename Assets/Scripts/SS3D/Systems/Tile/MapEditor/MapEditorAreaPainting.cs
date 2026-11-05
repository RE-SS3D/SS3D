using System;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Tile.MapEditor.Commands;
using SS3D.Systems.Tile.MapEditor.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Tile.MapEditor
{
    public sealed partial class MapEditorSubSystem
    {
        private Vector2Int? _areaStrokeStart, _areaStrokeEnd;
        private bool _areaStrokeRectangle;
        private Vector2Int[] _areaStrokeTiles;

        private void OnRoomPaletteSelected(ushort areaId)
        {
            CancelAreaStroke();
            if (areaId == 0) ClearAreaSelection();
            else
            {
                if (_areaOverlay.LabelPositions.TryGetValue(areaId, out Vector3 center)) _session.CenterOn(center);
                _selectedPaintAreaId = areaId;
                _viewModel.AreaRoomId = areaId;
                if (SubSystems.TryGet(out AreaSubSystem areas)) _viewModel.AreaRoomName = areas.FloorVisualCache.GetAreaName(areaId);
                RpcInspectSelectedArea(areaId, false, LocalConnection);
            }
            OnToolSelected(MapEditorTool.Edit);
        }

        private void CancelAreaStroke()
        {
            _areaStrokeStart = _areaStrokeEnd = null;
            _areaStrokeTiles = null;
            _areaOverlay.ClearPaintPreview();
        }

        private void HandleAreaInput()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { CancelAreaStroke(); return; }
            if (CurrentTool is MapEditorTool.Select or MapEditorTool.Dropper)
            {
                if (_controls.Place.WasPerformedThisFrame() && !MouseOverUI)
                    RpcInspectArea(TileHelper.GetPointedPosition(true, PickCamera),
                        CurrentTool == MapEditorTool.Select, CurrentTool == MapEditorTool.Dropper, LocalConnection);
                return;
            }
            if (CurrentTool is not (MapEditorTool.Edit or MapEditorTool.Delete)) return;
            Vector3 world = TileHelper.GetPointedPosition(true, PickCamera);
            Vector2Int tile = new(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z));
            if (_controls.Place.WasPerformedThisFrame() && !MouseOverUI)
            {
                _areaStrokeStart = tile;
                _areaStrokeEnd = null;
                _areaStrokeRectangle = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
            }
            if (!_areaStrokeStart.HasValue) return;
            if (_areaStrokeEnd != tile)
            {
                _areaStrokeEnd = tile;
                _areaStrokeTiles = BuildAreaStroke(_areaStrokeStart.Value, tile, _areaStrokeRectangle);
                ushort previewId = CurrentTool == MapEditorTool.Delete ? (ushort)0 : _selectedPaintAreaId;
                _areaOverlay.ShowPaintPreview(_areaStrokeTiles, previewId, CurrentTool == MapEditorTool.Delete);
            }
            if (_controls.Place.IsPressed()) return;
            Vector2Int[] tiles = _areaStrokeTiles;
            CancelAreaStroke();
            if (MouseOverUI) return;
            if (tiles == null || tiles.Length == 0) { ShowLocalToast("Stroke is too large (maximum 4096 tiles)."); return; }
            if (CurrentTool == MapEditorTool.Edit && _selectedPaintAreaId == 0)
                RpcCreateRoom(tiles, LocalConnection);
            else RpcPaintArea(CurrentTool == MapEditorTool.Delete ? (ushort)0 : _selectedPaintAreaId, tiles, LocalConnection);
        }

        public static Vector2Int[] BuildAreaStroke(Vector2Int start, Vector2Int end, bool rectangle)
        {
            long width = Math.Abs((long)end.x - start.x) + 1;
            long height = Math.Abs((long)end.y - start.y) + 1;
            if (width > 4096 || height > 4096 || (rectangle && width * height > 4096)) return Array.Empty<Vector2Int>();
            if (rectangle)
            {
                Vector2Int[] tiles = new Vector2Int[(int)(width * height)];
                int index = 0;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        tiles[index++] = new Vector2Int(Math.Min(start.x, end.x) + x, Math.Min(start.y, end.y) + y);
                return tiles;
            }
            int steps = (int)Math.Max(width, height);
            Vector2Int[] line = new Vector2Int[steps];
            for (int i = 0; i < steps; i++)
                line[i] = Vector2Int.RoundToInt(Vector2.Lerp(start, end, steps == 1 ? 0 : (float)i / (steps - 1)));
            return line;
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcCreateRoom(Vector2Int[] tiles, NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn) || !SubSystems.TryGet(out AreaSubSystem areas)) return;
            EnsureCommandServiceBound();
            if (_commandService.Context == null) return;
            bool wasAuthored = areas.HasAuthoredAreas;
            AreaRecord room = areas.CreateAuthoredRoom();
            if (room == null) return;
            if (!areas.TryPaintArea(room.Id.Value, tiles, out AreaPaintingService.AreaPaintChange stroke, out string error))
            {
                areas.UndoCreatedRoom(room, wasAuthored);
                TargetToast(conn, error);
                return;
            }
            _commandService.Execute(new CreateRoomCommand(areas, room, stroke, wasAuthored), conn.ClientId);
            _inspectedRooms[conn] = room;
            TargetAreaProperties(conn, room.Id.Value, Vector3.zero, room.DisplayName, room.ParentTag, room.AmbienceProfileId, 0);
            TargetSyncUndoState(conn, _commandService.UndoDepth, _commandService.RedoDepth, true);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcPaintArea(ushort target, Vector2Int[] tiles, NetworkConnection conn = null)
        {
            if (!MapEditorPermissions.TryAuthorize(conn) || !SubSystems.TryGet(out AreaSubSystem areas)) return;
            EnsureCommandServiceBound();
            if (_commandService.Context == null) return;
            if (target != 0 && (!_inspectedRooms.TryGetValue(conn, out AreaRecord selected) || selected.Id.Value != target ||
                !areas.TryGetArea(selected.Id, out AreaRecord current) || !ReferenceEquals(selected, current)))
            { TargetToast(conn, "Select the room again before painting."); return; }
            if (!areas.TryPaintArea(target, tiles, out AreaPaintingService.AreaPaintChange change, out string error))
            { TargetToast(conn, error); return; }
            _commandService.Execute(new PaintRoomCommand(change), conn.ClientId);
            if (_inspectedRooms.TryGetValue(conn, out AreaRecord inspected) && !areas.TryGetArea(inspected.Id, out _))
            {
                _inspectedRooms.Remove(conn);
                TargetClearAreaSelection(conn);
            }
            TargetSyncUndoState(conn, _commandService.UndoDepth, _commandService.RedoDepth, true);
        }

        private sealed class PaintRoomCommand : IMapEditorCommand
        {
            private readonly AreaPaintingService.AreaPaintChange _change;
            private bool _registered;
            public PaintRoomCommand(AreaPaintingService.AreaPaintChange change) => _change = change;
            public MapEditorCommandDto ToDto() => throw new NotSupportedException("Area strokes are server-local commands.");
            public void Apply(MapEditorCommandContext ctx) { if (_registered) _change.Redo(); _registered = true; }
            public void Revert(MapEditorCommandContext ctx) => _change.Undo();
        }

        private sealed class CreateRoomCommand : IMapEditorCommand
        {
            private readonly AreaSubSystem _areas;
            private readonly bool _wasAuthored;
            private readonly AreaRecord _room;
            private readonly AreaPaintingService.AreaPaintChange _stroke;
            private bool _registered;
            public CreateRoomCommand(AreaSubSystem areas, AreaRecord room,
                AreaPaintingService.AreaPaintChange stroke, bool wasAuthored)
            { _areas = areas; _room = room; _stroke = stroke; _wasAuthored = wasAuthored; }
            public MapEditorCommandDto ToDto() => throw new NotSupportedException("Room creation is server-local.");
            public void Apply(MapEditorCommandContext ctx)
            {
                if (_registered) { _areas.RestoreCreatedRoom(_room); _stroke.Redo(); }
                _registered = true;
            }
            public void Revert(MapEditorCommandContext ctx)
            {
                _stroke.Undo();
                _areas.UndoCreatedRoom(_room, _wasAuthored);
            }
        }
    }
}
