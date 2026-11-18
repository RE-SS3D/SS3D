using SS3D.Systems.Health;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>One body zone on the scan doll.</summary>
    public struct HealthScanZoneView
    {
        public string Label;

        public Color Fill;

        /// <summary>0–1: the bone layer shows through at high brute or burn.</summary>
        public float BoneOpacity;

        public string Tooltip;

        public bool Severed;
    }

    /// <summary>One organ in the readout list and on the doll.</summary>
    public struct HealthScanOrganView
    {
        public string Label;

        public float Percent;

        public string PercentText;

        public string StatusWord;

        public Color Fill;

        public StatusTone Tone;

        public string Tooltip;
    }

    /// <summary>
    /// Everything the VSU-5R vitals screen draws, built by <see cref="HealthScanSnapshotMapper"/>.
    /// </summary>
    public sealed class HealthScanViewModel : IMachineInterfaceViewModel
    {
        public const string DefaultModelLabel = "VSU-5R · anatomical scan unit";

        public const string DefaultDeviceTitle = "VSU-5R";

        public string ModelLabel { get; set; } = DefaultModelLabel;

        public string DeviceTitle { get; set; } = DefaultDeviceTitle;

        public string SubjectLine { get; set; } = "Vitals Scan — Unknown Subject";

        public string ScanMetaText { get; set; } = "Full Body · Anatomical Layer Scan";

        public string FooterText { get; set; } = "SS3D Vitals Scan Unit — Model VSU-5R";

        public StatusTone StatusTone { get; set; } = StatusTone.Success;

        public string StatusBadgeText { get; set; } = "CLEAR";

        public string StatusHeadline { get; set; } = "No Anomalies Detected";

        public string StatusSubline { get; set; } = string.Empty;

        /// <summary>Indexed by <see cref="BodyZone"/>.</summary>
        public HealthScanZoneView[] Zones { get; set; } = new HealthScanZoneView[HealthConstants.ZoneCount];

        /// <summary>Indexed by <see cref="OrganType"/>.</summary>
        public HealthScanOrganView[] Organs { get; set; } = new HealthScanOrganView[HealthScanSnapshotBuilder.OrganCount];

        /// <summary>The two lungs share one drawing; it takes the worse lung.</summary>
        public HealthScanOrganView LungsOverlay { get; set; }

        public string BloodText { get; set; } = "100%";

        /// <summary>0–1 fill of the blood vial.</summary>
        public float BloodFill { get; set; } = 1f;

        public StatusTone BloodTone { get; set; } = StatusTone.Neutral;

        public string ToxinText { get; set; } = "0%";

        /// <summary>Needle angle in degrees, −90 (empty) to 90 (full scale).</summary>
        public float ToxinNeedleDegrees { get; set; } = -90f;

        public StatusTone ToxinTone { get; set; } = StatusTone.Neutral;

        public string SpO2Text { get; set; } = "100%";

        public StatusTone SpO2Tone { get; set; } = StatusTone.Neutral;

        public string ShockText { get; set; } = "0";

        public StatusTone ShockTone { get; set; } = StatusTone.Neutral;

        public string HeartRhythmText { get; set; } = "Sinus rhythm";

        /// <summary>Arrest or death: the trace runs flat.</summary>
        public bool NoCardiacOutput { get; set; }

        public static HealthScanViewModel CreatePreview(bool injured)
        {
            var zones = new float[HealthConstants.ZoneCount];
            var burns = new float[HealthConstants.ZoneCount];
            var flags = new HealthScanZoneFlags[HealthConstants.ZoneCount];
            var organs = new[] { 100f, 100f, 100f, 100f, 100f };
            if (injured)
            {
                zones[(int)BodyZone.Chest] = 78f;
                zones[(int)BodyZone.LeftArm] = 100f;
                burns[(int)BodyZone.RightLeg] = 32f;
                flags[(int)BodyZone.Chest] = HealthScanZoneFlags.Bleeding;
                flags[(int)BodyZone.LeftArm] = HealthScanZoneFlags.Disabled | HealthScanZoneFlags.Bleeding;
                organs[(int)OrganType.LeftLung] = 30f;
                organs[(int)OrganType.Heart] = 72f;
            }

            return HealthScanSnapshotMapper.ToViewModel(new HealthScanSnapshot
            {
                ModelLabel = DefaultModelLabel,
                DeviceTitle = DefaultDeviceTitle,
                SubjectName = "Subject 04",
                State = injured ? HealthState.Critical : HealthState.Healthy,
                IsConscious = true,
                CriticalFlags = injured ? HealthCriticalFlags.LowBlood | HealthCriticalFlags.HighOxyDebt : HealthCriticalFlags.None,
                BloodVolumeRatio = injured ? 0.38f : 1f,
                OxyDebt = injured ? 0.58f : 0f,
                Toxin = 0f,
                Shock = injured ? 44f : 0f,
                ZoneBrute = zones,
                ZoneBurn = burns,
                ZoneFlags = flags,
                OrganFunction = organs,
            });
        }
    }
}
