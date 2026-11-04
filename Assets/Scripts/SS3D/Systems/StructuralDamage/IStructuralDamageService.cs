using SS3D.Systems.Tile;

namespace SS3D.Systems.StructuralDamage
{
    public interface IStructuralDamageService
    {
        /// <summary>
        /// Server-authoritative structural damage. Returns false if the tile has no supported structure or steel table.
        /// Destroyed clears the target from its own layer via construction.
        /// </summary>
        bool TryApplyStructuralDamage(TileCoord coord, float force, StructuralDamageSource source);

        bool TryGetIntegrity(TileCoord coord, out StructuralIntegrityStage stage, out float remaining, out float max);
    }
}
