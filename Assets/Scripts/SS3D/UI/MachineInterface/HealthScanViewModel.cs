using SS3D.Systems.Health;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>The pulse trace's shape (the design's four rhythms).</summary>
    public enum HealthScanRhythm
    {
        Steady = 0,
        Rapid = 1,
        Weak = 2,
        Arrest = 3,
    }

    /// <summary>One body zone on the scan doll, and its wound-list row when hurt.</summary>
    public struct HealthScanZoneView
    {
        public string Label;

        /// <summary>Uninjured, Bruised, Wound, Severe, Disabled or Severed.</summary>
        public string TierLabel;

        public WoundSeverity Tier;

        public StatusTone TierTone;

        public Color Fill;

        /// <summary>Soft glow round the zone, alpha 0 for none (UITK has no filters; drawn as an outline tint).</summary>
        public Color Glow;

        /// <summary>0–1: the bone layer shows through at high brute or burn.</summary>
        public float BoneOpacity;

        /// <summary>1 normally; a severed limb is only a ghost of where it was.</summary>
        public float Opacity;

        public string NumbersText;

        /// <summary>"Bleeding · Splinted", or empty.</summary>
        public string FlagsText;

        public bool Bleeding;

        public bool Injured;
    }

    /// <summary>One organ in the readout and on the doll.</summary>
    public struct HealthScanOrganView
    {
        public string Label;

        public float Percent;

        public string PercentText;

        /// <summary>Healthy, Impaired or Failing.</summary>
        public string StatusWord;

        /// <summary>The second line of the hover card ("Function 58% · weak").</summary>
        public string Detail;

        public Color Fill;

        public StatusTone Tone;
    }

    /// <summary>
    /// Everything the VSU-5R vitals screen draws, built by <see cref="HealthScanSnapshotMapper"/>.
    /// Layout follows the "Vitals Scan Result Spec" design page.
    /// </summary>
    public sealed class HealthScanViewModel : IMachineInterfaceViewModel
    {
        public const string DefaultModelLabel = "VSU-5R · anatomical scan unit";

        public const string DefaultDeviceTitle = "VSU-5R";

        public string Title { get; set; } = "VITALS SCAN · MEDICAL";

        public string ModelLabel { get; set; } = DefaultModelLabel;

        public string DeviceTitle { get; set; } = DefaultDeviceTitle;

        public string TitleText { get; set; } = "Vitals Scan — Unknown Subject";

        public string SubText { get; set; } = "Live Scan · Full Body";

        public string FooterText { get; set; } = "SS3D Vitals Scan Unit — Model VSU-5R";

        public StatusTone StatusTone { get; set; } = StatusTone.Success;

        public string StatusText { get; set; } = "Stable";

        /// <summary>Indexed by <see cref="BodyZone"/>.</summary>
        public HealthScanZoneView[] Zones { get; set; } = new HealthScanZoneView[HealthConstants.ZoneCount];

        /// <summary>Indexed by <see cref="OrganType"/>.</summary>
        public HealthScanOrganView[] Organs { get; set; } = new HealthScanOrganView[HealthScanSnapshotBuilder.OrganCount];

        /// <summary>The two lungs share one drawing: it takes the worse lung, and its card lists both.</summary>
        public HealthScanOrganView Lungs { get; set; }

        /// <summary>Zones in <see cref="BodyZone"/> order that carry any injury, for the wound list.</summary>
        public List<int> WoundZones { get; set; } = new();

        public string BloodText { get; set; } = "100%";

        /// <summary>0–1 fill of the blood vial.</summary>
        public float BloodFill { get; set; } = 1f;

        public StatusTone BloodTone { get; set; } = StatusTone.Neutral;

        public string BleedText { get; set; } = "NONE";

        public StatusTone BleedTone { get; set; } = StatusTone.Neutral;

        public string ToxinText { get; set; } = "0%";

        /// <summary>0–1 of the dial.</summary>
        public float ToxinFraction { get; set; }

        public StatusTone ToxinTone { get; set; } = StatusTone.Neutral;

        public string SpO2Text { get; set; } = "100%";

        public StatusTone SpO2Tone { get; set; } = StatusTone.Neutral;

        public HealthScanRhythm Rhythm { get; set; } = HealthScanRhythm.Steady;

        public string RhythmText { get; set; } = "STEADY";

        public StatusTone RhythmTone { get; set; } = StatusTone.Success;

        public string ShockText { get; set; } = "0%";

        public StatusTone ShockTone { get; set; } = StatusTone.Success;

        /// <summary>Shock and the pain still building, each 0–1 of the bar.</summary>
        public float ShockFraction { get; set; }

        public float IncomingFraction { get; set; }

        /// <summary>Where the stand, downed and critical lines sit on the bar, 0–1.</summary>
        public float StandMarker { get; set; }

        public float DownedMarker { get; set; }

        public float CriticalMarker { get; set; }

        public static HealthScanViewModel CreatePreview(bool injured)
        {
            var brute = new float[HealthConstants.ZoneCount];
            var burn = new float[HealthConstants.ZoneCount];
            var flags = new HealthScanZoneFlags[HealthConstants.ZoneCount];
            var tiers = new WoundSeverity[HealthConstants.ZoneCount];
            var organs = new[] { 100f, 100f, 100f, 100f, 100f };
            if (injured)
            {
                brute[(int)BodyZone.Chest] = 78f;
                tiers[(int)BodyZone.Chest] = WoundSeverity.Severe;
                flags[(int)BodyZone.Chest] = HealthScanZoneFlags.Bleeding;
                brute[(int)BodyZone.LeftArm] = 100f;
                tiers[(int)BodyZone.LeftArm] = WoundSeverity.Disabled;
                flags[(int)BodyZone.LeftArm] = HealthScanZoneFlags.Disabled | HealthScanZoneFlags.Bleeding;
                burn[(int)BodyZone.RightLeg] = 30f;
                tiers[(int)BodyZone.RightLeg] = WoundSeverity.Wound;
                organs[(int)OrganType.Heart] = 58f;
                organs[(int)OrganType.LeftLung] = 30f;
                organs[(int)OrganType.RightLung] = 44f;
                organs[(int)OrganType.Brain] = 81f;
            }

            return HealthScanSnapshotMapper.ToViewModel(new HealthScanSnapshot
            {
                ModelLabel = DefaultModelLabel,
                DeviceTitle = DefaultDeviceTitle,
                SubjectName = "Subject 04",
                State = injured ? HealthState.Critical : HealthState.Healthy,
                IsConscious = !injured,
                CriticalFlags = injured ? HealthCriticalFlags.LowBlood | HealthCriticalFlags.HighOxyDebt : HealthCriticalFlags.None,
                BloodVolumeRatio = injured ? 0.46f : 1f,
                OxyDebt = injured ? 0.58f : 0f,
                Shock = injured ? 91f : 0f,
                TotalBleedingRate = injured ? 2.5f : 0f,
                ZoneBrute = brute,
                ZoneBurn = burn,
                ZoneFlags = flags,
                ZoneSeverity = tiers,
                OrganFunction = organs,
            });
        }
    }
}
