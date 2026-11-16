using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapImport
{
    public sealed class Ss13TypeMapConfig
    {
        public string DefaultFloor { get; set; } = "TileGrey";

        public string DefaultWall { get; set; } = "SteelWall";

        public string DefaultWindow { get; set; } = "SteelWindow";

        public string DefaultDoor { get; set; } = "CivillianAirlock";

        public List<Ss13TypeMapEntry> Prefixes { get; } = new List<Ss13TypeMapEntry>();
    }

    public sealed class Ss13TypeMapEntry
    {
        public string Match { get; set; } = string.Empty;

        public MapImportKind Kind { get; set; }

        public string So { get; set; } = string.Empty;
    }
}
