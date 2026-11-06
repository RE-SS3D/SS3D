using System;
using System.Collections.Generic;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Old saved names of renamed tile objects. Maps store objects by name and the loader drops
    /// names it can't resolve, so a rename without an entry here deletes the object from every saved map.
    /// </summary>
    public static class RenamedTileObjects
    {
        private static readonly Dictionary<string, string> NewNames = new(StringComparer.OrdinalIgnoreCase)
        {
            { "AtmosPipesL1", "AtmosPipesSupply" },
            { "AtmosPipesL2", "AtmosPipesMixed" },
            { "AtmosPipesL3", "AtmosPipesWaste" },
            { "AtmosPipesL4", "AtmosPipesSurface" },
        };

        public static IReadOnlyDictionary<string, string> All => NewNames;

        public static string Resolve(string savedName) =>
            savedName != null && NewNames.TryGetValue(savedName, out string newName) ? newName : savedName;
    }
}
