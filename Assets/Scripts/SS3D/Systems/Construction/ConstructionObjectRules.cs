using SS3D.Systems.Tile;

namespace SS3D.Systems.Construction
{
    /// <summary>
    /// Which placed tile objects an object step (weld, pry, repair) acts on. Shared by discovery, the
    /// hover outline, <c>CanInteract</c> and the server's completion check.
    /// </summary>
    public static class ConstructionObjectRules
    {
        public static bool IsDamaged(PlacedTileObject placed) =>
            placed.IntegrityStage is StructuralIntegrityStage.Damaged or StructuralIntegrityStage.Cracked;

        /// <summary>Whether <paramref name="placed"/> is the kind of object <paramref name="rule"/> acts on.</summary>
        public static bool Matches(ConstructionRule rule, PlacedTileObject placed)
        {
            if (rule == null || rule.IsCellAction || placed == null || placed.tileObjectSO == null || placed.Layer != rule.Layer)
            {
                return false;
            }

            return rule.Target switch
            {
                ConstructionTargetKind.FloorTurf => placed.GenericType == TileObjectGenericType.Floor,
                ConstructionTargetKind.DamagedPlenum => placed.GenericType == TileObjectGenericType.Plenum && IsDamaged(placed),
                ConstructionTargetKind.DamagedStructure => placed.GenericType == TileObjectGenericType.Wall && IsDamaged(placed),
                ConstructionTargetKind.NamedTile => placed.NameString == rule.TargetName
                    && (!rule.RequiresIntact || !IsDamaged(placed)),
                _ => false,
            };
        }
    }
}
