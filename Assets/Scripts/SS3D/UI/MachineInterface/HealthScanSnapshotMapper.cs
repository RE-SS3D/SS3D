using SS3D.Systems.Health;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Turns a scan into what the VSU-5R screen draws. Danger lines sit on the simulation's own
    /// critical thresholds (<see cref="HealthConstants"/>), so the scanner flags what the body treats
    /// as critical; warnings start earlier, at the design's 15% deviation.
    /// </summary>
    public static class HealthScanSnapshotMapper
    {
        /// <summary>Deviation from normal at which a pool or organ reads as a warning (0–1).</summary>
        public const float WarningDeviation = 0.15f;

        /// <summary>Zone damage that draws the zone at full colour.</summary>
        public const float FullScaleZoneDamage = 100f;

        /// <summary>Zone damage at which the bone layer starts to show.</summary>
        public const float BoneShowsFrom = 40f;

        private const float BoneFullAfter = 45f;

        public static readonly string[] ZoneLabels = { "Head", "Chest", "L Arm", "R Arm", "L Leg", "R Leg", "Groin" };

        public static readonly string[] OrganLabels = { "Brain", "Heart", "L Lung", "R Lung", "Liver" };

        private static readonly Color ZoneBase = Hex(0x3a3a3f);
        private static readonly Color BruteLow = Hex(0xb13d3d);
        private static readonly Color BruteHigh = Hex(0xd17a7a);
        private static readonly Color BurnLow = Hex(0xe6ad53);
        private static readonly Color BurnHigh = Hex(0xf0c47d);
        private static readonly Color OrganOk = Hex(0x7fbf6b);
        private static readonly Color OrganImpaired = Hex(0xe6ad53);
        private static readonly Color OrganCritical = Hex(0xc25b5b);
        private static readonly Color TextPrimary = Hex(0xe8e6e3);
        private static readonly Color TextSecondary = Hex(0x9a9690);

        public static HealthScanViewModel ToViewModel(HealthScanSnapshot snapshot)
        {
            string title = string.IsNullOrEmpty(snapshot.DeviceTitle) ? HealthScanViewModel.DefaultDeviceTitle : snapshot.DeviceTitle;
            var model = new HealthScanViewModel
            {
                ModelLabel = string.IsNullOrEmpty(snapshot.ModelLabel) ? HealthScanViewModel.DefaultModelLabel : snapshot.ModelLabel,
                DeviceTitle = title,
                SubjectLine = $"Vitals Scan — {(string.IsNullOrWhiteSpace(snapshot.SubjectName) ? "Unknown Subject" : snapshot.SubjectName)}",
                FooterText = $"SS3D Vitals Scan Unit — Model {title}",
            };

            bool anyInjury = MapZones(model, snapshot);
            bool organWarning = MapOrgans(model, snapshot);
            bool systemicWarning = MapSystemic(model, snapshot);
            ApplyStatus(model, snapshot, anyInjury || organWarning || systemicWarning);
            return model;
        }

        public static StatusTone PoolTone(float deviation, float criticalDeviation)
        {
            if (deviation >= criticalDeviation)
            {
                return StatusTone.Danger;
            }

            return deviation >= WarningDeviation ? StatusTone.Warning : StatusTone.Neutral;
        }

        public static StatusTone OrganTone(float percent)
        {
            if (percent < HealthConstants.CriticalOrganFunctionPercent)
            {
                return StatusTone.Danger;
            }

            return percent < (1f - WarningDeviation) * 100f ? StatusTone.Warning : StatusTone.Success;
        }

        /// <summary>Paint colour for a tone where USS classes can't reach (Painter2D, tints).</summary>
        public static Color ToneColor(StatusTone tone)
        {
            return tone switch
            {
                StatusTone.Success => OrganOk,
                StatusTone.Warning => OrganImpaired,
                StatusTone.Danger => OrganCritical,
                StatusTone.Neutral => TextSecondary,
                _ => TextPrimary,
            };
        }

        /// <summary>The design's zone fill: grey, blending to red (brute) or amber (burn) as damage rises.</summary>
        public static Color ZoneFill(float brute, float burn)
        {
            float severity = Mathf.Max(brute, burn);
            if (severity <= 0f)
            {
                return ZoneBase;
            }

            bool burnLeads = burn >= brute;
            float t = Mathf.Clamp01(severity / FullScaleZoneDamage);
            float opacity = Mathf.Min(1f, 0.35f + t * 0.65f);
            Color target = burnLeads
                ? Color.Lerp(BurnLow, BurnHigh, Mathf.Clamp01(severity / 80f))
                : Color.Lerp(BruteLow, BruteHigh, Mathf.Clamp01(severity / 80f));
            return Color.Lerp(ZoneBase, target, opacity);
        }

        private static bool MapZones(HealthScanViewModel model, in HealthScanSnapshot snapshot)
        {
            bool anyInjury = false;
            for (int i = 0; i < HealthConstants.ZoneCount; i++)
            {
                float brute = Read(snapshot.ZoneBrute, i);
                float burn = Read(snapshot.ZoneBurn, i);
                HealthScanZoneFlags flags = snapshot.ZoneFlags != null && i < snapshot.ZoneFlags.Length
                    ? snapshot.ZoneFlags[i]
                    : HealthScanZoneFlags.None;
                bool severed = (flags & HealthScanZoneFlags.Severed) != 0;
                anyInjury |= brute > 0f || burn > 0f || flags != HealthScanZoneFlags.None;
                float severity = Mathf.Max(brute, burn);

                model.Zones[i] = new HealthScanZoneView
                {
                    Label = ZoneLabels[i],
                    Fill = ZoneFill(brute, burn),
                    BoneOpacity = severed ? 0f : Mathf.Clamp01((severity - BoneShowsFrom) / BoneFullAfter),
                    Severed = severed,
                    Tooltip = ZoneTooltip(ZoneLabels[i], brute, burn, flags),
                };
            }

            return anyInjury;
        }

        private static string ZoneTooltip(string label, float brute, float burn, HealthScanZoneFlags flags)
        {
            if ((flags & HealthScanZoneFlags.Severed) != 0)
            {
                return $"{label.ToUpperInvariant()}  [SEVERED]";
            }

            string text = $"{label.ToUpperInvariant()}  BRUTE {Mathf.RoundToInt(brute)}  BURN {Mathf.RoundToInt(burn)}";
            if ((flags & HealthScanZoneFlags.Bleeding) != 0)
            {
                text += "  [BLEEDING]";
            }

            if ((flags & HealthScanZoneFlags.Disabled) != 0)
            {
                text += (flags & HealthScanZoneFlags.Splinted) != 0 ? "  [SPLINTED]" : "  [DISABLED]";
            }

            return text;
        }

        private static bool MapOrgans(HealthScanViewModel model, in HealthScanSnapshot snapshot)
        {
            bool warning = false;
            for (int i = 0; i < HealthScanSnapshotBuilder.OrganCount; i++)
            {
                model.Organs[i] = BuildOrgan(OrganLabels[i], snapshot.OrganFunction != null && i < snapshot.OrganFunction.Length
                    ? snapshot.OrganFunction[i]
                    : 100f);
                warning |= model.Organs[i].Tone != StatusTone.Success;
            }

            HealthScanOrganView left = model.Organs[(int)OrganType.LeftLung];
            HealthScanOrganView right = model.Organs[(int)OrganType.RightLung];
            HealthScanOrganView worse = left.Percent <= right.Percent ? left : right;
            worse.Label = "Lungs";
            worse.Tooltip = $"LUNGS  L {left.PercentText}  R {right.PercentText}";
            model.LungsOverlay = worse;
            return warning;
        }

        private static HealthScanOrganView BuildOrgan(string label, float percent)
        {
            StatusTone tone = OrganTone(percent);
            string word = tone switch
            {
                StatusTone.Danger => "CRITICAL",
                StatusTone.Warning => "IMPAIRED",
                _ => "HEALTHY",
            };
            string percentText = $"{Mathf.RoundToInt(Mathf.Max(0f, percent))}%";
            return new HealthScanOrganView
            {
                Label = label,
                Percent = percent,
                PercentText = percentText,
                StatusWord = word,
                Tone = tone,
                Fill = tone switch
                {
                    StatusTone.Danger => OrganCritical,
                    StatusTone.Warning => OrganImpaired,
                    _ => OrganOk,
                },
                Tooltip = $"{label.ToUpperInvariant()}  {word}  {percentText}",
            };
        }

        private static bool MapSystemic(HealthScanViewModel model, in HealthScanSnapshot snapshot)
        {
            float blood = Mathf.Clamp01(snapshot.BloodVolumeRatio);
            model.BloodFill = blood;
            model.BloodText = $"{Mathf.RoundToInt(blood * 100f)}%";
            model.BloodTone = PoolTone(1f - blood, 1f - HealthConstants.CriticalBloodVolumeRatio);

            float oxy = Mathf.Clamp01(snapshot.OxyDebt);
            model.SpO2Text = $"{Mathf.RoundToInt((1f - oxy) * 100f)}%";
            model.SpO2Tone = PoolTone(oxy, HealthConstants.CriticalOxyDebt);

            float toxin = Mathf.Clamp01(snapshot.Toxin);
            model.ToxinText = $"{Mathf.RoundToInt(toxin * 100f)}%";
            model.ToxinNeedleDegrees = -90f + toxin * 180f;
            model.ToxinTone = PoolTone(toxin, HealthConstants.CriticalToxinConcentration);

            model.ShockText = Mathf.RoundToInt(Mathf.Max(0f, snapshot.Shock)).ToString();
            model.ShockTone = snapshot.Shock >= HealthConstants.ShockCriticalThreshold ? StatusTone.Danger
                : snapshot.Shock >= HealthConstants.ShockDownedThreshold ? StatusTone.Warning
                : StatusTone.Neutral;

            model.NoCardiacOutput = snapshot.State == HealthState.Dead || snapshot.IsCardiacArrest;
            model.HeartRhythmText = snapshot.State == HealthState.Dead ? "Asystole"
                : snapshot.IsCardiacArrest ? "Arrest — no output"
                : "Sinus rhythm";

            return model.BloodTone != StatusTone.Neutral
                || model.SpO2Tone != StatusTone.Neutral
                || model.ToxinTone != StatusTone.Neutral
                || model.ShockTone != StatusTone.Neutral;
        }

        private static void ApplyStatus(HealthScanViewModel model, in HealthScanSnapshot snapshot, bool anyFinding)
        {
            string consciousness = snapshot.IsConscious ? "Conscious" : "Unconscious";
            switch (snapshot.State)
            {
                case HealthState.Dead:
                    model.StatusTone = StatusTone.Danger;
                    model.StatusBadgeText = "DECEASED";
                    model.StatusHeadline = "No Vital Signs";
                    model.StatusSubline = "Brain function lost. Revival needs cloning.";
                    return;
                case HealthState.CardiacArrest:
                    model.StatusTone = StatusTone.Danger;
                    model.StatusBadgeText = "ARREST";
                    model.StatusHeadline = "Cardiac Arrest";
                    model.StatusSubline = "Heart stopped — defibrillate before the brain dies.";
                    return;
                case HealthState.Critical:
                    model.StatusTone = StatusTone.Danger;
                    model.StatusBadgeText = "CRITICAL";
                    model.StatusHeadline = "Critical — Multiple Systems Flagged";
                    model.StatusSubline = $"{consciousness} · {DescribeCritical(snapshot.CriticalFlags)}";
                    return;
                case HealthState.Downed:
                    model.StatusTone = StatusTone.Warning;
                    model.StatusBadgeText = "DOWNED";
                    model.StatusHeadline = "Downed — Cannot Stand";
                    model.StatusSubline = $"{consciousness} · shock {model.ShockText}";
                    return;
            }

            if (anyFinding)
            {
                model.StatusTone = StatusTone.Warning;
                model.StatusBadgeText = "INJURED";
                model.StatusHeadline = "Injuries Detected";
                model.StatusSubline = consciousness;
                return;
            }

            model.StatusTone = StatusTone.Success;
            model.StatusBadgeText = "CLEAR";
            model.StatusHeadline = "No Anomalies Detected";
            model.StatusSubline = consciousness;
        }

        private static string DescribeCritical(HealthCriticalFlags flags)
        {
            var parts = new List<string>();
            if ((flags & HealthCriticalFlags.LowBlood) != 0)
            {
                parts.Add("blood loss");
            }

            if ((flags & HealthCriticalFlags.HighOxyDebt) != 0)
            {
                parts.Add("hypoxia");
            }

            if ((flags & HealthCriticalFlags.HighToxin) != 0)
            {
                parts.Add("toxins");
            }

            if ((flags & HealthCriticalFlags.LowBrain) != 0)
            {
                parts.Add("brain damage");
            }

            if ((flags & HealthCriticalFlags.HighShock) != 0)
            {
                parts.Add("shock");
            }

            return parts.Count == 0 ? "vitals failing" : string.Join(", ", parts);
        }

        private static float Read(float[] values, int index)
        {
            return values != null && index < values.Length ? Mathf.Max(0f, values[index]) : 0f;
        }

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f, 1f);
        }
    }
}
