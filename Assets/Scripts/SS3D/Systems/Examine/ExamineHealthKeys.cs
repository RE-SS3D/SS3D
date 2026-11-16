namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Localization keys for character health examine lines (Tier 0 public / Tier 1 self-feel).
    /// </summary>
    public static class ExamineHealthKeys
    {
        public const string DeadSelf = "examine.health.dead.self";
        public const string DeadOther = "examine.health.dead.other";
        public const string CardiacArrestSelf = "examine.health.cardiac_arrest.self";
        public const string CardiacArrestOther = "examine.health.cardiac_arrest.other";
        public const string CriticalSelf = "examine.health.critical.self";
        public const string CriticalOther = "examine.health.critical.other";
        public const string UnconsciousSelf = "examine.health.unconscious.self";
        public const string UnconsciousOther = "examine.health.unconscious.other";
        public const string DownedSelf = "examine.health.downed.self";
        public const string DownedOther = "examine.health.downed.other";
        public const string ShockSelf = "examine.health.shock.self";
        public const string ShockOther = "examine.health.shock.other";

        // Tier 0 appearance — shared shape, self vs other copy.
        public const string SelfHurtAndBleeding = "examine.health.self.hurt_and_bleeding";
        public const string SelfBleeding = "examine.health.self.bleeding";
        public const string SelfHurt = "examine.health.self.hurt";
        public const string SelfMissing = "examine.health.self.missing";
        public const string SelfMissingAndHurtAndBleeding = "examine.health.self.missing_hurt_bleeding";
        public const string SelfMissingAndBleeding = "examine.health.self.missing_bleeding";
        public const string SelfMissingAndHurt = "examine.health.self.missing_hurt";

        public const string OtherHurtAndBleeding = "examine.health.other.hurt_and_bleeding";
        public const string OtherBleeding = "examine.health.other.bleeding";
        public const string OtherHurt = "examine.health.other.hurt";
        public const string OtherMissing = "examine.health.other.missing";
        public const string OtherMissingAndHurtAndBleeding = "examine.health.other.missing_hurt_bleeding";
        public const string OtherMissingAndBleeding = "examine.health.other.missing_bleeding";
        public const string OtherMissingAndHurt = "examine.health.other.missing_hurt";

        // Tier 1 — first-person feel lines (self only).
        public const string FeelDizzy = "examine.health.feel.dizzy";
        public const string FeelFading = "examine.health.feel.fading";
        public const string FeelWeak = "examine.health.feel.weak";
        public const string FeelBreathless = "examine.health.feel.breathless";
        public const string FeelNauseous = "examine.health.feel.nauseous";
        public const string FeelHeartTight = "examine.health.feel.heart_tight";
        public const string FeelBreathHurts = "examine.health.feel.breath_hurts";
        public const string FeelSideAches = "examine.health.feel.side_aches";
        public const string FeelLimbUseless = "examine.health.feel.limb_useless";

        public const string DeadSelfFallback = "I am <k=danger>dead</k>.";
        public const string DeadOtherFallback = "He is <k=danger>dead</k>.";
        public const string CardiacArrestSelfFallback = "My heart has <k=danger>stopped</k>.";
        public const string CardiacArrestOtherFallback = "He is in <k=danger>cardiac arrest</k>.";
        public const string CriticalSelfFallback = "I feel myself <k=danger>slipping away</k>.";
        public const string CriticalOtherFallback = "He is in <k=danger>critical condition</k>.";
        public const string UnconsciousSelfFallback = "Everything goes dark.";
        public const string UnconsciousOtherFallback = "He is unconscious.";
        public const string DownedSelfFallback = "I am <k=warn>down</k>, and everything hurts.";
        public const string DownedOtherFallback = "He is <k=warn>down</k>, conscious and in pain.";
        public const string ShockSelfFallback = "I am <k=warn>shaking</k> and feel faint.";
        public const string ShockOtherFallback = "He is <k=warn>pale and shaking</k>.";

        public const string SelfHurtAndBleedingFallback = "I am <k=warn>hurt</k> and <k=danger>bleeding</k> from the {0}.";
        public const string SelfBleedingFallback = "I am <k=danger>bleeding</k> from the {0}.";
        public const string SelfHurtFallback = "I am <k=warn>hurt</k>.";
        public const string SelfMissingFallback = "I am <k=danger>missing</k> my {0}.";
        public const string SelfMissingAndHurtAndBleedingFallback =
            "I am <k=danger>missing</k> my {0} and am <k=warn>hurt</k> and <k=danger>bleeding</k> from the {1}.";
        public const string SelfMissingAndBleedingFallback =
            "I am <k=danger>missing</k> my {0} and am <k=danger>bleeding</k> from the {1}.";
        public const string SelfMissingAndHurtFallback =
            "I am <k=danger>missing</k> my {0} and am <k=warn>hurt</k>.";

        // Interim masculine pronouns — roster is male-only for now.
        public const string OtherHurtAndBleedingFallback = "He seems to be <k=warn>hurt</k> and <k=danger>bleeding</k> from the {0}.";
        public const string OtherBleedingFallback = "He seems to be <k=danger>bleeding</k> from the {0}.";
        public const string OtherHurtFallback = "He seems to be <k=warn>hurt</k>.";
        public const string OtherMissingFallback = "He is <k=danger>missing</k> his {0}.";
        public const string OtherMissingAndHurtAndBleedingFallback =
            "He is <k=danger>missing</k> his {0} and seems to be <k=warn>hurt</k> and <k=danger>bleeding</k> from the {1}.";
        public const string OtherMissingAndBleedingFallback =
            "He is <k=danger>missing</k> his {0} and seems to be <k=danger>bleeding</k> from the {1}.";
        public const string OtherMissingAndHurtFallback =
            "He is <k=danger>missing</k> his {0} and seems to be <k=warn>hurt</k>.";

        public const string FeelDizzyFallback = "I feel dizzy.";
        public const string FeelFadingFallback = "I can barely stay conscious.";
        public const string FeelWeakFallback = "I feel weak and lightheaded.";
        public const string FeelBreathlessFallback = "I can't catch my breath.";
        public const string FeelNauseousFallback = "I feel nauseous.";
        public const string FeelHeartTightFallback = "My chest feels tight.";
        public const string FeelBreathHurtsFallback = "Every breath hurts.";
        public const string FeelSideAchesFallback = "My side aches.";
        public const string FeelLimbUselessFallback = "I can't move my {0}.";
    }
}
