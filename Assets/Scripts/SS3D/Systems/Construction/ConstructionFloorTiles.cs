namespace SS3D.Systems.Construction
{
    /// <summary>
    /// Which floor tile item lays which floor (D17 of 2026-11_construction-mvp1): prying a white floor
    /// returns a white tile, and a white tile lays a white floor. Each item stacks only with its own kind.
    /// Every floor tile in the game is in <see cref="Kinds"/> or <see cref="NotPryable"/>;
    /// <c>ConstructionCoverageTests</c> fails for a floor that is in neither.
    /// </summary>
    public static class ConstructionFloorTiles
    {
        /// <summary>
        /// Floor turf name, floor tile item name, item display name, and whether the item sounds like wood.
        /// The first row is the default. Items other than the first are built by <c>FloorTileItemRecipe</c>.
        /// </summary>
        public static readonly (string Turf, string Item, string DisplayName, bool Wooden)[] Kinds =
        {
            (ConstructionNames.FloorTurf, ConstructionNames.FloorTileItem, "Grey floor tile", false),
            ("TileGreyDark", "FloorTileGreyDark", "Dark floor tile", false),
            ("TileWhite", "FloorTileWhite", "White floor tile", false),
            ("TileKitchen", "FloorTileKitchen", "Kitchen floor tile", false),
            ("TileBar", "FloorTileBar", "Bar floor tile", false),
            ("TileWood", "FloorTileWood", "Wooden floor tile", true),
        };

        /// <summary>Floors a crowbar refuses, with why. They stay put instead of vanishing.</summary>
        public static readonly (string Turf, string Reason)[] NotPryable =
        {
            ("TileDirt1", "Natural ground, not a laid tile"),
            ("TileGrass", "Natural ground, not a laid tile"),
            ("FancyCarpetRed", "Carpet needs its own item; out of scope for MVP1"),
            ("TileReinforced", "Reinforced floors need rods and a wrench (construction.md §14, out of scope)"),
            ("TilePlating", "Plating-look turf; the hull plating itself is the Plenum layer"),
        };

        /// <summary>The floor a tile item lays; the grey floor for an unknown item.</summary>
        public static string TurfFor(string itemName)
        {
            foreach ((string turf, string item, string _, bool _) in Kinds)
            {
                if (item == itemName)
                {
                    return turf;
                }
            }

            return ConstructionNames.FloorTurf;
        }

        /// <summary>The tile item a pried floor returns, or null when the floor cannot be pried.</summary>
        public static string ItemFor(string turfName)
        {
            foreach ((string turf, string item, string _, bool _) in Kinds)
            {
                if (turf == turfName)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>Whether a crowbar can lift this floor: only floors with a tile item to hand back.</summary>
        public static bool IsPryable(string turfName) => ItemFor(turfName) != null;
    }
}
