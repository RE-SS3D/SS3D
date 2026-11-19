using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>What a turf object is, for the editor's paint-over rules.</summary>
    public enum TurfKind
    {
        /// <summary>Not a turf object, or an empty turf slot.</summary>
        None,
        Floor,
        /// <summary>Walls and windows.</summary>
        Wall,
        Door,
    }

    public enum PlacementOutcome
    {
        Place,
        /// <summary>The same object is already there: nothing to do, and nothing to report.</summary>
        AlreadyThere,
        Blocked,
    }

    /// <summary>A cell the editor lays a subfloor on before placing what needs one.</summary>
    public readonly struct SubfloorCell
    {
        public SubfloorCell(Vector3 position, bool replacesGrating)
        {
            Position = position;
            ReplacesGrating = replacesGrating;
        }

        public Vector3 Position { get; }

        /// <summary>A catwalk or lattice is swapped for the subfloor, since turf can't stand on one; its wires and pipes stay.</summary>
        public bool ReplacesGrating { get; }
    }

    /// <summary>What one map editor placement will do: place (with or without replacing), skip, or refuse.</summary>
    public readonly struct PlacementPlan
    {
        private static readonly SubfloorCell[] NoSubfloor = Array.Empty<SubfloorCell>();
        private static readonly BuildFailReason[] NoFailures = Array.Empty<BuildFailReason>();

        public PlacementOutcome Outcome { get; init; }

        /// <summary>Swap out what is in the slot: Alt held, or the paint-over rules.</summary>
        public bool Replace { get; init; }

        public BuildFailReason[] Failures { get; init; }

        /// <summary>Cells that get a subfloor first; empty when the footprint already has one.</summary>
        public IReadOnlyList<SubfloorCell> Subfloor { get; init; }

        public bool CanPlace => Outcome == PlacementOutcome.Place;

        public string PrimaryMessage => BuildFailMessages.FormatPrimary(Failures);

        public static PlacementPlan Place(bool replace, IReadOnlyList<SubfloorCell> subfloor = null) => new()
        {
            Outcome = PlacementOutcome.Place,
            Replace = replace,
            Failures = NoFailures,
            Subfloor = subfloor ?? NoSubfloor,
        };

        public static PlacementPlan AlreadyThere => new()
        {
            Outcome = PlacementOutcome.AlreadyThere,
            Failures = NoFailures,
            Subfloor = NoSubfloor,
        };

        public static PlacementPlan Blocked(BuildFailReason[] failures, bool replace = false) => new()
        {
            Outcome = PlacementOutcome.Blocked,
            Replace = replace,
            Failures = failures ?? NoFailures,
            Subfloor = NoSubfloor,
        };
    }

    /// <summary>
    /// The map editor's placement rules on top of <see cref="BuildChecker"/>, shared by the client's ghosts and the
    /// server's commands so both agree on what a click does:
    /// <list type="bullet">
    /// <item>Auto subfloor: anything that needs a subfloor gets one when its cell has none (or only a catwalk under turf).</item>
    /// <item>Paint-over: floors repaint floors, walls go over floors and walls, doors go into floors, walls and doors,
    /// all without Alt. Floors never replace walls or doors, and walls never replace doors, unless Alt is held.</item>
    /// <item>Placing the same object where it already is does nothing, silently.</item>
    /// </list>
    /// Editor only: in-round construction still uses <see cref="BuildChecker"/> as is.
    /// </summary>
    public static class MapEditorPlacementRules
    {
        /// <summary>The subfloor asset auto subfloor lays (<see cref="GenericObjectSo.NameString"/>).</summary>
        public const string SubfloorAssetName = "Plenum";

        /// <summary>Largest drag the editor previews and sends: a 64 × 64 rectangle.</summary>
        public const int MaxDragCells = 4096;

        // A run of doors is scanned through to the walls either side of it, as SS13 import does.
        private const int DoorRunScan = 8;

        public static TurfKind KindOf(TileObjectSo tile) => tile == null ? TurfKind.None : KindOf(tile.layer, tile.genericType);

        public static TurfKind KindOf(TileLayer layer, TileObjectGenericType type)
        {
            if (layer != TileLayer.Turf)
                return TurfKind.None;

            return type switch
            {
                TileObjectGenericType.Wall => TurfKind.Wall,
                TileObjectGenericType.Door => TurfKind.Door,
                _ => TurfKind.Floor,
            };
        }

        /// <summary>Whether placing <paramref name="placing"/> swaps out <paramref name="existing"/> turf without Alt.</summary>
        public static bool PaintsOver(TurfKind placing, TurfKind existing) => (placing, existing) switch
        {
            (TurfKind.Floor, TurfKind.Floor) => true,
            (TurfKind.Wall, TurfKind.Floor or TurfKind.Wall) => true,
            (TurfKind.Door, TurfKind.Floor or TurfKind.Wall or TurfKind.Door) => true,
            _ => false,
        };

        /// <summary>
        /// Whether the room tool places <paramref name="placing"/> over <paramref name="existing"/>. A room drawn over a
        /// room re-skins it: its walls go over walls and floors on the border, its floor over floors inside. Doors and
        /// windows stay, as openings someone chose; a room built of windows does replace windows. Walls inside the
        /// rectangle stay too, as partitions.
        /// </summary>
        public static bool RoomBuildsOver(TurfKind placing, bool placingWindow, TurfKind existing, bool existingWindow) =>
            existing switch
            {
                TurfKind.None => true,
                TurfKind.Door => false,
                TurfKind.Wall when existingWindow && !placingWindow => false,
                _ => placing == TurfKind.Wall || existing == TurfKind.Floor,
            };

        /// <summary>Windows are walls to the tile map; the room tool and the room-wall memory tell them apart by name.</summary>
        public static bool IsWindow(TileObjectSo tile) =>
            tile != null && tile.NameString.IndexOf("Window", StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>The turf object in <paramref name="cell"/>, or null for an empty or unknown cell.</summary>
        public static TileObjectSo TurfAt(TileMap map, Vector3 cell)
        {
            if (map == null
                || !map.TryGetTileLocation(TileLayer.Turf, cell, out ITileLocation turf)
                || !turf.TryGetPlacedObject(out PlacedTileObject placed)
                || placed == null)
                return null;

            return placed.tileObjectSO;
        }

        /// <summary>The turf kind in <paramref name="cell"/>, or <see cref="TurfKind.None"/> for an empty or unknown cell.</summary>
        public static TurfKind TurfKindAt(TileMap map, Vector3 cell) => KindOf(TurfAt(map, cell));

        /// <summary>
        /// What placing <paramref name="tile"/> at <paramref name="position"/> will do.
        /// </summary>
        /// <param name="forceReplace">Alt is held: replace whatever is in the slot.</param>
        /// <param name="autoSubfloor">Lay a subfloor where the footprint has none.</param>
        public static PlacementPlan Plan(TileMap map, TileObjectSo tile, Vector3 position, Direction direction,
            bool forceReplace, bool autoSubfloor)
        {
            if (map == null || tile == null)
                return PlacementPlan.Blocked(null);

            Vector3 cell = TileHelper.GetClosestPosition(position);
            bool replace = forceReplace;

            if (map.TryGetTileLocation(tile.layer, cell, out ITileLocation slot)
                && slot.TryGetPlacedObject(out PlacedTileObject existing, direction)
                && existing != null
                && existing.tileObjectSO != null)
            {
                if (IsSameObject(tile, direction, existing.tileObjectSO, existing.Direction))
                    return PlacementPlan.AlreadyThere;

                if (PaintsOver(KindOf(tile), KindOf(existing.tileObjectSO)))
                    replace = true;
            }

            BuildFailReason[] failures = map.EvaluateBuild(tile, cell, direction, replace);
            if (failures.Length == 0)
                return PlacementPlan.Place(replace);

            if (!autoSubfloor || tile.layer == TileLayer.Plenum || !OnlySubfloorFailures(failures))
                return PlacementPlan.Blocked(failures, replace);

            List<SubfloorCell> subfloor = new(1);
            foreach (Vector2Int offset in tile.GetGridOffsetList(direction))
            {
                Vector3 footprint = new(cell.x + offset.x, 0f, cell.z + offset.y);
                map.TryGetTileLocation(TileLayer.Plenum, footprint, out ITileLocation plenumLocation);
                if (plenumLocation is not SingleTileLocation plenum)
                    return PlacementPlan.Blocked(failures, replace);

                if (plenum.IsEmpty())
                    subfloor.Add(new SubfloorCell(footprint, replacesGrating: false));
                else if (tile.layer == TileLayer.Turf && BuildChecker.IsGrating(plenum.PlacedObject.NameString))
                    subfloor.Add(new SubfloorCell(footprint, replacesGrating: true));
            }

            return subfloor.Count > 0
                ? PlacementPlan.Place(replace, subfloor)
                : PlacementPlan.Blocked(failures, replace);
        }

        /// <summary>
        /// The same asset already in the slot. Floors and walls have no facing worth keeping, so any direction
        /// matches; everything else must face the same way (a rotated chair is a different placement).
        /// </summary>
        public static bool IsSameObject(TileObjectSo placing, Direction direction, TileObjectSo existing, Direction existingDirection)
        {
            if (placing == null || existing == null
                || !string.Equals(placing.NameString, existing.NameString, StringComparison.Ordinal))
                return false;

            TurfKind kind = KindOf(placing);
            return kind is TurfKind.Floor or TurfKind.Wall || existingDirection == direction;
        }

        private static bool OnlySubfloorFailures(BuildFailReason[] failures)
        {
            foreach (BuildFailReason failure in failures)
            {
                if (failure is not (BuildFailReason.TurfNeedsPlenum or BuildFailReason.MissingOrInvalidPlenum))
                    return false;
            }

            return failures.Length > 0;
        }

        /// <summary>
        /// The direction a door in <paramref name="cell"/> faces, from the walls either side of it: walls north and south
        /// make an east-west passage (East), walls east and west a north-south one (South), as SS13 import infers.
        /// False at a corner, a crossing or in the open, where the author's rotation stands.
        /// </summary>
        public static bool TryInferDoorDirection(TileMap map, Vector3 cell, out Direction direction)
        {
            cell = TileHelper.GetClosestPosition(cell);
            bool northSouth = HasWallAlong(map, cell, 0, 1) || HasWallAlong(map, cell, 0, -1);
            bool eastWest = HasWallAlong(map, cell, 1, 0) || HasWallAlong(map, cell, -1, 0);

            direction = Direction.South;
            if (northSouth == eastWest)
                return false;

            direction = northSouth ? Direction.East : Direction.South;
            return true;
        }

        private static bool HasWallAlong(TileMap map, Vector3 cell, int dx, int dz)
        {
            for (int step = 1; step <= DoorRunScan; step++)
            {
                TurfKind kind = TurfKindAt(map, cell + new Vector3(dx * step, 0f, dz * step));
                if (kind == TurfKind.Wall)
                    return true;
                if (kind != TurfKind.Door)
                    return false;
            }

            return false;
        }

        /// <summary>The cardinal direction an offset within a tile points most towards (+z is North, +x East).</summary>
        public static Direction CardinalFrom(Vector3 offset)
        {
            if (Mathf.Abs(offset.x) > Mathf.Abs(offset.z))
                return offset.x >= 0f ? Direction.East : Direction.West;

            return offset.z >= 0f ? Direction.North : Direction.South;
        }

        /// <summary>The four cardinal directions, the one <paramref name="offset"/> points most towards first.</summary>
        public static void CardinalsByCloseness(Vector3 offset, List<Direction> into)
        {
            into.Clear();
            into.Add(Direction.North);
            into.Add(Direction.East);
            into.Add(Direction.South);
            into.Add(Direction.West);
            into.Sort((a, b) => Dot(offset, b).CompareTo(Dot(offset, a)));
        }

        private static float Dot(Vector3 offset, Direction direction)
        {
            Tuple<int, int> vector = TileHelper.ToCardinalVector(direction);
            return offset.x * vector.Item1 + offset.z * vector.Item2;
        }

        /// <summary>One step from <paramref name="cell"/> towards <paramref name="direction"/>.</summary>
        public static Vector3 Step(Vector3 cell, Direction direction)
        {
            Tuple<int, int> vector = TileHelper.ToCardinalVector(direction);
            return cell + new Vector3(vector.Item1, 0f, vector.Item2);
        }

        /// <summary>
        /// Splits the rectangle between two corner tiles into its border (walls) and interior (floor). A rectangle one tile
        /// wide or deep is all border.
        /// </summary>
        public static void RoomCells(Vector2Int corner, Vector2Int oppositeCorner, List<Vector3> border, List<Vector3> interior)
        {
            border.Clear();
            interior.Clear();
            int x1 = Math.Min(corner.x, oppositeCorner.x);
            int x2 = Math.Max(corner.x, oppositeCorner.x);
            int z1 = Math.Min(corner.y, oppositeCorner.y);
            int z2 = Math.Max(corner.y, oppositeCorner.y);

            for (int z = z1; z <= z2; z++)
            {
                for (int x = x1; x <= x2; x++)
                {
                    bool edge = x == x1 || x == x2 || z == z1 || z == z2;
                    (edge ? border : interior).Add(new Vector3(x, 0f, z));
                }
            }
        }

        /// <summary>Cells in the rectangle between two corner tiles.</summary>
        public static int RectangleCellCount(Vector2Int corner, Vector2Int oppositeCorner) =>
            (Math.Abs(corner.x - oppositeCorner.x) + 1) * (Math.Abs(corner.y - oppositeCorner.y) + 1);

        /// <summary>"7 × 5 · 35 tiles": a rectangle's size, for the cursor badge while dragging.</summary>
        public static string RectangleReadout(Vector2Int corner, Vector2Int oppositeCorner)
        {
            int width = Math.Abs(corner.x - oppositeCorner.x) + 1;
            int depth = Math.Abs(corner.y - oppositeCorner.y) + 1;
            return $"{width} × {depth} · {width * depth} tiles";
        }
    }
}
