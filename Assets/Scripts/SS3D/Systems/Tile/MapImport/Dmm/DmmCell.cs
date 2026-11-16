using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapImport.Dmm
{
    /// <summary>
    /// One tile cell after expanding a DMM grid key, in BYOND map coordinates (Z ignored for v1).
    /// </summary>
    public sealed class DmmCell
    {
        public int X { get; set; }

        public int Y { get; set; }

        public int Z { get; set; }

        public string Key { get; set; } = string.Empty;

        public List<DmmAtom> Atoms { get; } = new List<DmmAtom>();
    }
}
