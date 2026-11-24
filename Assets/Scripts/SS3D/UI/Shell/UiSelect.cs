using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Shell
{
    /// <summary>
    /// A dropdown drawn inside its own tree: a field showing the value, and a list of choices that opens under it on
    /// the nearest ancestor with <see cref="ListHostClassName"/>. Use it in place of a <c>DropdownField</c> under a
    /// scaled surface (<see cref="UiReferenceScale"/>): Unity opens a DropdownField's list at the panel's root and sizes
    /// it wrongly when an ancestor is scaled. Styles: <c>Tokens/ui-controls.uss</c>.
    /// </summary>
    public sealed class UiSelect : VisualElement, INotifyValueChanged<string>
    {
        public const string UssClassName = "ui-select";
        /// <summary>The element the list opens on: a surface root, so the list draws over everything after the field.</summary>
        public const string ListHostClassName = "ui-select-host";

        private readonly Button _field;
        private readonly Label _shown;
        private List<string> _choices = new();
        private string _value;
        private VisualElement _list;
        private VisualElement _host;

        /// <param name="label">A caption above the field, as a DropdownField's label; none when null or empty.</param>
        public UiSelect(string label = null)
        {
            AddToClassList(UssClassName);
            if (!string.IsNullOrEmpty(label))
            {
                Label caption = new(label) { pickingMode = PickingMode.Ignore };
                caption.AddToClassList("ui-select__label");
                Add(caption);
            }

            _field = new Button(Toggle);
            _field.AddToClassList("ui-select__field");
            _shown = new Label { pickingMode = PickingMode.Ignore };
            _shown.AddToClassList("ui-select__value");
            _field.Add(_shown);
            VisualElement arrow = new() { pickingMode = PickingMode.Ignore };
            arrow.AddToClassList("ui-select__arrow");
            _field.Add(arrow);
            Add(_field);
            RegisterCallback<DetachFromPanelEvent>(_ => Close());
        }

        public List<string> Choices
        {
            get => _choices;
            set => _choices = value ?? new List<string>();
        }

        /// <summary>The value's position in <see cref="Choices"/>, or -1.</summary>
        public int Index => _value == null ? -1 : _choices.IndexOf(_value);

        public bool IsOpen => _list != null;

        public string value
        {
            get => _value;
            set
            {
                if (value == _value) return;
                string previous = _value;
                SetValueWithoutNotify(value);
                using ChangeEvent<string> change = ChangeEvent<string>.GetPooled(previous, value);
                change.target = this;
                SendEvent(change);
            }
        }

        public void SetValueWithoutNotify(string newValue)
        {
            _value = newValue;
            _shown.text = newValue ?? "";
        }

        public void Close()
        {
            if (_list == null) return;
            _host?.UnregisterCallback<PointerDownEvent>(OnHostPointerDown, TrickleDown.TrickleDown);
            _list.RemoveFromHierarchy();
            _list = null;
            _host = null;
            RemoveFromClassList(UssClassName + "--open");
        }

        private void Toggle()
        {
            if (_list != null) Close();
            else Open();
        }

        private void Open()
        {
            _host = FindHost();
            if (_host == null || _choices.Count == 0) return;
            _list = new VisualElement { name = string.IsNullOrEmpty(name) ? "ui-select-list" : name + "-list" };
            _list.AddToClassList(UssClassName + "__list");
            ScrollView scroll = new(ScrollViewMode.Vertical) { horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            scroll.AddToClassList(UssClassName + "__scroll");
            _list.Add(scroll);
            foreach (string choice in _choices)
            {
                string picked = choice;
                Button item = new(() =>
                {
                    Close();
                    value = picked;
                }) { text = choice };
                item.AddToClassList(UssClassName + "__item");
                item.EnableInClassList(UssClassName + "__item--on", choice == _value);
                scroll.Add(item);
            }

            _host.Add(_list);
            // Host-local coordinates already undo any scale above the host.
            Rect at = _host.WorldToLocal(_field.worldBound);
            _list.style.position = Position.Absolute;
            _list.style.left = at.x;
            _list.style.top = at.yMax + 2f;
            _list.style.width = at.width;
            AddToClassList(UssClassName + "--open");
            _host.RegisterCallback<PointerDownEvent>(OnHostPointerDown, TrickleDown.TrickleDown);
        }

        private void OnHostPointerDown(PointerDownEvent evt)
        {
            if (evt.target is VisualElement target && (_list.Contains(target) || _field.Contains(target))) return;
            Close();
        }

        /// <summary>The nearest ancestor marked as a list host, else the top of this tree.</summary>
        private VisualElement FindHost()
        {
            VisualElement top = null;
            for (VisualElement element = hierarchy.parent; element != null; element = element.hierarchy.parent)
            {
                if (element.ClassListContains(ListHostClassName)) return element;
                top = element;
            }

            return top;
        }
    }
}
