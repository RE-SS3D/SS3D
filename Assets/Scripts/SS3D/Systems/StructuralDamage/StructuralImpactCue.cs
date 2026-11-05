using SS3D.Systems.Tile;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>Which sound a structural impact message asks each client to play. Travels on the wire.</summary>
    public enum StructuralImpactCue : byte
    {
        /// <summary>Particles only. The killing blow's sound comes from the break that follows.</summary>
        None = 0,
        Hit = 1,

        /// <summary>A bullet off a metal surface.</summary>
        Ricochet = 2,

        /// <summary>A hit that reached Cracked, adding a heavier strain.</summary>
        Strain = 3,

        /// <summary>The structure broke and has no wreckage message of its own.</summary>
        Break = 4,
    }

    public static class StructuralImpactCues
    {
        /// <summary>Cue for a landed hit, given the stage before and after it.</summary>
        public static StructuralImpactCue Resolve(StructuralIntegrityStage before, StructuralIntegrityStage after,
            StructuralMaterial material, StructuralDamageSource source)
        {
            if (after == StructuralIntegrityStage.Destroyed)
                return StructuralImpactCue.None;

            if (after == StructuralIntegrityStage.Cracked && before != StructuralIntegrityStage.Cracked)
                return StructuralImpactCue.Strain;

            return source == StructuralDamageSource.Ranged && material == StructuralMaterial.Metal
                ? StructuralImpactCue.Ricochet
                : StructuralImpactCue.Hit;
        }
    }
}
