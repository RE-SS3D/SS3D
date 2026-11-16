using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Interactions.UI
{
    /// <summary>
    /// A circular progress track drawn with <see cref="Painter2D"/>: a dim full circle and a bright arc
    /// clockwise from twelve o'clock. Size comes from layout; no USS asset needed.
    /// </summary>
    public sealed class ProgressRingElement : VisualElement
    {
        private float _progress;
        private Color _arcColor = Color.white;

        public float LineWidth { get; set; } = 4f;

        public Color TrackColor { get; set; } = new(0f, 0f, 0f, 0.45f);

        public float Progress
        {
            get => _progress;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(clamped, _progress))
                {
                    return;
                }

                _progress = clamped;
                MarkDirtyRepaint();
            }
        }

        public Color ArcColor
        {
            get => _arcColor;
            set
            {
                _arcColor = value;
                MarkDirtyRepaint();
            }
        }

        public ProgressRingElement()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f - LineWidth * 0.5f;
            if (radius <= 0f)
            {
                return;
            }

            Vector2 center = rect.center;
            Painter2D painter = context.painter2D;
            painter.lineWidth = LineWidth;
            painter.lineCap = LineCap.Round;

            painter.strokeColor = TrackColor;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Stroke();

            if (_progress <= 0f)
            {
                return;
            }

            painter.strokeColor = _arcColor;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(-90f), Angle.Degrees(-90f + 360f * _progress));
            painter.Stroke();
        }
    }
}
