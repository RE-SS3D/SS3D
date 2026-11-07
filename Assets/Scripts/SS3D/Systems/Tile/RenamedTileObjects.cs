using System;
using System.Collections.Generic;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Old saved names of renamed tile objects. Since schema 2 maps load by prefab id first
    /// (<see cref="SavedAssetLookup"/>), so a rename needs no entry here; these cover saves from before ids.
    /// </summary>
    public static class RenamedTileObjects
    {
        private static readonly Dictionary<string, string> NewNames = new(StringComparer.OrdinalIgnoreCase)
        {
            { "AtmosPipesL1", "AtmosPipesSupply" },
            { "AtmosPipesL2", "AtmosPipesMixed" },
            { "AtmosPipesL3", "AtmosPipesWaste" },
            { "AtmosPipesL4", "AtmosPipesSurface" },
            { "Plant0", "PlantEarth" },
            { "Plant7", "PlantExotic" },
        };

        public static IReadOnlyDictionary<string, string> All => NewNames;

        public static string Resolve(string savedName) =>
            savedName != null && NewNames.TryGetValue(savedName, out string newName) ? newName : savedName;
    }
}
