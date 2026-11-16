using System;
using SS3D.Systems.Health;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Melee timing and damage profile. Interim lethality — retune after armor (combat plan Phase 5).
    /// </summary>
    [Serializable]
    public struct MeleeWeaponProfile
    {
        public float BruteDamage;
        public float BurnDamage;
        public float WindupSeconds;
        public float RecoverySeconds;
        public float StaminaCost;
        public bool CanSever;

        /// <summary>
        /// Force applied to structural turf on connect. Prefabs serialized before this field existed
        /// may store 0 — use <see cref="ResolveStructuralForce"/>.
        /// </summary>
        public float StructuralForce;

        /// <summary>
        /// Hostile exhaustion in absolute stamina points on a living hit, clamped to the target's current stamina.
        /// </summary>
        public float StaminaDamage;

        /// <summary>
        /// Seconds a living hit knocks the target down (combat.md §5); 0 never floors anyone. A contact stun:
        /// armor that blocks the taser's prongs does not stop it.
        /// </summary>
        public float KnockdownSeconds;

        public MeleeDamagePacket ToDamagePacket() => new(BruteDamage, BurnDamage, CanSever);

        /// <summary>
        /// Explicit StructuralForce when set; otherwise a provisional fraction of brute so old prefabs still hit walls.
        /// </summary>
        public float ResolveStructuralForce()
        {
            if (StructuralForce > 0f)
            {
                return StructuralForce;
            }

            return BruteDamage * 0.75f;
        }

        public static MeleeWeaponProfile Fists => new()
        {
            BruteDamage = 8f,
            BurnDamage = 0f,
            WindupSeconds = 0.25f,
            RecoverySeconds = 0.55f,
            StaminaCost = 8f,
            CanSever = false,
            StructuralForce = 5f,
        };

        /// <summary>Low-base fallback for any held item without a dedicated profile.</summary>
        public static MeleeWeaponProfile Improvised => new()
        {
            BruteDamage = 6f,
            BurnDamage = 0f,
            WindupSeconds = 0.3f,
            RecoverySeconds = 1.2f,
            StaminaCost = 10f,
            CanSever = false,
            StructuralForce = 8f,
        };

        public static MeleeWeaponProfile Crowbar => new()
        {
            BruteDamage = 18f,
            BurnDamage = 0f,
            WindupSeconds = 0.35f,
            RecoverySeconds = 0.8f,
            StaminaCost = 12f,
            CanSever = false,
            StructuralForce = 35f,
        };

        public static MeleeWeaponProfile Hatchet => new()
        {
            BruteDamage = 16f,
            BurnDamage = 0f,
            WindupSeconds = 0.3f,
            RecoverySeconds = 0.7f,
            StaminaCost = 11f,
            CanSever = true,
            StructuralForce = 28f,
        };

        public static MeleeWeaponProfile KitchenKnife => new()
        {
            BruteDamage = 12f,
            BurnDamage = 0f,
            WindupSeconds = 0.2f,
            RecoverySeconds = 0.55f,
            StaminaCost = 9f,
            CanSever = true,
            StructuralForce = 4f,
        };

        /// <summary>
        /// Security's non-lethal option: light brute, a stamina drain and the taser's knockdown on every
        /// living hit. Always on (no cell); first-guess balance, to tune from playtests.
        /// </summary>
        public static MeleeWeaponProfile StunBaton => new()
        {
            BruteDamage = 5f,
            BurnDamage = 0f,
            WindupSeconds = 0.3f,
            RecoverySeconds = 0.9f,
            StaminaCost = 10f,
            CanSever = false,
            StructuralForce = 6f,
            StaminaDamage = 5f,
            KnockdownSeconds = RangedWeaponProfile.TaserKnockdownSeconds,
        };
    }
}
