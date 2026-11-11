using System;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Construction
{
    /// <summary>
    /// Shape and validity of construction cell steps: the hull-hole ghost and the steps offered on a hovered
    /// floor or plating (<see cref="ConstructionRule.ActsOnCell"/>), shared by client and server. Pure over a <see cref="TileMap"/> plus a wreckage lookup; the
    /// client map can be partial, so the server repeats every check before it starts or finishes a step.
    /// </summary>
    public static class ConstructionCellRules
    {
        /// <summary>Horizontal reach from the body to the cell centre. One cell around you, diagonals included.</summary>
        public const float ReachMeters = 2f;

        public static TileCoord CoordOf(TileMap map, Vector3 gridPosition) =>
            new(map.MapId, new Vector2Int(Mathf.RoundToInt(gridPosition.x), Mathf.RoundToInt(gridPosition.z)));

        /// <summary>Whether the cell has the shape the rule acts on (a hull hole, bare plating, …).</summary>
        /// <param name="hasWreckage">Wreckage lookup (<see cref="TileSubSystem.HasWreckage"/>); null means none.</param>
        public static bool Matches(TileMap map, ConstructionRule rule, Vector3 gridPosition,
            Func<TileCoord, TileLayer, bool> hasWreckage = null)
        {
            if (map == null || rule == null || !(rule.IsCellAction || rule.ActsOnCell))
            {
                return false;
            }

            if (rule.Target == ConstructionTargetKind.Wreckage)
            {
                return BuildChecker.WreckageBlocks(hasWreckage, CoordOf(map, gridPosition), TileLayer.Turf);
            }

            if (!map.TryGetTileLocations(gridPosition, out ITileLocation[] locations))
            {
                return false;
            }

            ITileLocation plenum = locations[(int)TileLayer.Plenum];
            ITileLocation turf = locations[(int)TileLayer.Turf];

            return rule.Target switch
            {
                ConstructionTargetKind.HullHole => plenum.IsFullyEmpty(),
                ConstructionTargetKind.BarePlating => IsPlating(plenum) && turf.IsFullyEmpty(),
                ConstructionTargetKind.ClearFloor => IsPlating(plenum) && (turf.IsFullyEmpty() || IsFloor(turf)),
                _ => false,
            };
        }

        /// <summary>
        /// Why the matched rule cannot run here, or null when it can. Materials and reach are checked by the
        /// caller, which knows the held stack and the body.
        /// </summary>
        /// <param name="checkStation">Server only: area ids do not replicate, so clients skip the station check.</param>
        public static string Validate(TileMap map, ConstructionRule rule, Vector3 gridPosition, TileObjectSo result,
            bool checkStation, Func<TileCoord, TileLayer, bool> hasWreckage = null)
        {
            if (!Matches(map, rule, gridPosition, hasWreckage))
            {
                return "Nothing to do here";
            }

            if (rule.Target == ConstructionTargetKind.HullHole && checkStation && !IsInsideStation(map, gridPosition))
            {
                return "Outside the station";
            }

            if (rule.Outcome != ConstructionOutcome.Place)
            {
                return null;
            }

            if (result == null)
            {
                return "Unknown result";
            }

            // Wreckage blocks building on its layer (construction.md §8).
            if (BuildChecker.WreckageBlocks(hasWreckage, CoordOf(map, gridPosition), result.layer))
            {
                return BuildFailMessages.Format(BuildFailReason.WreckageInTheWay);
            }

            BuildFailReason[] failures = map.EvaluateBuild(result, gridPosition, Direction.North, ReplacesExisting(map, rule, gridPosition));
            return failures.Length > 0 ? BuildFailMessages.FormatPrimary(failures) : null;
        }

        /// <summary>A girder on a floored cell replaces the floor tile; everything else places into an empty layer.</summary>
        public static bool ReplacesExisting(TileMap map, ConstructionRule rule, Vector3 gridPosition) =>
            rule.Target == ConstructionTargetKind.ClearFloor
            && map.TryGetTileLocation(TileLayer.Turf, gridPosition, out ITileLocation turf)
            && !turf.IsFullyEmpty();

        /// <summary>
        /// No new deck into space (construction.md §4): a hull hole keeps its area, open space has none.
        /// </summary>
        public static bool IsInsideStation(TileMap map, Vector3 gridPosition) =>
            map.TryGetAreaId(CoordOf(map, gridPosition), out ushort areaId) && areaId != 0;

        public static bool InReach(Vector3 bodyPosition, Vector3 gridPosition)
        {
            Vector2 delta = new(bodyPosition.x - gridPosition.x, bodyPosition.z - gridPosition.z);
            return delta.sqrMagnitude <= ReachMeters * ReachMeters;
        }

        /// <summary>Plating, not a catwalk or lattice. Same name test as <see cref="BuildChecker"/>.</summary>
        public static bool IsPlating(ITileLocation plenum) =>
            plenum is SingleTileLocation single && !single.IsEmpty()
            && single.PlacedObject != null && single.PlacedObject.NameString.Contains(ConstructionNames.Plenum);

        private static bool IsFloor(ITileLocation turf) =>
            turf is SingleTileLocation single && !single.IsEmpty()
            && single.PlacedObject != null && single.PlacedObject.GenericType == TileObjectGenericType.Floor;
    }
}
