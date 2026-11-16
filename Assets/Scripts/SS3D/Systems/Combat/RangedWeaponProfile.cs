using System;
using SS3D.Systems.Health;
using SS3D.Systems.Inventory.Containers;

namespace SS3D.Systems.Combat
{
    /// <summary>Which animation set a wielded gun uses. Serialized on prefabs: append, never reorder.</summary>
    public enum WeaponHoldStyle : byte
    {
        Rifle = 0,
        Pistol = 1,
    }

    /// <summary>
    /// Which gunshot and reload sounds a ballistic gun plays (<see cref="CombatAudioTrackIds.SoundsFor"/>).
    /// Energy weapons sound by <see cref="RangedVisualStyle"/> instead. Serialized on prefabs: append, never reorder.
    /// </summary>
    public enum FirearmSoundSet : byte
    {
        Rifle = 0,
        Smg = 1,
        Pistol = 2,
        ServicePistol = 3,
    }

    /// <summary>
    /// Hitscan firearm profile. Accuracy cone is weapon-intrinsic and readable via reticle bloom.
    /// Mag refill is timed reload (no loose ammo items this pass).
    /// </summary>
    [Serializable]
    public struct RangedWeaponProfile
    {
        /// <summary>Client presentation; Ballistic preserves existing firearms.</summary>
        public RangedVisualStyle VisualStyle;

        public float BruteDamage;
        public float BurnDamage;

        /// <summary>
        /// Hostile exhaustion in absolute stamina points (a default body holds StaminaFactory.DefaultMax = 10),
        /// clamped to available stamina so it cannot create oxy debt.
        /// </summary>
        public float StaminaDamage;

        /// <summary>Seconds a living hit knocks the target down (combat.md §5); 0 never floors anyone.</summary>
        public float KnockdownSeconds;

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

        /// <summary>
        /// Body pose while wielded. Presentation only: <see cref="RequiresBothHands"/> stays the gameplay
        /// rule, and the pose is never inferred from it.
        /// </summary>
        public WeaponHoldStyle HoldStyle;

        /// <summary>Gunshot and reload sounds for a ballistic gun; ignored by energy weapons.</summary>
        public FirearmSoundSet SoundSet;

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
        /// <summary>
        /// Operative SMG: a bigger magazine and faster cadence than the M4 for lighter hits and a wider cone.
        /// First-guess balance, to tune from playtests.
        /// </summary>
        public static RangedWeaponProfile C20R
        {
            get
            {
                RangedWeaponProfile profile = M4;
                profile.BruteDamage = 14f;
                profile.BaseSpreadDegrees = 0.5f;
                profile.RecoilClimbDegrees = 0.25f;
                profile.FalloffStartMeters = 15f;
                profile.FalloffEndMeters = 35f;
                profile.FalloffExtraSpreadDegrees = 2f;
                profile.MaxRangeMeters = 35f;
                profile.FireCooldownSeconds = 0.1f;
                profile.MagazineSize = 24;
                profile.StructuralForce = 9f;
                profile.SoundSet = FirearmSoundSet.Smg;
                return profile;
            }
        }

        /// <summary>Operative sidearm: one-handed, hits harder per round than the security pistol.</summary>
        public static RangedWeaponProfile Stechkin => Pistol(18f, 8, 0.3f, FirearmSoundSet.Pistol);

        /// <summary>Security sidearm: one-handed, the crew's standard lethal option before the armory M4.</summary>
        public static RangedWeaponProfile SecurityPistol => Pistol(15f, 10, 0.25f, FirearmSoundSet.ServicePistol);

        public const float PistolReloadSeconds = 1.6f;

