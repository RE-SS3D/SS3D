using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class ActionLog : VisualElement
    {
        private const int DefaultMaxEntries = 4;

        private readonly Label _title;
        private readonly VisualElement _entries;
        private int _maxEntries = DefaultMaxEntries;

        public ActionLog()
        {
            AddToClassList("action-log");

            _title = new Label("ACTION LOG");
            _title.AddToClassList("action-log__title");
            _title.AddToClassList("font-arcade");

            _entries = new VisualElement();
            _entries.AddToClassList("action-log__entries");

            Add(_title);
            Add(_entries);
        }

        [UxmlAttribute]
        public int MaxEntries
        {
            get => _maxEntries;
            set => _maxEntries = value > 0 ? value : DefaultMaxEntries;
        }

        public void SetEntries(IReadOnlyList<string> entries)
        {
            _entries.Clear();

            if (entries == null || entries.Count == 0)
            {
                style.display = DisplayStyle.None;
                return;
            }

            style.display = DisplayStyle.Flex;
            int count = Mathf.Min(entries.Count, _maxEntries);

            for (int i = 0; i < count; i++)
            {
                Label entry = new(entries[i]);
                entry.AddToClassList("action-log__entry");
                entry.AddToClassList("font-terminal");
                _entries.Add(entry);
            }
        }
    }
}
