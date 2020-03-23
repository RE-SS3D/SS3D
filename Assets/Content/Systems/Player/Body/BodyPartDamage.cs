using SS3D.Engine.Health;

namespace SS3D.Content.Systems.Player.Body
{
    /// <summary>
    /// Class to store and add/remove damage caused to a bodypart
    /// </summary>
    public class BodyPartDamage
    {
        public BodyPartDamage(DamageType damageType, float damageAmount)
        {
            DamageType = damageType;
            DamageAmount = damageAmount;
        }

        public DamageType DamageType { get; }
        public float DamageAmount { get; private set; }

        public float Damage(float damageAmount)
        {
            DamageAmount += damageAmount;
            return DamageAmount;
        }

        public float Heal(float healAmount)
        {
            DamageAmount -= healAmount;
            if (DamageAmount < 0)
            {
                DamageAmount = 0;
            }
            return DamageAmount;
        }
    }
}