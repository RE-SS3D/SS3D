using Enums;

namespace Player.Body
{
    /// <summary>
    /// Class to store and add/remove damage caused to a bodypart
    /// </summary>
    public class BodyPartDamage
    {
        public BodyPartDamage(DamageType damageType, float damageAmount)
        {
            this.damageType = damageType;
            this.damageAmount = damageAmount;
        }

        private readonly DamageType damageType;
        private float damageAmount;

        public DamageType DamageType => damageType;

        public float DamageAmount => damageAmount;

        public float Damage(float damageAmount)
        {
            this.damageAmount += damageAmount;
            return this.damageAmount;
        }

        public float Heal(float healAmount)
        {
            damageAmount -= healAmount;
            if (damageAmount < 0)
            {
                damageAmount = 0;
            }
            return damageAmount;
        }
    }
}