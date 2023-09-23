using System;

namespace SS3D.Systems.Health
{
    public struct BodyDamageInfo
    {
        public DamageType InjuryType;
        public float Quantity;
        public float Suceptibility;
        public float Resistance;
        public float MaxDamage;

        public BodyDamageInfo(DamageType damageType, float quantity = 0, float suceptibility = 0, float resistance = 0, float maxDamage = 1000)
        {
            InjuryType = damageType;
            Quantity = quantity;
            Suceptibility = suceptibility;
            Resistance = resistance;
            MaxDamage = maxDamage;
        }
        
        public static BodyDamageInfo operator +(BodyDamageInfo a, float b)
        {
            float quantity = Math.Clamp(a.Quantity + Math.Max(b * a.Suceptibility - a.Resistance, 0), 0, a.MaxDamage);
            return new(a.InjuryType, quantity, a.Suceptibility, a.Resistance, a.MaxDamage);
        }
        public static BodyDamageInfo operator -(BodyDamageInfo a, float b)
        {
            float quantity = Math.Clamp(a.Quantity - Math.Max(b * a.Suceptibility - a.Resistance, 0), 0, a.MaxDamage);
            return new(a.InjuryType, quantity, a.Suceptibility, a.Resistance, a.MaxDamage);
        }

        public static BodyDamageInfo operator +(BodyDamageInfo a, BodyDamageInfo b)
        {
            return a + b.Quantity;
        }
        public static BodyDamageInfo operator -(BodyDamageInfo a, BodyDamageInfo b)
        {
            return a - b.Quantity;
        }
    }
}