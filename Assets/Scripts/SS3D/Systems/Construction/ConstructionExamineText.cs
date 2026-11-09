using SS3D.Core;
using SS3D.Systems.Examine;
using SS3D.Systems.Tile;

namespace SS3D.Systems.Construction
{
    /// <summary>
    /// The next construction step for a placed structure, as an examine line: "A welder would repair it."
    /// The object is the readout (construction.md §1); nothing here performs an action.
    /// </summary>
    public static class ConstructionExamineText
    {
        /// <summary>Key and English fallback for the next step, or false when there is none to suggest.</summary>
        public static bool TryGetNextStep(PlacedTileObject placed, out string key, out string fallback)
        {
            key = null;
            fallback = null;
            if (placed == null || placed.tileObjectSO == null)
            {
                return false;
            }

            if (ConstructionObjectRules.IsDamaged(placed)
                && (placed.GenericType == TileObjectGenericType.Plenum || placed.GenericType == TileObjectGenericType.Wall))
            {
                key = ExamineConstructionKeys.RepairWelder;
                fallback = ExamineConstructionKeys.RepairWelderFallback;
                return true;
            }

            if (placed.Layer == TileLayer.Turf && placed.GenericType == TileObjectGenericType.Floor)
            {
                key = ExamineConstructionKeys.FloorPry;
                fallback = ExamineConstructionKeys.FloorPryFallback;
                return true;
            }

            if (placed.GenericType == TileObjectGenericType.Plenum && IsBare(placed))
            {
                key = ExamineConstructionKeys.BarePlating;
                fallback = ExamineConstructionKeys.BarePlatingFallback;
                return true;
            }

            return false;
        }

        private static bool IsBare(PlacedTileObject plenum)
        {
            if (!SubSystems.TryGet(out TileSubSystem tiles) || tiles.CurrentMap == null)
            {
                return false;
            }

            // TryGet: examine must not create chunks.
            return !tiles.CurrentMap.TryGetTileLocation(TileLayer.Turf, ConstructionApply.GridWorld(plenum), out ITileLocation turf)
                || turf.IsFullyEmpty();
        }
    }
}
