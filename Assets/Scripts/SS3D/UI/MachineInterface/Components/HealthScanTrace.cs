using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    /// <summary>
    /// The pulse-oximeter trace: three beats in the reading's tone, or a flat line when the heart
    /// gives no output. Decorative — the number beside it is the reading.
    /// </summary>
    [UxmlElement]
    public partial class HealthScanTrace : VisualElement
    {
        // One beat as (x, y) in a 76×84 cell, y down: baseline, spike up, dip, back to baseline.
        private static readonly Vector2[] Beat =
        {
            new(0f, 42f), new(28f, 42f), new(34f, 10f), new(40f, 74f), new(46f, 42f), new(76f, 42f),
        };

        private const float CellWidth = 76f;
        private const float CellHeight = 84f;
        private const int Beats = 3;

        private Color _color = Color.white;
        private bool _flat;

        public HealthScanTrace()
        {
            AddToClassList("health-scan-trace");
            generateVisualContent += Draw;
        }

        public void SetTrace(Color color, bool flat)
        {
            _color = color;
            _flat = flat;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            float sx = rect.width / (CellWidth * Beats);
            float sy = rect.height / CellHeight;
            Painter2D painter = context.painter2D;
            painter.strokeColor = _color;
            painter.lineWidth = 2f;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, 42f * sy));
            if (_flat)
            {
                painter.LineTo(new Vector2(rect.width, 42f * sy));
            }
            else
            {
                for (int beat = 0; beat < Beats; beat++)
                {
                    foreach (Vector2 point in Beat)
                    {
                        painter.LineTo(new Vector2((beat * CellWidth + point.x) * sx, point.y * sy));
                    }
                }
            }

            painter.Stroke();
        }
    }
}
