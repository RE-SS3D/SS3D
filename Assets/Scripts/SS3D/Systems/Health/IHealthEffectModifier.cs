namespace SS3D.Systems.Health
{
    /// <summary>
    /// Per-tick deltas on organ function and systemic pools from downstream systems (virology, chemistry, stamina overdraw).
    /// </summary>
    public interface IHealthEffectModifier
    {
        void ApplyTick(ref SystemicPools pools, OrganState[] organs);
    }
}
