using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class MachineWindow : VisualElement
    {
        public event Action CloseClicked;

        public const float DefaultWidth = 380f;
        public const float WideWidth = 780f;

        private readonly VisualElement _header;
        private readonly Label _titleLabel;
        private readonly VisualElement _content;
        private readonly Button _closeButton;
        private bool _isDragging;
        private Vector2 _dragStartPointer;
        private Vector2 _dragStartPosition;

        public MachineWindow()
        {
            AddToClassList("machine-window");

            _header = new VisualElement();
            _header.AddToClassList("machine-window__header");

            _titleLabel = new Label("MACHINE");
            _titleLabel.AddToClassList("machine-window__title");
            _titleLabel.AddToClassList("font-titling");

            _closeButton = new Button(OnCloseClicked) { text = "×" };
            _closeButton.AddToClassList("machine-window__close");

            _header.Add(_titleLabel);
            _header.Add(_closeButton);

            _content = new VisualElement();
            _content.AddToClassList("machine-window__content");

            Add(_header);
            Add(_content);

            RegisterDragHandlers();
        }

        [UxmlAttribute]
        public string Title
        {
            get => _titleLabel.text;
            set => _titleLabel.text = value;
        }

        public VisualElement Content => _content;

        private void RegisterDragHandlers()
        {
            _header.RegisterCallback<PointerDownEvent>(OnHeaderPointerDown);
            _header.RegisterCallback<PointerMoveEvent>(OnHeaderPointerMove);
            _header.RegisterCallback<PointerUpEvent>(OnHeaderPointerUp);
            _header.RegisterCallback<PointerCaptureOutEvent>(_ => EndDrag());
        }

        private void OnCloseClicked()
        {
            CloseClicked?.Invoke();
        }

        private void OnHeaderPointerDown(PointerDownEvent evt)
        {
            if (evt.target == _closeButton)
            {
                return;
            }

            ConvertToPixelPosition();
            _isDragging = true;
            _dragStartPointer = evt.position;
            _dragStartPosition = new Vector2(resolvedStyle.left, resolvedStyle.top);
            _header.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnHeaderPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging)
            {
                return;
            }

            Vector2 delta = (Vector2)evt.position - _dragStartPointer;
            Vector2 newPosition = _dragStartPosition + delta;
            ClampToParent(ref newPosition);
            style.left = newPosition.x;
            style.top = newPosition.y;
            evt.StopPropagation();
        }

        private void OnHeaderPointerUp(PointerUpEvent evt)
        {
            EndDrag();
            evt.StopPropagation();
        }

        private void EndDrag()
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;
            _header.ReleasePointer(PointerId.mousePointerId);
        }

        private void ConvertToPixelPosition()
        {
            VisualElement parentElement = parent;
            if (parentElement == null)
            {
                return;
            }

            Rect parentBounds = parentElement.worldBound;
            Rect selfBounds = worldBound;

            style.translate = new Translate(0, 0);
            style.left = selfBounds.x - parentBounds.x;
            style.top = selfBounds.y - parentBounds.y;
        }

        private void ClampToParent(ref Vector2 position)
        {
            VisualElement parentElement = parent;
            if (parentElement == null)
            {
                return;
            }

            float panelWidth = resolvedStyle.width;
            float panelHeight = resolvedStyle.height;
            float parentWidth = parentElement.resolvedStyle.width;
            float parentHeight = parentElement.resolvedStyle.height;

            if (float.IsNaN(panelWidth) || panelWidth <= 0f)
            {
                panelWidth = ClassListContains("machine-window--wide") ? WideWidth : DefaultWidth;
            }

            if (float.IsNaN(panelHeight) || panelHeight <= 0f)
            {
                panelHeight = 400f;
            }

            position.x = Mathf.Clamp(position.x, 0f, Mathf.Max(0f, parentWidth - panelWidth));
            position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, parentHeight - panelHeight));
        }
    }
}
