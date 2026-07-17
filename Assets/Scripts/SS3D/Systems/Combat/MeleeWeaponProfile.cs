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
        public bool CanSever;

        public MeleeDamagePacket ToDamagePacket() => new(BruteDamage, BurnDamage, CanSever);

        public static MeleeWeaponProfile Fists => new()
        {
            BruteDamage = 8f,
            BurnDamage = 0f,
            WindupSeconds = 0.25f,
            RecoverySeconds = 0.35f,
            CanSever = false,
        };

        public static MeleeWeaponProfile Crowbar => new()
        {
            BruteDamage = 18f,
            BurnDamage = 0f,
            WindupSeconds = 0.35f,
            RecoverySeconds = 0.5f,
            CanSever = false,
        };

        public static MeleeWeaponProfile Hatchet => new()
        {
            BruteDamage = 16f,
            BurnDamage = 0f,
            WindupSeconds = 0.3f,
            RecoverySeconds = 0.45f,
            CanSever = true,
        };

        public static MeleeWeaponProfile KitchenKnife => new()
        {
            BruteDamage = 12f,
            BurnDamage = 0f,
            WindupSeconds = 0.2f,
            RecoverySeconds = 0.35f,
            CanSever = true,
        };
    }
}
