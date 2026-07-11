using SS3D.UI.MachineInterface;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Components
{
    [UxmlElement]
    public partial class DispenseTray : VisualElement
    {
        public event Action<int> TakeRequested;

        private readonly VisualElement _items;

        public DispenseTray()
        {
            AddToClassList("dispense-tray");

            Label title = new("DISPENSE TRAY");
            title.AddToClassList("dispense-tray__title");
            title.AddToClassList("font-arcade");

            _items = new VisualElement();
            _items.AddToClassList("dispense-tray__items");

            Add(title);
            Add(_items);
        }

        public void SetTrayItems(IReadOnlyList<VendingTrayItemViewData> items)
        {
            _items.Clear();

            if (items == null || items.Count == 0)
            {
                Label empty = new("— empty —");
                empty.AddToClassList("dispense-tray__empty");
                empty.AddToClassList("font-terminal");
                _items.Add(empty);
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                VendingTrayItemViewData item = items[i];
                VisualElement row = new();
                row.AddToClassList("dispense-tray__row");

                VisualElement left = new();
                left.AddToClassList("dispense-tray__row-left");

                InventorySlot slot = new() { Unknown = true, Size = 32 };
                Label name = new(item.Name);
                name.AddToClassList("dispense-tray__item-name");
                name.AddToClassList("font-body");

                left.Add(slot);
                left.Add(name);

                SteelButton takeButton = new() { Text = "Take" };
                int trayIndex = item.TrayIndex;
                takeButton.Clicked += () => TakeRequested?.Invoke(trayIndex);

                row.Add(left);
                row.Add(takeButton);
                _items.Add(row);
            }
        }
    }
}
