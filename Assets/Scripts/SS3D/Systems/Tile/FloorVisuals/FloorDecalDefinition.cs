using UnityEngine;

namespace SS3D.Systems.Tile.FloorVisuals
{
    /// <summary>
    /// Catalog entry for a sparse floor decal (warning lines, plaques, etc.).
    /// </summary>
    [CreateAssetMenu(fileName = "FloorDecalDefinition", menuName = "TileMap/FloorDecalDefinition", order = 1)]
    public sealed class FloorDecalDefinition : ScriptableObject
    {
        [Tooltip("Stable id stored per tile. 0 is reserved for empty.")]
        public ushort Id = 1;

        public string DisplayName = "Floor Decal";

        public Texture2D Texture;

        public Material MaterialOverride;

        public Color Tint = Color.white;
    }
}
