using SS3D.Systems.Tile;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Hop-based blast resolution: yield at epicenter, subtractive falloff per open hop.
    /// </summary>
    public interface IBlastResolutionService
    {
        /// <summary>
        /// Server-only. Spreads force from <paramref name="epicenter"/> until remaining force
        /// drops below the useful minimum. Structural turf and crew on visited tiles take damage.
        /// </summary>
        void Resolve(TileCoord epicenter, float yield, float falloff);
    }
}
