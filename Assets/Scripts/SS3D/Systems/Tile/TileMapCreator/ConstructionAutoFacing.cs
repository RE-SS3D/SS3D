using SS3D.Core;
using SS3D.Systems.Tile.MapEditor;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SS3D.Systems.Tile.TileMapCreator
{
    /// <summary>
    /// Which way the map editor's cursor ghost faces: a wall mount goes on the wall face under the cursor, a door lines up
    /// with the wall it goes in. The ghost turns only when that answer changes, so R's manual choice stands until the
    /// cursor reaches another face or cell.
    /// </summary>
    internal sealed class ConstructionAutoFacing
    {
        private readonly List<Direction> _sides = new(4);
        private Direction _mountFace;
        private bool _hasMountFace;
        private Vector2Int _appliedCell;
        private Direction _applied;
        private bool _hasApplied;

        /// <summary>Forget the last facing applied, so the next ghost turns to its own (a new object was picked).</summary>
        public void Reset() => _hasApplied = false;

        /// <summary>
        /// The wall cell a wall mount goes on, remembering the face for <see cref="Apply"/>: the side of the wall the
        /// cursor ray hits, else the side of a wall cell the cursor is over, else the nearest wall beside the floor cell
        /// under the cursor, facing back into that cell. Falls back to the cell under the cursor.
        /// </summary>
        public Vector3 AimWallMount(Camera camera, TileMap map)
        {
            _hasMountFace = false;
            Vector3 ground = TileHelper.GetPointedPosition(false, camera);
            Vector3 groundCell = TileHelper.GetClosestPosition(ground);
            if (map == null || camera == null || Mouse.current == null)
                return groundCell;

            Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, StationBounds.WithoutCeiling(Physics.DefaultRaycastLayers),
                    QueryTriggerInteraction.Ignore))
            {
                PlacedTileObject placed = hit.collider.GetComponentInParent<PlacedTileObject>();
                if (placed != null && placed.tileObjectSO != null)
                {
                    // The live transform, not WorldOrigin, which can lag its SyncVar on a remote client.
                    Vector3 cell = TileHelper.GetClosestPosition(placed.transform.position);
                    if (placed.tileObjectSO.layer is TileLayer.WallMountHigh or TileLayer.WallMountLow)
                        return FaceOf(cell, placed.Direction);

                    if (MapEditorPlacementRules.KindOf(placed.tileObjectSO) == TurfKind.Wall)
                    {
                        // The side the ray hit; a wall's end or top, where a neighbouring wall usually sits, falls back
                        // to the open side nearest the hit.
                        Vector3 towards = Mathf.Abs(hit.normal.y) < 0.7f ? hit.normal : hit.point - cell;
                        return FaceOf(cell, OpenSideOf(map, cell, towards, hit.point - cell));
                    }
                }
            }

            if (MapEditorPlacementRules.TurfKindAt(map, groundCell) == TurfKind.Wall)
                return FaceOf(groundCell, OpenSideOf(map, groundCell, ground - groundCell, ground - groundCell));

            MapEditorPlacementRules.CardinalsByCloseness(ground - groundCell, _sides);
            foreach (Direction side in _sides)
            {
                Vector3 wallCell = MapEditorPlacementRules.Step(groundCell, side);
                if (MapEditorPlacementRules.TurfKindAt(map, wallCell) == TurfKind.Wall)
                    return FaceOf(wallCell, TileHelper.GetOpposite(side));
            }

            return groundCell;
        }

        /// <summary>
        /// Turns <paramref name="hologram"/> to the facing worked out for <paramref name="tile"/> at
        /// <paramref name="position"/>, if that facing changed since the last call. Returns whether the ghost turned.
        /// </summary>
        public bool Apply(ConstructionHologram hologram, Vector3 position, TileObjectSo tile, bool isWallMount, TileMap map)
        {
            Direction wanted;
            bool isDoor = MapEditorPlacementRules.KindOf(tile) == TurfKind.Door;
            if (isWallMount)
            {
                if (!_hasMountFace)
                    return false;
                wanted = _mountFace;
            }
            else if (!isDoor || !MapEditorPlacementRules.TryInferDoorDirection(map, position, out wanted))
            {
                return false;
            }

            Vector2Int cell = new(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z));
            if (_hasApplied && _appliedCell == cell && _applied == wanted)
                return false;

            _hasApplied = true;
            _appliedCell = cell;
            _applied = wanted;
            Direction before = hologram.Direction;
            return hologram.TrySetDirection(wanted, acceptOpposite: isDoor) && hologram.Direction != before;
        }

        private Vector3 FaceOf(Vector3 cell, Direction face)
        {
            _mountFace = face;
            _hasMountFace = true;
            return cell;
        }

        /// <summary>
        /// <paramref name="preferred"/>'s side of a wall when nothing walls it off, else the side without a neighbouring
        /// wall that <paramref name="offset"/> points most towards.
        /// </summary>
        private Direction OpenSideOf(TileMap map, Vector3 wallCell, Vector3 preferred, Vector3 offset)
        {
            Direction side = MapEditorPlacementRules.CardinalFrom(preferred);
            if (MapEditorPlacementRules.TurfKindAt(map, MapEditorPlacementRules.Step(wallCell, side)) != TurfKind.Wall)
                return side;

            MapEditorPlacementRules.CardinalsByCloseness(offset, _sides);
            foreach (Direction candidate in _sides)
            {
                if (MapEditorPlacementRules.TurfKindAt(map, MapEditorPlacementRules.Step(wallCell, candidate)) != TurfKind.Wall)
                    return candidate;
            }

            return side;
        }
    }
}
