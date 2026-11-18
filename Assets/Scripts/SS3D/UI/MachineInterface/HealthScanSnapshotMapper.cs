using SS3D.Systems.Health;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Turns a scan into what the VSU-5R screen draws, following the "Vitals Scan Result Spec" design
    /// page. Pool danger lines sit on the simulation's own critical thresholds (<see cref="HealthConstants"/>),
    /// so the scanner flags what the body treats as critical; warnings start at the design's 15%.
    /// </summary>
    public static class HealthScanSnapshotMapper
    {
        /// <summary>Deviation from normal at which a pool reads as a warning (0–1).</summary>
        public const float WarningDeviation = 0.15f;

        /// <summary>Organ function at or above this reads healthy (the design's line).</summary>
        public const float OrganHealthyPercent = 85f;

        /// <summary>Organ function below this reads failing (the design's line).</summary>
        public const float OrganFailingPercent = 40f;

        /// <summary>Zone damage that draws the zone at full colour.</summary>
        public const float FullScaleZoneDamage = 100f;

        /// <summary>Zone damage at which the bone layer starts to show.</summary>
        public const float BoneShowsFrom = 40f;

        /// <summary>Full scale of the shock bar, past the critical line so it stays visible.</summary>
        public const float ShockBarMax = HealthConstants.ShockCriticalThreshold * 1.2f;

        /// <summary>Blood lost per minute, in percent of full volume, per unit of summed bleed rate.</summary>
        public const float BleedPercentPerMinutePerRate = HealthConstants.BleedingBloodDrainScale * 60f * 100f;

        private const float BoneFullAfter = 45f;

        public static readonly string[] ZoneLabels = { "Head", "Chest", "L Arm", "R Arm", "L Leg", "R Leg", "Groin" };

        public static readonly string[] OrganLabels = { "Brain", "Heart", "Lung L", "Lung R", "Liver" };

        public static readonly Color ZoneBase = Hex(0x55555c);
        public static readonly Color DisabledFill = Hex(0x050506);
        public static readonly Color SeveredFill = Hex(0x2a2a2e);
        private static readonly Color BruteLow = Hex(0xb13d3d);
        private static readonly Color BruteHigh = Hex(0xd17a7a);
        private static readonly Color BurnLow = Hex(0xe6ad53);
        private static readonly Color BurnHigh = Hex(0xf0c47d);
        private static readonly Color Healthy = Hex(0x7fbf6b);
        private static readonly Color Impaired = Hex(0xe6ad53);
        private static readonly Color Failing = Hex(0xc25b5b);
        private static readonly Color TextPrimary = Hex(0xe8e6e3);
        private static readonly Color TextSecondary = Hex(0x9a9690);

        public static HealthScanViewModel ToViewModel(HealthScanSnapshot snapshot)
        {
            string title = string.IsNullOrEmpty(snapshot.DeviceTitle) ? HealthScanViewModel.DefaultDeviceTitle : snapshot.DeviceTitle;
            string subject = string.IsNullOrWhiteSpace(snapshot.SubjectName) ? "Unknown Subject" : snapshot.SubjectName;
            var model = new HealthScanViewModel
            {
                ModelLabel = string.IsNullOrEmpty(snapshot.ModelLabel) ? HealthScanViewModel.DefaultModelLabel : snapshot.ModelLabel,
                DeviceTitle = title,
                TitleText = $"Vitals Scan — {subject}",
                SubText = snapshot.Live ? "Live Scan · Full Body" : "Out of Range · Last Reading",
                FooterText = $"SS3D Vitals Scan Unit — Model {title}",
            };

            MapZones(model, snapshot);
            MapOrgans(model, snapshot);
            MapSystemic(model, snapshot);
            MapShock(model, snapshot);
            MapRhythm(model, snapshot);
            ApplyStatus(model, snapshot);
            return model;
        }

        /// <summary>Paint colour for a tone where USS classes can't reach (Painter2D, tints).</summary>
        public static Color ToneColor(StatusTone tone)
        {
            return tone switch
            {
                StatusTone.Success => Healthy,
                StatusTone.Warning => Impaired,
                StatusTone.Danger => Failing,
                StatusTone.Neutral => TextSecondary,
                _ => TextPrimary,
            };
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
            if (percent < OrganFailingPercent)
            {
                return StatusTone.Danger;
            }

            return percent < OrganHealthyPercent ? StatusTone.Warning : StatusTone.Success;
        }

        public static StatusTone TierTone(WoundSeverity tier)
        {
            return tier switch
            {
                WoundSeverity.Severed or WoundSeverity.Disabled or WoundSeverity.Severe => StatusTone.Danger,
                WoundSeverity.Wound => StatusTone.Warning,
                _ => StatusTone.Neutral,
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
            float opacity = Mathf.Min(1f, 0.35f + Mathf.Clamp01(severity / FullScaleZoneDamage) * 0.65f);
            float brightness = Mathf.Clamp01(severity / 80f);
            Color target = burnLeads
                ? Color.Lerp(BurnLow, BurnHigh, brightness)
                : Color.Lerp(BruteLow, BruteHigh, brightness);
            return Color.Lerp(ZoneBase, target, opacity);
        }

        private static void MapZones(HealthScanViewModel model, in HealthScanSnapshot snapshot)
        {
            for (int i = 0; i < HealthConstants.ZoneCount; i++)
            {
                float brute = Read(snapshot.ZoneBrute, i);
                float burn = Read(snapshot.ZoneBurn, i);
                float severity = Mathf.Max(brute, burn);
                HealthScanZoneFlags flags = snapshot.ZoneFlags != null && i < snapshot.ZoneFlags.Length
                    ? snapshot.ZoneFlags[i]
                    : HealthScanZoneFlags.None;
                WoundSeverity tier = snapshot.ZoneSeverity != null && i < snapshot.ZoneSeverity.Length
                    ? snapshot.ZoneSeverity[i]
                    : WoundSeverity.None;
                if ((flags & HealthScanZoneFlags.Severed) != 0)
                {
                    tier = WoundSeverity.Severed;
                }
                else if ((flags & HealthScanZoneFlags.Disabled) != 0 && tier < WoundSeverity.Disabled)
                {
                    tier = WoundSeverity.Disabled;
                }
                else if (tier == WoundSeverity.None && severity > 0f)
                {
                    tier = WoundSeverity.Bruised;
                }

                bool bleeding = (flags & HealthScanZoneFlags.Bleeding) != 0;
                var zone = new HealthScanZoneView
                {
                    Label = ZoneLabels[i],
                    Tier = tier,
                    TierLabel = tier == WoundSeverity.None ? "Uninjured" : tier.ToString(),
                    TierTone = TierTone(tier),
                    Fill = ZoneFill(brute, burn),
                    Glow = Color.clear,
                    BoneOpacity = Mathf.Clamp01((severity - BoneShowsFrom) / BoneFullAfter),
                    Opacity = 1f,
                    NumbersText = $"BRUTE {Mathf.RoundToInt(brute)}  BURN {Mathf.RoundToInt(burn)}",
                    FlagsText = FlagsText(flags),
                    Bleeding = bleeding,
                    Injured = tier != WoundSeverity.None || bleeding,
                };

                if (severity > 0f)
                {
                    Color glow = burn >= brute ? BurnLow : BruteLow;
                    glow.a = Mathf.Lerp(0.25f, 0.7f, Mathf.Clamp01(severity / FullScaleZoneDamage));
                    zone.Glow = glow;
                }

                if (tier == WoundSeverity.Severed)
                {
                    // Limb gone: only a ghost of where it was.
                    zone.Fill = SeveredFill;
                    zone.Glow = Color.clear;
                    zone.BoneOpacity = 0f;
                    zone.Opacity = 0.35f;
                }
                else if (tier == WoundSeverity.Disabled)
                {
                    // Dead to the body: near-black, with a bright red rim so it reads on the dark screen.
                    zone.Fill = DisabledFill;
                    zone.Glow = BruteHigh;
                }

                model.Zones[i] = zone;
                if (zone.Injured)
                {
                    model.WoundZones.Add(i);
                }
            }
        }

        private static string FlagsText(HealthScanZoneFlags flags)
        {
            string text = string.Empty;
            if ((flags & HealthScanZoneFlags.Bleeding) != 0)
            {
                text = "BLEEDING";
            }

            if ((flags & HealthScanZoneFlags.Splinted) != 0)
            {
                text += text.Length > 0 ? " · SPLINTED" : "SPLINTED";
            }

            return text;
        }

        private static void MapOrgans(HealthScanViewModel model, in HealthScanSnapshot snapshot)
        {
            for (int i = 0; i < HealthScanSnapshotBuilder.OrganCount; i++)
            {
                float percent = snapshot.OrganFunction != null && i < snapshot.OrganFunction.Length
                    ? snapshot.OrganFunction[i]
                    : 100f;
                model.Organs[i] = BuildOrgan(OrganLabels[i], percent, $"Function {Mathf.RoundToInt(Mathf.Max(0f, percent))}%");
            }

            HealthScanOrganView left = model.Organs[(int)OrganType.LeftLung];
            HealthScanOrganView right = model.Organs[(int)OrganType.RightLung];
            model.Lungs = BuildOrgan("Lungs", Mathf.Min(left.Percent, right.Percent), $"Left {left.PercentText} · Right {right.PercentText}");
        }

        private static HealthScanOrganView BuildOrgan(string label, float percent, string detail)
        {
            StatusTone tone = OrganTone(percent);
            return new HealthScanOrganView
            {
                Label = label,
                Percent = percent,
                PercentText = $"{Mathf.RoundToInt(Mathf.Max(0f, percent))}%",
                StatusWord = tone switch
                {
                    StatusTone.Danger => "Failing",
                    StatusTone.Warning => "Impaired",
                    _ => "Healthy",
                },
                Detail = detail,
                Tone = tone,
                Fill = ToneColor(tone),
            };
        }

        private static void MapSystemic(HealthScanViewModel model, in HealthScanSnapshot snapshot)
        {
            float blood = Mathf.Clamp01(snapshot.BloodVolumeRatio);
            model.BloodFill = blood;
            model.BloodText = $"{Mathf.RoundToInt(blood * 100f)}%";
            model.BloodTone = PoolTone(1f - blood, 1f - HealthConstants.CriticalBloodVolumeRatio);

            float bleedPerMinute = Mathf.Max(0f, snapshot.TotalBleedingRate) * BleedPercentPerMinutePerRate;
            model.BleedText = bleedPerMinute > 0f ? $"−{bleedPerMinute:0}%/MIN" : "NONE";
            model.BleedTone = snapshot.TotalBleedingRate >= 1f ? StatusTone.Danger
                : snapshot.TotalBleedingRate > 0f ? StatusTone.Warning
                : StatusTone.Neutral;

            float oxy = Mathf.Clamp01(snapshot.OxyDebt);
            model.SpO2Text = $"{Mathf.RoundToInt((1f - oxy) * 100f)}%";
            model.SpO2Tone = PoolTone(oxy, HealthConstants.CriticalOxyDebt);

            float toxin = Mathf.Clamp01(snapshot.Toxin);
            model.ToxinText = $"{Mathf.RoundToInt(toxin * 100f)}%";
            model.ToxinFraction = toxin;
            model.ToxinTone = PoolTone(toxin, HealthConstants.CriticalToxinConcentration);
        }

        private static void MapShock(HealthScanViewModel model, in HealthScanSnapshot snapshot)
        {
            float shock = Mathf.Max(0f, snapshot.Shock);
            float incoming = Mathf.Max(0f, snapshot.IncomingPain);
            model.ShockFraction = Mathf.Clamp01(shock / ShockBarMax);
            model.IncomingFraction = Mathf.Clamp(incoming / ShockBarMax, 0f, 1f - model.ShockFraction);
            model.StandMarker = HealthConstants.ShockStandThreshold / ShockBarMax;
            model.DownedMarker = HealthConstants.ShockDownedThreshold / ShockBarMax;
            model.CriticalMarker = HealthConstants.ShockCriticalThreshold / ShockBarMax;
            int pending = Mathf.RoundToInt(incoming);
            model.ShockText = Mathf.RoundToInt(shock) + (pending > 0 ? $"  (+{pending} incoming)" : string.Empty);
            model.ShockTone = shock >= HealthConstants.ShockCriticalThreshold ? StatusTone.Danger
                : shock >= HealthConstants.ShockDownedThreshold ? StatusTone.Warning
                : StatusTone.Success;
        }

        private static void MapRhythm(HealthScanViewModel model, in HealthScanSnapshot snapshot)
        {
            float heart = snapshot.OrganFunction != null && snapshot.OrganFunction.Length > (int)OrganType.Heart
                ? snapshot.OrganFunction[(int)OrganType.Heart]
                : 100f;

            if (snapshot.State == HealthState.Dead || snapshot.IsCardiacArrest)
            {
                model.Rhythm = HealthScanRhythm.Arrest;
                model.RhythmText = snapshot.State == HealthState.Dead ? "NO OUTPUT" : "ARREST — NO OUTPUT";
                model.RhythmTone = StatusTone.Danger;
            }
            else if (heart < OrganFailingPercent || snapshot.State == HealthState.Critical)
            {
                model.Rhythm = HealthScanRhythm.Weak;
                model.RhythmText = "WEAK";
                model.RhythmTone = StatusTone.Danger;
            }
            else if (snapshot.Shock >= HealthConstants.ShockStandThreshold
                || snapshot.BloodVolumeRatio < 1f - WarningDeviation
                || snapshot.OxyDebt >= WarningDeviation)
            {
                model.Rhythm = HealthScanRhythm.Rapid;
                model.RhythmText = "RAPID";
                model.RhythmTone = StatusTone.Warning;
            }
            else
            {
                model.Rhythm = HealthScanRhythm.Steady;
                model.RhythmText = "STEADY";
                model.RhythmTone = StatusTone.Success;
            }
        }

        private static void ApplyStatus(HealthScanViewModel model, in HealthScanSnapshot snapshot)
        {
            string consciousness = snapshot.IsConscious ? "Conscious" : "Unconscious";
            (model.StatusTone, model.StatusText) = snapshot.State switch
            {
                HealthState.Dead => (StatusTone.Danger, "Deceased — No Vital Signs"),
                HealthState.CardiacArrest => (StatusTone.Danger, "Cardiac Arrest — Defib Window"),
                HealthState.Critical => (StatusTone.Danger, $"Critical — {consciousness}"),
                HealthState.Downed => (StatusTone.Warning, $"Downed — {consciousness}"),
                _ => model.WoundZones.Count > 0
                    ? (StatusTone.Warning, $"Injured — {consciousness}")
                    : (StatusTone.Success, "Stable"),
            };
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
