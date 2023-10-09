using System;

namespace SS3D.Systems.Health
{
    public struct BodyDamageInfo
    {
        public DamageType InjuryType;
        public float Quantity;
        public float Suceptibility;
        public float Resistance;

        public BodyDamageInfo(DamageType damageType, float quantity = 0, float suceptibility = 1, float resistance = 0)
        {
            InjuryType = damageType;
            Quantity = quantity;
            Suceptibility = suceptibility;
            Resistance = resistance;
        }
        
        public static BodyDamageInfo operator +(BodyDamageInfo a, float b)
        {
            float quantity = Math.Max(a.Quantity + Math.Max(b * a.Suceptibility - a.Resistance, 0), 0);
            return new(a.InjuryType, quantity, a.Suceptibility, a.Resistance);
        }
        public static BodyDamageInfo operator -(BodyDamageInfo a, float b)
        {
            float quantity = Math.Max(a.Quantity - Math.Max(b * a.Suceptibility - a.Resistance, 0), 0);
            return new(a.InjuryType, quantity, a.Suceptibility, a.Resistance);
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