        private static RangedWeaponProfile Pistol(float brute, int capacity, float cooldown, FirearmSoundSet sounds)
        {
            RangedWeaponProfile profile = M4;
            profile.BruteDamage = brute;
            profile.BaseSpreadDegrees = 0.45f;
            profile.RecoilClimbDegrees = 0.35f;
            profile.RecoilDecayPerSecond = 5f;
            profile.FalloffStartMeters = 12f;
            profile.FalloffEndMeters = 30f;
            profile.FalloffExtraSpreadDegrees = 2f;
            profile.MaxRangeMeters = 30f;
            profile.FireCooldownSeconds = cooldown;
            profile.MagazineSize = capacity;
            // Pistols have no reload clip (the Pistol pack only kicks the forearms), so this is a plain timer.
            profile.ReloadSeconds = PistolReloadSeconds;
            profile.StructuralForce = 8f;
            profile.StaminaCost = 2f;
            profile.RequiresBothHands = false;
            profile.HoldStyle = WeaponHoldStyle.Pistol;
            profile.SoundSet = sounds;
            return profile;
        }

        /// <summary>Short-range fuel pulses; hold primary to sustain the jet. Provisional balance.</summary>
        public static RangedWeaponProfile Flamethrower
        {
            get
            {
                RangedWeaponProfile profile = Energy(RangedVisualStyle.Flame, 4f, 0f, 60, 0.16f, NapalmSprayRules.Range);
                profile.StructuralForce = 0f;
                profile.BaseSpreadDegrees = NapalmSprayRules.HalfAngle;
                profile.RecoilClimbDegrees = 0f;
                profile.RecoilPerShot = 0f;
                profile.MovementBloomPerSpeed = 0f;
                profile.FalloffExtraSpreadDegrees = 0f;
                profile.ExhaustionSpreadDegrees = 0f;
                profile.StaminaCost = 0.15f;
                profile.ReloadSeconds = 4f;
                return profile;
            }
        }

        public static RangedWeaponProfile LaserRifle => Energy(RangedVisualStyle.Laser, 24f, 0f, 20, 0.3f, 40f);
        public static RangedWeaponProfile IonRifle => Energy(RangedVisualStyle.Ion, 16f, 0f, 12, 0.6f, 30f);
        public static RangedWeaponProfile Taser
        {
            get
            {
                RangedWeaponProfile profile = Energy(RangedVisualStyle.Taser, 0f, 6.5f, 3, 0.75f, 12f);
                profile.KnockdownSeconds = TaserKnockdownSeconds;
                profile.ReloadSeconds = TaserRechargeSeconds;
                return profile;
            }
        }

        // First-guess taser balance, to tune from playtests: every unarmored hit floors the target, so the
        // limit is charges. Three covers one engagement with a miss or a second target, then a long recharge.

        /// <summary>A stun baton's knockdown (combat.md §5: roughly 2–3 s), delivered at range.</summary>
        public const float TaserKnockdownSeconds = 2.5f;

        public const float TaserRechargeSeconds = 9f;

        /// <summary>Share of a stun's stamina drain that still lands through armor that blocks the knockdown.</summary>
        public const float StunBlockedStaminaFactor = 0.5f;

        private static RangedWeaponProfile Energy(RangedVisualStyle style, float burn, float stamina,
            int capacity, float cooldown, float range)
        {
            RangedWeaponProfile profile = M4;
            profile.VisualStyle = style;
            profile.BruteDamage = 0f;
            profile.BurnDamage = burn;
            profile.StaminaDamage = stamina;
            profile.StructuralForce = style == RangedVisualStyle.Taser ? 0f : 6f;
            profile.MagazineSize = capacity;
            profile.FireCooldownSeconds = cooldown;
            profile.MaxRangeMeters = range;
            profile.FalloffStartMeters = range * 0.5f;
            profile.FalloffEndMeters = range;
            profile.RecoilPerShot = 0.3f;
            profile.StaminaCost = 2f;
            profile.RequiresBothHands = style != RangedVisualStyle.Taser;
            profile.HoldStyle = style == RangedVisualStyle.Taser ? WeaponHoldStyle.Pistol : WeaponHoldStyle.Rifle;
            return profile;
        }
    }
}
