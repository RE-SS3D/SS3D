using System;
using SS3D.Systems.Tile.MapImport.Dmm;

namespace SS3D.Systems.Tile.MapImport
{
    /// <summary>
    /// Collapses SS13 pipe network types / piping_layer onto the SS3D pipe lines.
    /// Network type in the path beats the numeric piping_layer var.
    /// </summary>
    public static class MapImportPipeResolver
    {
        public const string AtmosPipesSupply = "AtmosPipesSupply";
        public const string AtmosPipesMixed = "AtmosPipesMixed";
        public const string AtmosPipesWaste = "AtmosPipesWaste";
        public const string AtmosPipesSurface = "AtmosPipesSurface";

        public static string Resolve(string path, DmmAtom atom)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (ContainsSegment(path, "/supply"))
                    return AtmosPipesSupply;
                if (ContainsSegment(path, "/scrubbers"))
                    return AtmosPipesMixed;
                if (ContainsSegment(path, "/waste") ||
                    ContainsSegment(path, "/general") ||
                    ContainsSegment(path, "/yellow"))
                    return AtmosPipesWaste;
            }

            if (atom != null && atom.TryGetInt("piping_layer", out int layer))
            {
                return layer switch
                {
                    1 => AtmosPipesSupply,
                    2 => AtmosPipesMixed,
                    3 => AtmosPipesWaste,
                    4 => AtmosPipesSurface,
                    _ => AtmosPipesWaste,
                };
            }

            return AtmosPipesWaste;
        }

        private static bool ContainsSegment(string path, string segment) =>
            path.IndexOf(segment, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
