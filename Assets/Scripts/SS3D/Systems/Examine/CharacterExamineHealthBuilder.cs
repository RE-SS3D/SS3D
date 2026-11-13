using System;
using System.Collections.Generic;
using System.Text;
using SS3D.Localization;
using SS3D.Systems.Health;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Builds qualitative health examine lines from a character's synced health state.
    /// Tier 0 (self and others): state + one consolidated appearance sentence (I vs he).
    /// Tier 1 (self only): short first-person feel lines. Quiet when healthy.
    /// </summary>
    public static class CharacterExamineHealthBuilder
    {
        public const int MaxFeelLines = 4;

        /// <summary>Soft thresholds for first-person feel lines (below organ "healthy" readout).</summary>
        public const float FeelBrainDizzyPercent = 80f;
        public const float FeelBrainFadingPercent = 40f;
        public const float FeelBloodWeakRatio = 0.85f;
        public const float FeelOxyBreathless = 0.25f;
        public const float FeelToxinNauseous = 0.25f;
        public const float FeelOrganHurtPercent = 80f;
        public const float FeelOrganSeverePercent = 50f;

        private readonly struct AppearanceKeys
        {
            public string HurtAndBleeding { get; }
            public string Bleeding { get; }
            public string Hurt { get; }
            public string Missing { get; }
            public string MissingHurtBleeding { get; }
            public string MissingBleeding { get; }
            public string MissingHurt { get; }

            public string HurtAndBleedingFallback { get; }
            public string BleedingFallback { get; }
            public string HurtFallback { get; }
            public string MissingFallback { get; }
            public string MissingHurtBleedingFallback { get; }
            public string MissingBleedingFallback { get; }
            public string MissingHurtFallback { get; }

            public AppearanceKeys(
                string hurtAndBleeding,
                string bleeding,
                string hurt,
                string missing,
                string missingHurtBleeding,
                string missingBleeding,
                string missingHurt,
                string hurtAndBleedingFallback,
                string bleedingFallback,
                string hurtFallback,
                string missingFallback,
                string missingHurtBleedingFallback,
                string missingBleedingFallback,
                string missingHurtFallback)
            {
                HurtAndBleeding = hurtAndBleeding;
                Bleeding = bleeding;
                Hurt = hurt;
                Missing = missing;
                MissingHurtBleeding = missingHurtBleeding;
                MissingBleeding = missingBleeding;
                MissingHurt = missingHurt;
                HurtAndBleedingFallback = hurtAndBleedingFallback;
                BleedingFallback = bleedingFallback;
                HurtFallback = hurtFallback;
                MissingFallback = missingFallback;
                MissingHurtBleedingFallback = missingHurtBleedingFallback;
                MissingBleedingFallback = missingBleedingFallback;
                MissingHurtFallback = missingHurtFallback;
            }

            public static AppearanceKeys Self { get; } = new(
                ExamineHealthKeys.SelfHurtAndBleeding,
                ExamineHealthKeys.SelfBleeding,
                ExamineHealthKeys.SelfHurt,
                ExamineHealthKeys.SelfMissing,
                ExamineHealthKeys.SelfMissingAndHurtAndBleeding,
                ExamineHealthKeys.SelfMissingAndBleeding,
                ExamineHealthKeys.SelfMissingAndHurt,
                ExamineHealthKeys.SelfHurtAndBleedingFallback,
                ExamineHealthKeys.SelfBleedingFallback,
                ExamineHealthKeys.SelfHurtFallback,
                ExamineHealthKeys.SelfMissingFallback,
                ExamineHealthKeys.SelfMissingAndHurtAndBleedingFallback,
                ExamineHealthKeys.SelfMissingAndBleedingFallback,
                ExamineHealthKeys.SelfMissingAndHurtFallback);

            public static AppearanceKeys Other { get; } = new(
                ExamineHealthKeys.OtherHurtAndBleeding,
                ExamineHealthKeys.OtherBleeding,
                ExamineHealthKeys.OtherHurt,
                ExamineHealthKeys.OtherMissing,
                ExamineHealthKeys.OtherMissingAndHurtAndBleeding,
                ExamineHealthKeys.OtherMissingAndBleeding,
                ExamineHealthKeys.OtherMissingAndHurt,
                ExamineHealthKeys.OtherHurtAndBleedingFallback,
                ExamineHealthKeys.OtherBleedingFallback,
                ExamineHealthKeys.OtherHurtFallback,
                ExamineHealthKeys.OtherMissingFallback,
                ExamineHealthKeys.OtherMissingAndHurtAndBleedingFallback,
                ExamineHealthKeys.OtherMissingAndBleedingFallback,
                ExamineHealthKeys.OtherMissingAndHurtFallback);
        }

        public static void AppendSections(
            HumanHealthController health,
            bool includeSelfDetail,
            List<ExamineSection> sections)
        {
            if (health == null || sections == null)
            {
                return;
            }

            AppendSections(health.Snapshot, health.DebugDetail, includeSelfDetail, sections);
        }

        public static void AppendSections(
            HealthSnapshot snapshot,
            HealthDebugDetail detail,
            bool includeSelfDetail,
            List<ExamineSection> sections)
        {
            if (sections == null)
            {
                return;
            }

            if (snapshot.State == HealthState.Dead)
            {
                sections.Add(new ExamineSection(Localized(
                    includeSelfDetail ? ExamineHealthKeys.DeadSelf : ExamineHealthKeys.DeadOther,
                    null,
                    includeSelfDetail
                        ? ExamineHealthKeys.DeadSelfFallback
                        : ExamineHealthKeys.DeadOtherFallback)));
                return;
            }

            AppendStateLine(snapshot, includeSelfDetail, sections);

            string appearance = BuildAppearanceLine(
                snapshot,
                detail,
                includeSelfDetail ? AppearanceKeys.Self : AppearanceKeys.Other);
            if (!string.IsNullOrEmpty(appearance))
            {
                sections.Add(new ExamineSection(appearance));
            }

            if (includeSelfDetail)
            {
                AppendFeelLines(snapshot, detail, sections);
            }
        }

        private static void AppendStateLine(HealthSnapshot snapshot, bool isSelf, List<ExamineSection> sections)
        {
            if (snapshot.State == HealthState.CardiacArrest || snapshot.IsCardiacArrest)
            {
                sections.Add(new ExamineSection(Localized(
                    isSelf ? ExamineHealthKeys.CardiacArrestSelf : ExamineHealthKeys.CardiacArrestOther,
                    null,
                    isSelf
                        ? ExamineHealthKeys.CardiacArrestSelfFallback
                        : ExamineHealthKeys.CardiacArrestOtherFallback)));
                return;
            }

            if (snapshot.State == HealthState.Critical)
            {
                sections.Add(new ExamineSection(Localized(
                    isSelf ? ExamineHealthKeys.CriticalSelf : ExamineHealthKeys.CriticalOther,
                    null,
                    isSelf
                        ? ExamineHealthKeys.CriticalSelfFallback
                        : ExamineHealthKeys.CriticalOtherFallback)));
                return;
            }

            if (!snapshot.IsConscious)
            {
                sections.Add(new ExamineSection(Localized(
                    isSelf ? ExamineHealthKeys.UnconsciousSelf : ExamineHealthKeys.UnconsciousOther,
                    null,
                    isSelf
                        ? ExamineHealthKeys.UnconsciousSelfFallback
                        : ExamineHealthKeys.UnconsciousOtherFallback)));
                return;
            }

            // health.md §7 onlooker table: Downed, then high shock while still standing.
            if (snapshot.State == HealthState.Downed)
            {
                sections.Add(new ExamineSection(Localized(
                    isSelf ? ExamineHealthKeys.DownedSelf : ExamineHealthKeys.DownedOther,
                    null,
                    isSelf ? ExamineHealthKeys.DownedSelfFallback : ExamineHealthKeys.DownedOtherFallback)));
                return;
            }

            if (snapshot.Shock >= HealthConstants.ShockWarningThreshold)
            {
                sections.Add(new ExamineSection(Localized(
                    isSelf ? ExamineHealthKeys.ShockSelf : ExamineHealthKeys.ShockOther,
                    null,
                    isSelf ? ExamineHealthKeys.ShockSelfFallback : ExamineHealthKeys.ShockOtherFallback)));
            }
        }

        private static string BuildAppearanceLine(
            HealthSnapshot snapshot,
            HealthDebugDetail detail,
            AppearanceKeys keys)
        {
            List<string> bleedingZones = new(HealthConstants.ZoneCount);
            List<string> missingZones = new(HealthConstants.ZoneCount);
            bool hurt = false;

            for (int i = 0; i < HealthConstants.ZoneCount; i++)
            {
                BodyZone zone = (BodyZone)i;
                if (snapshot.IsZoneSevered(zone))
                {
                    missingZones.Add(ZoneNameLower(zone));
                    continue;
                }

                if (snapshot.IsZoneBleeding(zone))
                {
                    bleedingZones.Add(ZoneNameLower(zone));
                }

                if (detail.GetZone(zone).Severity >= WoundSeverity.Wound)
                {
                    hurt = true;
                }
            }

            bool missing = missingZones.Count > 0;
            bool bleeding = bleedingZones.Count > 0;
            if (!missing && !hurt && !bleeding)
            {
                return null;
            }

            string missingList = FormatZoneList(missingZones);
            string bleedingList = FormatZoneList(bleedingZones);

            if (missing && hurt && bleeding)
            {
                return Localized(
                    keys.MissingHurtBleeding,
                    new object[] { missingList, bleedingList },
                    string.Format(keys.MissingHurtBleedingFallback, missingList, bleedingList));
            }

            if (missing && bleeding)
            {
                return Localized(
                    keys.MissingBleeding,
                    new object[] { missingList, bleedingList },
                    string.Format(keys.MissingBleedingFallback, missingList, bleedingList));
            }

            if (missing && hurt)
            {
                return Localized(
                    keys.MissingHurt,
                    new object[] { missingList },
                    string.Format(keys.MissingHurtFallback, missingList));
            }

            if (missing)
            {
                return Localized(
                    keys.Missing,
                    new object[] { missingList },
                    string.Format(keys.MissingFallback, missingList));
            }

            if (hurt && bleeding)
            {
                return Localized(
                    keys.HurtAndBleeding,
                    new object[] { bleedingList },
                    string.Format(keys.HurtAndBleedingFallback, bleedingList));
            }

            if (bleeding)
            {
                return Localized(
                    keys.Bleeding,
                    new object[] { bleedingList },
                    string.Format(keys.BleedingFallback, bleedingList));
            }

            return Localized(keys.Hurt, null, keys.HurtFallback);
        }

        private static void AppendFeelLines(
            HealthSnapshot snapshot,
            HealthDebugDetail detail,
            List<ExamineSection> sections)
        {
            List<string> feel = new(MaxFeelLines);

            float brain = snapshot.BrainFunctionPercent;
            if (brain < FeelBrainFadingPercent)
            {
                TryAddFeel(feel, ExamineHealthKeys.FeelFading, ExamineHealthKeys.FeelFadingFallback);
            }
            else if (brain < FeelBrainDizzyPercent)
            {
                TryAddFeel(feel, ExamineHealthKeys.FeelDizzy, ExamineHealthKeys.FeelDizzyFallback);
            }

            if (snapshot.Pools.BloodVolumeRatio < FeelBloodWeakRatio)
            {
                TryAddFeel(feel, ExamineHealthKeys.FeelWeak, ExamineHealthKeys.FeelWeakFallback);
            }

            float lungs = Math.Min(
                detail.GetOrgan(OrganType.LeftLung).FunctionPercent,
                detail.GetOrgan(OrganType.RightLung).FunctionPercent);
            if (snapshot.Pools.OxyDebt >= FeelOxyBreathless || lungs < FeelOrganSeverePercent)
            {
                TryAddFeel(feel, ExamineHealthKeys.FeelBreathless, ExamineHealthKeys.FeelBreathlessFallback);
            }
            else if (lungs < FeelOrganHurtPercent)
            {
                TryAddFeel(feel, ExamineHealthKeys.FeelBreathHurts, ExamineHealthKeys.FeelBreathHurtsFallback);
            }

            if (snapshot.Pools.ToxinConcentration >= FeelToxinNauseous
                || detail.GetOrgan(OrganType.Liver).FunctionPercent < FeelOrganHurtPercent)
            {
                if (snapshot.Pools.ToxinConcentration >= FeelToxinNauseous)
                {
                    TryAddFeel(feel, ExamineHealthKeys.FeelNauseous, ExamineHealthKeys.FeelNauseousFallback);
                }
                else
                {
                    TryAddFeel(feel, ExamineHealthKeys.FeelSideAches, ExamineHealthKeys.FeelSideAchesFallback);
                }
            }

            if (snapshot.HeartFunctionPercent < FeelOrganHurtPercent)
            {
                TryAddFeel(feel, ExamineHealthKeys.FeelHeartTight, ExamineHealthKeys.FeelHeartTightFallback);
            }

            for (int i = 0; i < HealthConstants.ZoneCount && feel.Count < MaxFeelLines; i++)
            {
                BodyZone zone = (BodyZone)i;
                ZoneDamageState state = detail.GetZone(zone);
                if (state.IsSevered || !state.IsDisabled)
                {
                    continue;
                }

                if (zone is not (BodyZone.LeftArm or BodyZone.RightArm or BodyZone.LeftLeg or BodyZone.RightLeg))
                {
                    continue;
                }

                string zoneName = ZoneNameLower(zone);
                TryAddFeel(
                    feel,
                    ExamineHealthKeys.FeelLimbUseless,
                    string.Format(ExamineHealthKeys.FeelLimbUselessFallback, zoneName),
                    new object[] { zoneName });
            }

            foreach (string line in feel)
            {
                sections.Add(new ExamineSection(line));
            }
        }

        private static void TryAddFeel(List<string> feel, string key, string fallback, object[] args = null)
        {
            if (feel.Count >= MaxFeelLines)
            {
                return;
            }

            feel.Add(Localized(key, args, fallback));
        }

        private static string FormatZoneList(List<string> zones)
        {
            if (zones == null || zones.Count == 0)
            {
                return string.Empty;
            }

            if (zones.Count == 1)
            {
                return zones[0];
            }

            if (zones.Count == 2)
            {
                return $"{zones[0]} and {zones[1]}";
            }

            StringBuilder builder = new();
            for (int i = 0; i < zones.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(i == zones.Count - 1 ? ", and " : ", ");
                }

                builder.Append(zones[i]);
            }

            return builder.ToString();
        }

        private static string Localized(string key, object[] args, string fallback)
        {
            string result = LocalizedTextService.GetFormattedString(
                ExamineCanonicalKeyGenerator.ExamineTableName,
                key,
                args,
                fallback);

            if (!string.IsNullOrEmpty(result)
                && result.StartsWith("[MISSING:", StringComparison.Ordinal)
                && !string.IsNullOrEmpty(fallback))
            {
                return fallback;
            }

            return result;
        }

        private static string ZoneNameLower(BodyZone zone)
        {
            return zone switch
            {
                BodyZone.Head => "head",
                BodyZone.Chest => "chest",
                BodyZone.LeftArm => "left arm",
                BodyZone.RightArm => "right arm",
                BodyZone.LeftLeg => "left leg",
                BodyZone.RightLeg => "right leg",
                BodyZone.Groin => "groin",
                _ => "body",
            };
        }
    }
}
