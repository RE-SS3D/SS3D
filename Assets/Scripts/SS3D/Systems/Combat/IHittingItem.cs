using SS3D.Systems.Health;

namespace SS3D.Systems.Combat
{
    public interface IHittingItem
    {
        HitType HitType { get; }

        DamageType DamageType { get; }

        float BaseDamage { get; }
    }
}
