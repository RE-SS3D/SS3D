using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    /// <summary>
    /// The camera dial: the radial interaction menu (UI bible) at 100 px. A dark ring carries four verbs on its band, zoom
    /// in at the top and then clockwise rotate right, zoom out and rotate left; the quadrant under the pointer lights as a
    /// wedge, and the whole quadrant is that verb's button. The warm grey hub opens Camera Options.
    /// </summary>
    public sealed class MapEditorCameraDial : VisualElement
    {
        /// <summary>--ui-dial and half of --ui-dial-hub.</summary>
        private const float Size = 100f;
        private const float HubRadius = 23.75f;

        private static readonly CustomStyleProperty<Color> WedgeColorProperty = new("--wedge-color");

        private readonly VisualElement _wedge;
        private readonly VisualElement[] _verbs = new VisualElement[4];
        private Color _wedgeColor = new(1f, 1f, 1f, 0.41f);
        private int _pointed = -1;

        public MapEditorCameraDial(MapEditorIconsSo icons, Action openOptions)
        {
            name = "camera-dial";
            pickingMode = PickingMode.Position;
            AddToClassList("map-editor-camera-dial");

            VisualElement ring = new() { name = "camera-dial-ring", pickingMode = PickingMode.Ignore };
            ring.AddToClassList("map-editor-camera-dial__ring");
            Add(ring);

            _wedge = new VisualElement { pickingMode = PickingMode.Ignore };
            _wedge.AddToClassList("map-editor-camera-dial__wedge");
            _wedge.generateVisualContent += DrawWedge;
            _wedge.RegisterCallback<CustomStyleResolvedEvent>(OnWedgeStyle);
            Add(_wedge);

            VectorImage[] glyphs = { icons?.ZoomIn, icons?.RotateRight, icons?.ZoomOut, icons?.RotateLeft };
            string[] names = { "camera-dial-zoom-in", "camera-dial-rotate-right", "camera-dial-zoom-out", "camera-dial-rotate-left" };
            float band = (HubRadius + Size / 2f) / 2f;
            for (int i = 0; i < _verbs.Length; i++)
            {
                float angle = i * Mathf.PI / 2f;
                VisualElement verb = new() { name = names[i], pickingMode = PickingMode.Ignore };
                verb.AddToClassList("map-editor-camera-dial__btn");
                verb.style.left = Size / 2f + Mathf.Sin(angle) * band;
                verb.style.top = Size / 2f - Mathf.Cos(angle) * band;
                verb.Add(Glyph(glyphs[i], "map-editor-camera-dial__btn-icon"));
                _verbs[i] = verb;
                Add(verb);
            }

            Hub = new Button(openOptions) { name = "camera-dial-open", tooltip = "Camera options" };
            Hub.AddToClassList("map-editor-camera-dial__open");
            Hub.Add(Glyph(icons?.Camera, "map-editor-camera-dial__open-icon"));
            Add(Hub);

            RegisterCallback<PointerMoveEvent>(evt => Point(QuadrantAt(this.WorldToLocal(evt.position))));
            RegisterCallback<PointerLeaveEvent>(_ => Point(-1));
            RegisterCallback<ClickEvent>(OnClick);
        }

        /// <summary>A rotate step: -1 left, 1 right.</summary>
        public event Action<int> RotateRequested;

        /// <summary>A zoom step: 1 in, -1 out.</summary>
        public event Action<int> ZoomRequested;

        /// <summary>The hub: opens Camera Options.</summary>
        public Button Hub { get; }

        /// <summary>The hub goes white while Camera Options is open, as a selected tab does.</summary>
        public void SetOptionsOpen(bool open) => Hub.EnableInClassList("map-editor-camera-dial__open--active", open);

        /// <summary>The verb under a point in the dial's own space: 0 at the top, then clockwise; -1 on the hub or off the ring.</summary>
        public static int QuadrantAt(Vector2 local)
        {
            Vector2 offset = local - new Vector2(Size / 2f, Size / 2f);
            float radius = offset.magnitude;
            if (radius < HubRadius || radius > Size / 2f)
                return -1;

            // 0 at the top, growing clockwise (y grows downward in panel space).
            float angle = Mathf.Atan2(offset.x, -offset.y) * Mathf.Rad2Deg;
            return (Mathf.RoundToInt(angle / 90f) + 4) % 4;
        }

        private static VisualElement Glyph(VectorImage image, string className)
        {
            VisualElement glyph = new() { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("map-editor-vector-icon");
            glyph.AddToClassList(className);
            if (image != null)
                glyph.style.backgroundImage = new StyleBackground(image);
            return glyph;
        }

        private void OnClick(ClickEvent evt)
        {
            if (evt.target is VisualElement target && (target == Hub || Hub.Contains(target)))
                return;

            switch (QuadrantAt(this.WorldToLocal(evt.position)))
            {
                case 0:
                    ZoomRequested?.Invoke(1);
                    break;
                case 1:
                    RotateRequested?.Invoke(1);
                    break;
                case 2:
                    ZoomRequested?.Invoke(-1);
                    break;
                case 3:
                    RotateRequested?.Invoke(-1);
                    break;
            }
        }

        private void Point(int verb)
        {
            if (verb == _pointed)
                return;

            _pointed = verb;
            for (int i = 0; i < _verbs.Length; i++)
                _verbs[i].EnableInClassList("map-editor-camera-dial__btn--pointed", i == verb);
            _wedge.MarkDirtyRepaint();
        }

        private void OnWedgeStyle(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(WedgeColorProperty, out Color color))
                _wedgeColor = color;
            _wedge.MarkDirtyRepaint();
        }

        /// <summary>A 90° band from just under the hub's edge to the rim, the radial menu's wedge with a wider spread.</summary>
        private void DrawWedge(MeshGenerationContext context)
        {
            if (_pointed < 0)
                return;

            Painter2D painter = context.painter2D;
            Vector2 centre = new(Size / 2f, Size / 2f);
            // Painter2D measures angles from +x, clockwise; the dial's 0 is up.
            float middle = _pointed * 90f - 90f;
            painter.fillColor = _wedgeColor;
            painter.BeginPath();
            painter.Arc(centre, Size / 2f, Angle.Degrees(middle - 45f), Angle.Degrees(middle + 45f));
            painter.Arc(centre, HubRadius - 1f, Angle.Degrees(middle + 45f), Angle.Degrees(middle - 45f), ArcDirection.CounterClockwise);
            painter.ClosePath();
            painter.Fill();
        }
    }
}
