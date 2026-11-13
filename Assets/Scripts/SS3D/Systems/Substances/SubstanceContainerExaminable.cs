using System.Collections.Generic;
using SS3D.Systems.Examine;
using UnityEngine;

namespace SS3D.Systems.Substances
{
    /// <summary>
    /// Free diegetic properties only: approximate volume, qualitative temperature, and visible color.
    /// Chemical identity is observer knowledge (chemistry gameplay) — not shown here. All lines are
    /// fine print (examine.md §2).
    /// </summary>
    public sealed class SubstanceContainerExaminable : SimpleExaminable, IExamineContentProvider
    {
        [SerializeField]
        private SubstanceContainer _container;

        private void Awake()
        {
            if (_container == null)
            {
                _container = GetComponent<SubstanceContainer>();
            }
        }

        public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
        {
            if (_container == null)
            {
                return;
            }

            if (_container.IsEmpty)
            {
                sections.Add(Line(ExamineSubstanceKeys.Empty, ExamineSubstanceKeys.EmptyFallback, null));
            }
            else
            {
                (string key, string fallback) = ApproximateVolume(_container.CurrentVolumeMl);
                sections.Add(Line(ExamineSubstanceKeys.Contains, ExamineSubstanceKeys.ContainsFallback, Word(key, fallback)));
            }

            (string tempKey, string tempFallback) = TemperatureBand(_container.TemperatureKelvin);
            sections.Add(Line(ExamineSubstanceKeys.Feels, ExamineSubstanceKeys.FeelsFallback, Word(tempKey, tempFallback)));

            if (!_container.IsEmpty)
            {
                (string colorKey, string colorFallback) = DescribeColor(_container.GetBlendColor());
                sections.Add(Line(ExamineSubstanceKeys.Looks, ExamineSubstanceKeys.LooksFallback, Word(colorKey, colorFallback)));
            }
        }

        private static string Word(string key, string fallback) => ExamineText.Get(key, fallback);

        private static ExamineSection Line(string key, string fallback, string argument) =>
            new(argument == null ? ExamineText.Get(key, fallback) : ExamineText.Get(key, fallback, argument), ExamineBand.Close);

        private static (string, string) ApproximateVolume(float volumeMl)
        {
            if (volumeMl < 5f)
            {
                return (ExamineSubstanceKeys.VolumeFew, ExamineSubstanceKeys.VolumeFewFallback);
            }

            if (volumeMl < 30f)
            {
                return (ExamineSubstanceKeys.VolumeSmall, ExamineSubstanceKeys.VolumeSmallFallback);
            }

            if (volumeMl < 80f)
            {
                return (ExamineSubstanceKeys.VolumeModerate, ExamineSubstanceKeys.VolumeModerateFallback);
            }

            return (ExamineSubstanceKeys.VolumeLarge, ExamineSubstanceKeys.VolumeLargeFallback);
        }

        private static (string, string) TemperatureBand(float kelvin)
        {
            if (kelvin < 260f)
            {
                return (ExamineSubstanceKeys.TempFrozen, ExamineSubstanceKeys.TempFrozenFallback);
            }

            if (kelvin < 280f)
            {
                return (ExamineSubstanceKeys.TempCold, ExamineSubstanceKeys.TempColdFallback);
            }

            if (kelvin < 310f)
            {
                return (ExamineSubstanceKeys.TempCool, ExamineSubstanceKeys.TempCoolFallback);
            }

            if (kelvin < 340f)
            {
                return (ExamineSubstanceKeys.TempWarm, ExamineSubstanceKeys.TempWarmFallback);
            }

            if (kelvin < 370f)
            {
                return (ExamineSubstanceKeys.TempHot, ExamineSubstanceKeys.TempHotFallback);
            }

            return (ExamineSubstanceKeys.TempScalding, ExamineSubstanceKeys.TempScaldingFallback);
        }

        private static (string, string) DescribeColor(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            if (s < 0.15f)
            {
                return v > 0.7f
                    ? (ExamineSubstanceKeys.ColorClear, ExamineSubstanceKeys.ColorClearFallback)
                    : (ExamineSubstanceKeys.ColorMurky, ExamineSubstanceKeys.ColorMurkyFallback);
            }

            if (h < 0.05f || h > 0.95f)
            {
                return (ExamineSubstanceKeys.ColorRed, ExamineSubstanceKeys.ColorRedFallback);
            }

            if (h < 0.15f)
            {
                return (ExamineSubstanceKeys.ColorOrange, ExamineSubstanceKeys.ColorOrangeFallback);
            }

            if (h < 0.2f)
            {
                return (ExamineSubstanceKeys.ColorYellow, ExamineSubstanceKeys.ColorYellowFallback);
            }

            if (h < 0.45f)
            {
                return (ExamineSubstanceKeys.ColorGreen, ExamineSubstanceKeys.ColorGreenFallback);
            }

            if (h < 0.7f)
            {
                return (ExamineSubstanceKeys.ColorBlue, ExamineSubstanceKeys.ColorBlueFallback);
            }

            return (ExamineSubstanceKeys.ColorPurple, ExamineSubstanceKeys.ColorPurpleFallback);
        }
    }
}
