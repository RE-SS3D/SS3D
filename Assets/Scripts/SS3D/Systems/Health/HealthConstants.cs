namespace SS3D.Systems.Health
{
    public static class HealthConstants
    {
        public const int ZoneCount = 7;

        public const float TickIntervalSeconds = 1f;

        // Systemic thresholds (normalized 0–1 unless noted).
        public const float CriticalBloodVolumeRatio = 0.40f;
        public const float CriticalOxyDebt = 0.75f;
        public const float CriticalToxinConcentration = 0.75f;
        public const float CriticalBrainFunctionPercent = 30f;

        public const float ConsciousnessBrainFunctionPercent = 10f;

        // Pool dynamics per 1 Hz tick.
        // Tuning target: untreated Disabled-tier bleed (~1.5 rate) should show rising oxy debt
        // around 60–70% blood remaining; brain death ~90–150 s after a catastrophic head wound
        // (100 brute → Disabled + ~40 brain damage) without treatment.
        public const float BleedingBloodDrainScale = 0.021f;
        public const float LowBloodOxyDebtGainScale = 0.065f;
        public const float BaseOxygenDemand = 0.012f;
        public const float BloodDeliveryVolumeExponent = 1.75f;
        public const float BaseToxinIntake = 0f;
        public const float LiverClearanceRate = 0.02f;
        public const float RenalClearanceFactor = 0.5f;

        // Wound severity thresholds (brute damage per zone).
        public const float BruisedThreshold = 10f;
        public const float WoundThreshold = 25f;
        public const float SevereThreshold = 50f;
        public const float DisabledThreshold = 75f;
        // Wound severity thresholds (burn damage per zone).
        public const float BurnWoundThreshold = 25f;
        public const float BurnSevereThreshold = 50f;
        public const float BurnDisabledThreshold = 75f;

        // Organ damage mapping (zone hits → stored organ function loss).
        public const float HeadBruteToBrainDamageScale = 0.4f;
        public const float HeadBurnToBrainDamageScale = 0.25f;
        public const float ChestBruteToHeartDamageScale = 0.25f;
        public const float ChestBruteToLungDamageScale = 0.2f;
        public const float ChestBruteToLiverDamageScale = 0.15f;

        // Organ tick dynamics (1 Hz).
        public const float CardiacArrestBrainDrainPerTick = 4f;
        public const float CriticalOxyBrainDrainPerTick = 2f;
        public const float CriticalOxyHeartDrainPerTick = 1.5f;
        public const float CriticalBloodHeartDrainPerTick = 1f;
        public const float CriticalToxinHeartDrainPerTick = 0.5f;
        public const float OrganPerfusionBloodFloor = 0.25f;
        public const float CriticalOrganFunctionPercent = 30f;

        // Defibrillator (charge/battery deferred to Phase 7d).
        public const float DefibrillatorHeartRestorePercent = 60f;
        public const float DefibrillatorMisshockBurnDamage = 25f;

        // Limb capability multipliers.
        public const float LimbDisabledMovementMultiplier = 0.35f;
        public const float LimbSevereMovementMultiplier = 0.6f;
        public const float LimbWoundMovementMultiplier = 0.85f;
    }
}
