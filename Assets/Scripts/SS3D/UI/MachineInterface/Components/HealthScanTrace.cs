using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    /// <summary>
    /// The pulse-oximeter trace from the design: a grid, the rhythm's polyline, and a sweep bar running
    /// across it every <see cref="SweepSeconds"/>. Decorative — the numbers beside it are the reading.
    /// </summary>
    [UxmlElement]
    public partial class HealthScanTrace : VisualElement
    {
        private const float ViewWidth = 220f;
        private const float ViewHeight = 84f;
        private const float SweepSeconds = 2.6f;
        private const float SweepWidth = 8f;

        // The design's polylines, in its 220×84 view box (y down).
        private static readonly Vector2[] Steady = Points(
            0, 42, 20, 42, 28, 42, 34, 10, 40, 74, 46, 42, 60, 42, 96, 42, 104, 42, 110, 10, 116, 74, 122, 42,
            136, 42, 172, 42, 180, 42, 186, 10, 192, 74, 198, 42, 220, 42);

        private static readonly Vector2[] Rapid = Points(
            0, 42, 10, 42, 14, 14, 18, 70, 22, 42, 44, 42, 48, 14, 52, 70, 56, 42, 78, 42, 82, 14, 86, 70, 90, 42,
            112, 42, 116, 14, 120, 70, 124, 42, 146, 42, 150, 14, 154, 70, 158, 42, 180, 42, 184, 14, 188, 70,
            192, 42, 220, 42);

        private static readonly Vector2[] Weak = Points(
            0, 42, 24, 42, 30, 42, 34, 30, 38, 52, 42, 42, 70, 42, 100, 42, 106, 42, 110, 30, 114, 52, 118, 42,
            150, 42, 176, 42, 182, 42, 186, 30, 190, 52, 194, 42, 220, 42);

        private static readonly Vector2[] Flat = Points(0, 42, 220, 42);

        private static readonly Color GridColor = new(0.85f, 0.85f, 0.85f, 0.1f);

        private Vector2[] _points = Steady;
        private Color _color = Color.white;
        private float _sweep;
        private IVisualElementScheduledItem _ticker;

        public HealthScanTrace()
        {
            AddToClassList("health-scan-trace");
            generateVisualContent += Draw;
            RegisterCallback<AttachToPanelEvent>(_ => StartSweep());
            RegisterCallback<DetachFromPanelEvent>(_ => _ticker?.Pause());
        }

        public void SetTrace(HealthScanRhythm rhythm, Color color)
        {
            _points = rhythm switch
            {
                HealthScanRhythm.Rapid => Rapid,
                HealthScanRhythm.Weak => Weak,
                HealthScanRhythm.Arrest => Flat,
                _ => Steady,
            };
            _color = color;
            MarkDirtyRepaint();
        }

        private void StartSweep()
        {
            _ticker ??= schedule.Execute(() =>
            {
                _sweep = Time.realtimeSinceStartup % SweepSeconds / SweepSeconds;
                MarkDirtyRepaint();
            }).Every(16);
            _ticker.Resume();
        }

        private void Draw(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float sx = rect.width / ViewWidth;
            float sy = rect.height / ViewHeight;
            Painter2D painter = context.painter2D;

            painter.strokeColor = GridColor;
            painter.lineWidth = 1f;
            foreach (float y in new[] { 21f, 42f, 63f })
            {
                painter.BeginPath();
                painter.MoveTo(new Vector2(0f, y * sy));
                painter.LineTo(new Vector2(rect.width, y * sy));
                painter.Stroke();
            }

            painter.strokeColor = _color;
            painter.lineWidth = 2f;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            painter.MoveTo(Scale(_points[0], sx, sy));
            for (int i = 1; i < _points.Length; i++)
            {
                painter.LineTo(Scale(_points[i], sx, sy));
            }

            painter.Stroke();

            Color sweepColor = _color;
            sweepColor.a = 0.5f;
            float x = _sweep * rect.width;
            painter.fillColor = sweepColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(x, 0f));
            painter.LineTo(new Vector2(x + SweepWidth * sx, 0f));
            painter.LineTo(new Vector2(x + SweepWidth * sx, rect.height));
            painter.LineTo(new Vector2(x, rect.height));
            painter.ClosePath();
            painter.Fill();
        }

        private static Vector2 Scale(Vector2 point, float sx, float sy) => new(point.x * sx, point.y * sy);

        private static Vector2[] Points(params float[] xy)
        {
            var points = new Vector2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            }

            return points;
        }
    }
}
