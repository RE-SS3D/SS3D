using System;

namespace SS3D.Systems.Combat
{
    [Serializable]
    public struct MeleeWeaponProfile
    {
        public float BruteDamage;
        public float BurnDamage;
        public float WindupSeconds;
        public float RecoverySeconds;

        public MeleeDamagePacket ToDamagePacket() => new(BruteDamage, BurnDamage);

        public static MeleeWeaponProfile Fists => new()
        {
            BruteDamage = 8f,
            BurnDamage = 0f,
            WindupSeconds = 0.25f,
            RecoverySeconds = 0.35f,
        };

        public static MeleeWeaponProfile Crowbar => new()
        {
            BruteDamage = 18f,
            BurnDamage = 0f,
            WindupSeconds = 0.35f,
            RecoverySeconds = 0.5f,
        };
    }
}
