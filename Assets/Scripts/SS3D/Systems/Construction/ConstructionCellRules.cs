using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Construction
{
    /// <summary>
    /// Shape and validity of construction cell actions, shared by the client ghost and the server check
    /// (D3 of 2026-11_construction-mvp1). Pure over a <see cref="TileMap"/>; the client map can be partial,
    /// so the server repeats every check before it starts or finishes a step.
    /// </summary>
    public static class ConstructionCellRules
    {
        /// <summary>Horizontal reach from the body to the cell centre. One cell around you, diagonals included.</summary>
        public const float ReachMeters = 2f;

        /// <summary>Whether the cell has the shape the rule acts on (a hull hole, bare plating, …).</summary>
        public static bool Matches(TileMap map, ConstructionRule rule, Vector3 gridPosition)
        {
            if (map == null || rule == null || !rule.IsCellAction)
            {
                return false;
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
                _ => false,
            };
        }

        /// <summary>
        /// Why the matched rule cannot run here, or null when it can. Materials and reach are checked by the
        /// caller, which knows the held stack and the body.
        /// </summary>
        /// <param name="checkStation">Server only: area ids do not replicate, so clients skip the station check.</param>
        public static string Validate(TileMap map, ConstructionRule rule, Vector3 gridPosition, TileObjectSo result, bool checkStation)
        {
            if (!Matches(map, rule, gridPosition))
            {
                return "Nothing to do here";
            }

            if (rule.Target == ConstructionTargetKind.HullHole && checkStation && !IsInsideStation(map, gridPosition))
            {
                return "Outside the station";
            }

            if (rule.Outcome == ConstructionOutcome.Place)
            {
                if (result == null)
                {
                    return "Unknown result";
                }

                BuildFailReason[] failures = map.EvaluateBuild(result, gridPosition, Direction.North, replaceExisting: false);
                if (failures.Length > 0)
                {
                    return BuildFailMessages.FormatPrimary(failures);
                }
            }

            return null;
        }

        /// <summary>
        /// No new deck into space (construction.md §4): a hull hole keeps its area, open space has none.
        /// </summary>
        public static bool IsInsideStation(TileMap map, Vector3 gridPosition)
        {
            TileCoord coord = new(map.MapId, new Vector2Int(Mathf.RoundToInt(gridPosition.x), Mathf.RoundToInt(gridPosition.z)));
            return map.TryGetAreaId(coord, out ushort areaId) && areaId != 0;
        }

        public static bool InReach(Vector3 bodyPosition, Vector3 gridPosition)
        {
            Vector2 delta = new(bodyPosition.x - gridPosition.x, bodyPosition.z - gridPosition.z);
            return delta.sqrMagnitude <= ReachMeters * ReachMeters;
        }

        public static bool IsPlating(ITileLocation plenum) =>
            plenum is SingleTileLocation single && !single.IsEmpty()
            && single.PlacedObject != null && single.PlacedObject.NameString == ConstructionNames.Plenum;
    }
}
