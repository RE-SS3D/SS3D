using SS3D.Systems.Tile.FloorVisuals;
using System;

namespace SS3D.Systems.Tile.MapEditor
{
    /// <summary>Stable catalog keys for the floor paint swatches (<c>floor-paint:3</c>), one per palette colour.</summary>
    public static class MapEditorFloorPaintCatalog
    {
        public const string Prefix = "floor-paint:";

        public static string Encode(int paletteIndex) => $"{Prefix}{paletteIndex}";

        public static bool TryDecode(string assetName, out int paletteIndex)
        {
            paletteIndex = 0;
            return !string.IsNullOrEmpty(assetName)
                   && assetName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                   && int.TryParse(assetName.Substring(Prefix.Length), out paletteIndex)
                   && FloorPaint.IsPaint(paletteIndex);
        }
    }
}
