using SS3D.Systems.Health;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    /// <summary>
    /// Half-dial for the toxin pool: neutral, warning and danger bands on the scanner's own
    /// thresholds, with a needle at the reading.
    /// </summary>
    [UxmlElement]
    public partial class HealthScanGauge : VisualElement
    {
        private const float BandWidth = 8f;
        private const float StartDegrees = 180f;

        private float _value;

        public HealthScanGauge()
        {
            AddToClassList("health-scan-gauge");
            generateVisualContent += Draw;
        }

        /// <summary>The reading, 0–1 of full scale.</summary>
        public void SetValue(float value)
        {
            _value = Mathf.Clamp01(value);
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            float radius = Mathf.Min(rect.width * 0.5f, rect.height) - BandWidth;
            Vector2 centre = new(rect.width * 0.5f, rect.height - BandWidth * 0.5f);

            float warning = HealthScanSnapshotMapper.WarningDeviation;
            float danger = HealthConstants.CriticalToxinConcentration;
            DrawBand(painter, centre, radius, 0f, warning, HealthScanSnapshotMapper.ToneColor(StatusTone.Success));
            DrawBand(painter, centre, radius, warning, danger, HealthScanSnapshotMapper.ToneColor(StatusTone.Warning));
            DrawBand(painter, centre, radius, danger, 1f, HealthScanSnapshotMapper.ToneColor(StatusTone.Danger));

            float angle = (StartDegrees + _value * 180f) * Mathf.Deg2Rad;
            Vector2 tip = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius - BandWidth);
            painter.strokeColor = HealthScanSnapshotMapper.ToneColor(StatusTone.Info);
            painter.lineWidth = 2.5f;
            painter.lineCap = LineCap.Round;
            painter.BeginPath();
            painter.MoveTo(centre);
            painter.LineTo(tip);
            painter.Stroke();

            painter.fillColor = HealthScanSnapshotMapper.ToneColor(StatusTone.Info);
            painter.BeginPath();
            painter.Arc(centre, 4f, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Fill();
        }

        private static void DrawBand(Painter2D painter, Vector2 centre, float radius, float from, float to, Color color)
        {
            painter.strokeColor = color;
            painter.lineWidth = BandWidth;
            painter.lineCap = LineCap.Butt;
            painter.BeginPath();
            painter.Arc(centre, radius, Angle.Degrees(StartDegrees + from * 180f), Angle.Degrees(StartDegrees + to * 180f));
            painter.Stroke();
        }
    }
}
