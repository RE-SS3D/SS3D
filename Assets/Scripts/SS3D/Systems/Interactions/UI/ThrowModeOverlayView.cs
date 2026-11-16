using System.Collections.Generic;
using SS3D.Systems.Inputs;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Interactions.UI
{
    /// <summary>
    /// Throw mode (R) overlay: the item's flight drawn as a dashed arc from the hand to where it lands, and a
    /// ring on the floor at the landing point. No reticle or text: the arc is the feedback. Colours come from
    /// the armed-interaction stylesheet (valid green, clamped rust, blocked red) through <c>color</c>.
    /// </summary>
    public sealed class ThrowModeOverlayView : IUiSurface
    {
        public enum State
        {
            /// <summary>Lands where the cursor points.</summary>
            InRange,
            /// <summary>The cursor is past the item's range; the arc ends short of it.</summary>
            Clamped,
            /// <summary>Nothing can be thrown (too heavy, hands unusable): only a ring at the cursor.</summary>
            Blocked,
        }

        private const float LineWidth = 2f;
        private const float DashLength = 9f;
        private const float GapLength = 6f;

        private static readonly Color FallbackInRange = new(0.45f, 0.85f, 0.5f);
        private static readonly Color FallbackClamped = new(0.85f, 0.45f, 0.25f);
        private static readonly Color FallbackBlocked = new(0.9f, 0.3f, 0.3f);

        private readonly StyleSheet _styleSheet;
        private readonly List<Vector2> _arc = new();
        private readonly List<Vector2> _ring = new();

        private VisualElement _overlayRoot;
        private VisualElement _canvas;
        private State _state;

        public ThrowModeOverlayView(StyleSheet styleSheet)
        {
            _styleSheet = styleSheet;
        }

        public void Attach(VisualElement layerRoot)
        {
            _overlayRoot = new VisualElement { name = "throw-mode-overlay-surface", pickingMode = PickingMode.Ignore };
            Stretch(_overlayRoot);
            if (_styleSheet != null)
            {
                _overlayRoot.styleSheets.Add(_styleSheet);
            }

            _canvas = new VisualElement { pickingMode = PickingMode.Ignore };
            _canvas.AddToClassList("throw-mode-arc");
            Stretch(_canvas);
            _canvas.generateVisualContent += Draw;
            _overlayRoot.Add(_canvas);

            layerRoot.Add(_overlayRoot);
            Hide();
        }

        public void Detach()
        {
            _overlayRoot?.RemoveFromHierarchy();
            _overlayRoot = null;
            _canvas = null;
        }

        public void Hide()
        {
            if (_overlayRoot != null)
            {
                _overlayRoot.style.display = DisplayStyle.None;
            }
        }

        /// <param name="arcScreen">The flight path in screen pixels, release first (empty when <see cref="State.Blocked"/>).</param>
        /// <param name="ringScreen">The landing ring in screen pixels, a closed loop.</param>
        public void Show(IReadOnlyList<Vector2> arcScreen, IReadOnlyList<Vector2> ringScreen, State state)
        {
            if (_overlayRoot?.panel == null)
            {
                return;
            }

            _overlayRoot.style.display = DisplayStyle.Flex;
            _state = state;
            _canvas.EnableInClassList("throw-mode-arc--clamped", state == State.Clamped);
            _canvas.EnableInClassList("throw-mode-arc--blocked", state == State.Blocked);
            ToLocal(arcScreen, _arc);
            ToLocal(ringScreen, _ring);
            _canvas.MarkDirtyRepaint();
        }

        private void ToLocal(IReadOnlyList<Vector2> screen, List<Vector2> local)
        {
            local.Clear();
            for (int i = 0; i < screen.Count; i++)
            {
                local.Add(_canvas.WorldToLocal(InputInterface.ScreenToPanel(_overlayRoot.panel, screen[i])));
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            Painter2D painter = context.painter2D;
            Color color = ResolveColor();
            painter.lineWidth = LineWidth;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.strokeColor = color;

            DrawDashed(painter, _arc);

            if (_ring.Count >= 3)
            {
                painter.fillColor = new Color(color.r, color.g, color.b, 0.18f);
                painter.BeginPath();
                painter.MoveTo(_ring[0]);
                for (int i = 1; i < _ring.Count; i++)
                {
                    painter.LineTo(_ring[i]);
                }

                painter.ClosePath();
                painter.Fill();
                painter.Stroke();
            }
        }

        /// <summary>Strokes the polyline as dashes of fixed screen length, so the arc reads the same at any distance.</summary>
        private static void DrawDashed(Painter2D painter, List<Vector2> points)
        {
            if (points.Count < 2)
            {
                return;
            }

            bool drawing = true;
            float left = DashLength;
            painter.BeginPath();
            painter.MoveTo(points[0]);
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 from = points[i - 1];
                Vector2 to = points[i];
                float segment = Vector2.Distance(from, to);
                float done = 0f;
                while (segment - done > left)
                {
                    done += left;
                    Vector2 cut = Vector2.Lerp(from, to, done / segment);
                    if (drawing)
                    {
                        painter.LineTo(cut);
                    }
                    else
                    {
                        painter.MoveTo(cut);
                    }

                    drawing = !drawing;
                    left = drawing ? DashLength : GapLength;
                }

                left -= segment - done;
                if (drawing)
                {
                    painter.LineTo(to);
                }
                else
                {
                    painter.MoveTo(to);
                }
            }

            painter.Stroke();
        }

        private Color ResolveColor()
        {
            Color fallback = _state switch
            {
                State.Clamped => FallbackClamped,
                State.Blocked => FallbackBlocked,
                _ => FallbackInRange,
            };

            // Without the stylesheet the element keeps the inherited text colour; use the fallback then.
            return _overlayRoot != null && _overlayRoot.styleSheets.count > 0 ? _canvas.resolvedStyle.color : fallback;
        }

        private static void Stretch(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.top = 0;
            element.style.right = 0;
            element.style.bottom = 0;
        }
    }
}
