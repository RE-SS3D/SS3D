namespace SS3D.Systems.Construction
{
    /// <summary>
    /// Catalog names (<c>GenericObjectSo.NameString</c>, i.e. prefab names) that construction rules key on.
    /// <c>ConstructionRecipesTests</c> proves every one resolves to a real asset.
    /// </summary>
    public static class ConstructionNames
    {
        // Tile objects
        public const string Plenum = "Plenum";
        public const string FloorTurf = "TileGrey";
        public const string Girder = "SteelGirder";
        public const string GirderPlated = "SteelGirderPlated";
        public const string Wall = "SteelWall";
        public const string Window = "SteelWindow";

        // Items
        public const string SteelSheetItem = "SteelSheet";
        public const string GlassSheetItem = "GlassSheet";
        public const string FloorTileItem = "FloorTile";
        public const string WelderItem = "Welder";
        public const string CrowbarItem = "Crowbar";
    }
}
