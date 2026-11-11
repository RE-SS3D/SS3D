namespace SS3D.Systems.Construction
{
    /// <summary>
    /// Which floor tile item lays which floor (D17 of 2026-11_construction-mvp1): prying a white floor
    /// returns a white tile, and a white tile lays a white floor. Each item stacks only with its own kind.
    /// Floors with no row here (dirt, grass, carpet, wood) pry up to nothing.
    /// </summary>
    public static class ConstructionFloorTiles
    {
        /// <summary>Floor turf name, floor tile item name, item display name. The first row is the default.</summary>
        public static readonly (string Turf, string Item, string DisplayName)[] Kinds =
        {
            (ConstructionNames.FloorTurf, ConstructionNames.FloorTileItem, "Grey floor tile"),
            ("TileGreyDark", "FloorTileGreyDark", "Dark floor tile"),
            ("TileWhite", "FloorTileWhite", "White floor tile"),
            ("TileKitchen", "FloorTileKitchen", "Kitchen floor tile"),
            ("TileBar", "FloorTileBar", "Bar floor tile"),
        };

        /// <summary>The floor a tile item lays; the grey floor for an unknown item.</summary>
        public static string TurfFor(string itemName)
        {
            foreach ((string turf, string item, string _) in Kinds)
            {
                if (item == itemName)
                {
                    return turf;
                }
            }

            return ConstructionNames.FloorTurf;
        }

        /// <summary>The tile item a pried floor returns, or null when it is not a steel tile floor.</summary>
        public static string ItemFor(string turfName)
        {
            foreach ((string turf, string item, string _) in Kinds)
            {
                if (turf == turfName)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
