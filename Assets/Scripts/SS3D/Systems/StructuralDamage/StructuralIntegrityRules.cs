using SS3D.Systems.Tile;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Provisional integrity thresholds (balance deferred). Stage from remaining HP fraction.
    /// </summary>
    public static class StructuralIntegrityRules
    {
        public const float DefaultWallMaxIntegrity = 100f;
        public const float DefaultDoorMaxIntegrity = 80f;
        public const float DefaultWindowMaxIntegrity = 50f;
        public const float DefaultTableMaxIntegrity = 100f;
        public const float DefaultFloorMaxIntegrity = 60f;
        public const float DefaultPlenumMaxIntegrity = 200f;

        /// <summary>Remaining fraction at or below which the tile becomes Damaged.</summary>
        public const float DamagedFraction = 0.7f;

        /// <summary>Remaining fraction at or below which the tile becomes Cracked/venting.</summary>
        public const float CrackedFraction = 0.35f;

        public static bool IsStructural(PlacedTileObject placed)
        {
            return placed != null && IsStructural(placed.tileObjectSO);
        }

        public static bool IsSteelTable(TileObjectSo so)
        {
            return so != null && so.layer == TileLayer.FurnitureBase
                && so.genericType == TileObjectGenericType.Table
                && so.specificType == TileObjectSpecificType.Steel;
        }

        public static bool IsWoodTable(TileObjectSo so)
        {
            return so != null && so.layer == TileLayer.FurnitureBase
                && so.genericType == TileObjectGenericType.Table
                && so.specificType == TileObjectSpecificType.Wood;
        }

        public static bool IsGlassTable(TileObjectSo so)
        {
            return so != null && so.layer == TileLayer.FurnitureBase
                && so.genericType == TileObjectGenericType.Table
                && so.specificType == TileObjectSpecificType.Glass;
        }

        public static bool IsSupportedTable(TileObjectSo so) => IsSteelTable(so) || IsWoodTable(so) || IsGlassTable(so);

        public static bool IsMetalFurniture(TileObjectSo so)
        {
            return so != null && so.layer == TileLayer.FurnitureBase && so.structuralDamageEnabled
                && !IsSupportedTable(so);
        }

        public static bool IsStructural(TileObjectSo so)
        {
            return so != null && (IsSupportedTable(so) || IsMetalFurniture(so) || (so.layer == TileLayer.Turf
                && (so.genericType == TileObjectGenericType.Wall || so.genericType == TileObjectGenericType.Door)));
        }

        /// <summary>Direct-hit lookup prefers structural turf, then furniture; floors use the deferred blast-floor path.</summary>
        public static bool TryGetTarget(ITileQueryService query, TileCoord coord, out PlacedTileObject placed)
        {
            placed = null;
            if (query == null)
                return false;

            if (query.TryGetOccupant(coord, TileLayer.Turf, Direction.North, out ITileOccupant turf)
                && turf is PlacedTileObject structure && IsStructural(structure))
                placed = structure;
            else if (query.TryGetOccupant(coord, TileLayer.FurnitureBase, Direction.North, out ITileOccupant furniture)
                && furniture is PlacedTileObject table && IsStructural(table))
                placed = table;

            return placed != null;
        }

        public static bool IsBlastFloor(TileObjectSo so) => so != null
            && (so.layer == TileLayer.Plenum || (so.layer == TileLayer.Turf && so.genericType == TileObjectGenericType.Floor));

        public static float ResolveMaxIntegrity(TileObjectSo tileObjectSo)
        {
            if (tileObjectSo == null)
                return DefaultWallMaxIntegrity;

            if (tileObjectSo.structuralMaxIntegrity > 0f)
                return tileObjectSo.structuralMaxIntegrity;

            if (tileObjectSo.layer == TileLayer.Plenum)
                return DefaultPlenumMaxIntegrity;
            if (tileObjectSo.layer == TileLayer.Turf && tileObjectSo.genericType == TileObjectGenericType.Floor)
                return DefaultFloorMaxIntegrity;

            if (IsGlassTable(tileObjectSo))
                return DefaultWindowMaxIntegrity;

            if (IsSupportedTable(tileObjectSo))
                return DefaultTableMaxIntegrity;

            if (tileObjectSo.genericType == TileObjectGenericType.Door)
                return DefaultDoorMaxIntegrity;

            if (TileOccupancyEvaluator.IsWindowName(tileObjectSo.NameString))
                return DefaultWindowMaxIntegrity;

            return DefaultWallMaxIntegrity;
        }

        public static StructuralIntegrityStage StageFromRemaining(float remaining, float maxIntegrity)
        {
            if (maxIntegrity <= 0f || remaining <= 0f)
                return StructuralIntegrityStage.Destroyed;

            float fraction = remaining / maxIntegrity;
            if (fraction <= CrackedFraction)
                return StructuralIntegrityStage.Cracked;

            if (fraction <= DamagedFraction)
                return StructuralIntegrityStage.Damaged;

            return StructuralIntegrityStage.Intact;
        }

        public static bool LeaksAtmosphere(StructuralIntegrityStage stage)
        {
            return stage == StructuralIntegrityStage.Cracked
                || stage == StructuralIntegrityStage.Destroyed;
        }
    }
}
