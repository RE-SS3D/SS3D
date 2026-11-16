using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Systems.Tile.MapEditor.UI
{
    /// <summary>
    /// Drags a map editor panel by its header. Moves the panel with <c>style.translate</c>, so it works for panels in a
    /// flex column (Map Check) and absolutely placed ones (popovers) without changing their layout. Presses on buttons
    /// and fields inside the header still click. The header stays on screen.
    /// </summary>
    public sealed class PanelDragManipulator : PointerManipulator
    {
        /// <summary>Margin from the panel root's edges the dragged header is kept within.</summary>
        private const float KeepVisiblePx = 32f;

        private readonly VisualElement _moved;
        private readonly Func<VisualElement, bool> _isHandle;
        private readonly Action<Vector2> _offsetChanged;
        private Vector2 _offset;
        private Vector2 _lastPointer;
        private int _pointerId = -1;

        /// <param name="moved">The panel that moves.</param>
        /// <param name="isHandle">True when a press on this element (the event target) may start a drag.</param>
        /// <param name="initialOffset">Offset to restore, e.g. where the panel was last left.</param>
        /// <param name="offsetChanged">Called with the new offset on every drag step.</param>
        public PanelDragManipulator(VisualElement moved, Func<VisualElement, bool> isHandle,
            Vector2 initialOffset = default, Action<Vector2> offsetChanged = null)
        {
            _moved = moved;
            _isHandle = isHandle;
            _offsetChanged = offsetChanged;
            _offset = initialOffset;
            Apply();
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (_pointerId >= 0 || !CanStartManipulation(evt) || evt.target is not VisualElement pressed)
                return;
            if (IsInsideControl(pressed) || !_isHandle(pressed))
                return;

            _pointerId = evt.pointerId;
            _lastPointer = ClampToPanel(evt.position);
            target.CapturePointer(_pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != _pointerId || !target.HasPointerCapture(_pointerId))
                return;

            Vector2 pointer = ClampToPanel(evt.position);
            _offset += pointer - _lastPointer;
            _lastPointer = pointer;
            Apply();
            // Report every step: the map editor rebuilds popovers on state changes, which can end a drag mid-way.
            _offsetChanged?.Invoke(_offset);
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != _pointerId)
                return;

            target.ReleasePointer(_pointerId);
            evt.StopPropagation();
        }

        private void OnCaptureOut(PointerCaptureOutEvent evt)
        {
            if (_pointerId < 0)
                return;

            _pointerId = -1;
        }

        private void Apply() => _moved.style.translate = new Translate(_offset.x, _offset.y);

        /// <summary>
        /// The grabbed header sits under the pointer, so keeping the pointer inside the panel root keeps the header
        /// reachable. (A captured pointer keeps reporting positions outside the game view.)
        /// </summary>
        private Vector2 ClampToPanel(Vector2 pointer)
        {
            if (_moved.panel == null)
                return pointer;

            Rect screen = _moved.panel.visualTree.worldBound;
            return new Vector2(
                Mathf.Clamp(pointer.x, screen.xMin + KeepVisiblePx, screen.xMax - KeepVisiblePx),
                Mathf.Clamp(pointer.y, screen.yMin, screen.yMax - KeepVisiblePx));
        }

        private bool IsInsideControl(VisualElement element)
        {
            for (VisualElement e = element; e != null && e != target; e = e.parent)
            {
                if (e is Button || e is TextField || e is BaseSlider<float> || e is Toggle)
                    return true;
            }

            return false;
        }
    }
}
