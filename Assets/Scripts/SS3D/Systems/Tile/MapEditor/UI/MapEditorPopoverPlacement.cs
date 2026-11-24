using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    /// <summary>Which way a floating plate opens from its button; its caret sits on the edge facing the button.</summary>
    public enum MapEditorPopoverSide
    {
        Right,
        Left,
        Down,
    }

    /// <summary>
    /// Puts a popover beside the button that opened it, with the storage panel's caret aimed back at the button (UI
    /// bible: "point back to the source"). The view tools' popovers open to their right, the tool bar's below it, the
    /// camera dial's to its left. Positions are in the popover layer's space, from the anchor's <c>worldBound</c>, so a
    /// popover never depends on where its button sits in the hierarchy (tile.md Pitfalls: popover painted under Map Check).
    /// </summary>
    public static class MapEditorPopoverPlacement
    {
        public const string CaretClass = "map-editor-caret";
        private const string StripClass = "map-editor-toolbar-strip";

        /// <summary>The panel edge to the plate's edge: the caret's 17.5 px and 5 px of air (MapEditor.uss, hints).</summary>
        private const float Gap = 22.5f;
        private const float ScreenMargin = 16f;

        /// <summary>Places the popover now, and again once it has a size (a new popover has none until its first layout).</summary>
        public static void Place(VisualElement layer, VisualElement popover, VisualElement anchor, MapEditorPopoverSide side,
            bool showCaret)
        {
            PlaceNow(layer, popover, anchor, side, showCaret);
            popover.RegisterCallback<GeometryChangedEvent>(Again);

            void Again(GeometryChangedEvent evt)
            {
                popover.UnregisterCallback<GeometryChangedEvent>(Again);
                PlaceNow(layer, popover, anchor, side, showCaret);
            }
        }

        /// <summary>A dragged popover no longer points at its button, so it drops the caret.</summary>
        public static void ShowCaret(VisualElement popover, bool show)
        {
            VisualElement caret = popover.Q(className: CaretClass);
            if (caret != null)
                caret.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>The caret element for a plate's edge: <c>--left</c> on a plate opening right, and so on.</summary>
        public static VisualElement CreateCaret(MapEditorPopoverSide side)
        {
            VisualElement caret = new() { name = "caret", pickingMode = PickingMode.Ignore };
            caret.AddToClassList(CaretClass);
            caret.AddToClassList(side switch
            {
                MapEditorPopoverSide.Right => "map-editor-caret--left",
                MapEditorPopoverSide.Left => "map-editor-caret--right",
                _ => "map-editor-caret--up",
            });
            return caret;
        }

        private static void PlaceNow(VisualElement layer, VisualElement popover, VisualElement anchor, MapEditorPopoverSide side,
            bool showCaret)
        {
            Rect button = layer.WorldToLocal(anchor.worldBound);
            Rect edge = layer.WorldToLocal(EdgeOf(anchor).worldBound);
            float layerWidth = layer.layout.width;
            float layerHeight = layer.layout.height;
            if (float.IsNaN(button.x) || float.IsNaN(layerWidth))
                return;

            float width = float.IsNaN(popover.layout.width) ? popover.resolvedStyle.width : popover.layout.width;
            float height = float.IsNaN(popover.layout.height) ? 0f : popover.layout.height;
            VisualElement caret = popover.Q(className: CaretClass);
            if (caret == null)
            {
                caret = CreateCaret(side);
                popover.Add(caret);
            }

            caret.style.display = showCaret ? DisplayStyle.Flex : DisplayStyle.None;
            if (side == MapEditorPopoverSide.Down)
            {
                // Under the bar, centred on the button where it fits; the caret stays on the button's centre.
                float left = float.IsNaN(width)
                    ? button.center.x
                    : Mathf.Clamp(button.center.x - width / 2f, ScreenMargin, Mathf.Max(ScreenMargin, layerWidth - ScreenMargin - width));
                popover.style.left = left;
                popover.style.right = StyleKeyword.Auto;
                popover.style.top = edge.yMax + Gap;
                caret.style.left = button.center.x - left;
                return;
            }

            // Top-aligned with the button (6 px above, so the caret lands on the header), kept on the screen.
            float top = Mathf.Clamp(button.yMin - 6f, ScreenMargin, Mathf.Max(ScreenMargin, layerHeight - ScreenMargin - height));
            popover.style.top = top;
            if (side == MapEditorPopoverSide.Right)
            {
                popover.style.left = edge.xMax + Gap;
                popover.style.right = StyleKeyword.Auto;
            }
            else
            {
                popover.style.right = layerWidth - edge.xMin + Gap;
                popover.style.left = StyleKeyword.Auto;
            }

            caret.style.top = button.center.y - top;
        }

        /// <summary>The panel a button sits in: the popover clears the panel's edge, not just the button's.</summary>
        private static VisualElement EdgeOf(VisualElement anchor)
        {
            for (VisualElement element = anchor; element != null; element = element.parent)
            {
                if (element.ClassListContains(StripClass))
                    return element;
            }

            return anchor;
        }
    }
}
