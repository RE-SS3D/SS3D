using System.Collections.Generic;

namespace SS3D.Systems.Tile.MapImport.Dmm
{
    /// <summary>
    /// One typed path inside a DMM prefab key (turf, obj, area, …) with optional vars.
    /// </summary>
    public sealed class DmmAtom
    {
        public string Path { get; set; } = string.Empty;

        public Dictionary<string, string> Vars { get; } = new Dictionary<string, string>();

        public bool TryGetInt(string key, out int value)
        {
            value = 0;
            return Vars.TryGetValue(key, out string raw) && int.TryParse(raw.Trim(), out value);
        }
    }
}
