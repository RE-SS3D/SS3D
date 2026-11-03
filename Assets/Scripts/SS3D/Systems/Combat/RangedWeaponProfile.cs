using System;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Hitscan firearm profile. Accuracy cone is weapon-intrinsic and readable via reticle bloom.
    /// Mag refill is timed reload (no loose ammo items this pass).
    /// </summary>
    [Serializable]
    public struct RangedWeaponProfile
    {
        public float BruteDamage;
        public float BurnDamage;
        public bool CanSever;

        /// <summary>Base half-angle of the accuracy cone in degrees.</summary>
        public float BaseSpreadDegrees;

        /// <summary>Added spread degrees per stacked recoil unit.</summary>
        public float RecoilClimbDegrees;

        /// <summary>Recoil stacks added per shot.</summary>
        public float RecoilPerShot;

        /// <summary>Recoil stacks decayed per second while not firing.</summary>
        public float RecoilDecayPerSecond;

        /// <summary>Extra spread degrees per m/s of horizontal movement.</summary>
        public float MovementBloomPerSpeed;

        /// <summary>Distance (m) before range falloff begins widening the cone.</summary>
        public float FalloffStartMeters;

        /// <summary>Distance (m) at which range falloff reaches full extra spread.</summary>
        public float FalloffEndMeters;

        /// <summary>Extra spread degrees at FalloffEndMeters.</summary>
        public float FalloffExtraSpreadDegrees;

        /// <summary>Hitscan max range in meters.</summary>
        public float MaxRangeMeters;

        /// <summary>Minimum seconds between shots.</summary>
        public float FireCooldownSeconds;

        public int MagazineSize;
        public float ReloadSeconds;

        public float StructuralForce;

        /// <summary>Stamina cost per shot fired.</summary>
        public float StaminaCost;

        /// <summary>Extra spread degrees at full exhaustion (ExertionPenalty == 1).</summary>
        public float ExhaustionSpreadDegrees;

        /// <summary>
        /// When true, the gun may only occupy <see cref="RequiredHand"/> and reserves the other hand
        /// (cannot pick up / receive items there) while wielded.
        /// </summary>
        public bool RequiresBothHands;

        /// <summary>Primary grip hand for two-hand firearms (M4: Right).</summary>
        public HandSide RequiredHand;

        public MeleeDamagePacket ToDamagePacket() => new(BruteDamage, BurnDamage, CanSever);

        public float ResolveStructuralForce()
        {
            if (StructuralForce > 0f)
            {
                return StructuralForce;
            }

            return BruteDamage * 0.5f;
        }

        /// <summary>Assault rifle — tight standing cone at station ranges; bloom stays readable under move/recoil.</summary>
        public static RangedWeaponProfile M4 => new()
        {
            BruteDamage = 18f,
            BurnDamage = 0f,
            CanSever = false,
            BaseSpreadDegrees = 0.32f,
            RecoilClimbDegrees = 0.2f,
            RecoilPerShot = 1f,
            RecoilDecayPerSecond = 4f,
            MovementBloomPerSpeed = 0.2f,
            FalloffStartMeters = 22f,
            FalloffEndMeters = 50f,
            FalloffExtraSpreadDegrees = 1.4f,
            MaxRangeMeters = 50f,
            FireCooldownSeconds = 0.12f,
            MagazineSize = 30,
            ReloadSeconds = RifleReloadClipTiming.ReadySeconds,
            StructuralForce = 12f,
            StaminaCost = 3f,
            ExhaustionSpreadDegrees = 1.6f,
            RequiresBothHands = true,
            RequiredHand = HandSide.Right,
        };
    }
}
