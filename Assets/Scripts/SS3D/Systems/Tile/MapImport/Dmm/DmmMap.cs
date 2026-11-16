using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapImport.Dmm
{
    public sealed class DmmMap
    {
        public Dictionary<string, List<DmmAtom>> Prefabs { get; } =
            new Dictionary<string, List<DmmAtom>>();

        public List<DmmCell> Cells { get; } = new List<DmmCell>();

        public int KeyLength { get; set; }
    }
}
