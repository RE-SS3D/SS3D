using SS3D.UI.MachineInterface.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// Bottom-center gear strip (belt/ID/PDA/back) + the two hand slots, active hand marked with the
    /// accent-rust border wrapper from the mockup.
    /// </summary>
    public class HandsGearStrip : VisualElement
    {
        public enum GearSlot
        {
            Belt,
            Id,
            Pda,
            Back,
        }

        private readonly InventorySlot _belt;
        private readonly InventorySlot _id;
        private readonly InventorySlot _pda;
        private readonly InventorySlot _back;
        private readonly InventorySlot _handLeft;
        private readonly InventorySlot _handRight;

        public HandsGearStrip(MainHudIconSet icons)
        {
            AddToClassList("hands-gear-strip");

            _belt = CreateSlot("Belt", icons.Belt, 44);
            _id = CreateSlot("ID", icons.Id, 44);
            _pda = CreateSlot("PDA", icons.Pda, 44);
            _back = CreateSlot("Back", icons.Back, 44);

            VisualElement gear = new();
            gear.AddToClassList("hands-gear-strip__gear");
            gear.Add(_belt);
            gear.Add(_id);
            gear.Add(_pda);
            gear.Add(_back);

            VisualElement divider = new();
            divider.AddToClassList("hands-gear-strip__divider");

            _handLeft = CreateSlot("Left hand", icons.HandLeft, 64);
            _handRight = CreateSlot("Right hand", icons.HandRight, 64);

            VisualElement hands = new();
            hands.AddToClassList("hands-gear-strip__hands");
            hands.Add(_handLeft);
            hands.Add(_handRight);

            Add(gear);
            Add(divider);
            Add(hands);

            SetActiveHand(leftIsActive: true);
        }

        public void SetGearIcon(GearSlot slot, Sprite itemIcon)
        {
            GetGearSlot(slot).ItemIcon = itemIcon;
        }

        public void SetHandIcons(Sprite leftItemIcon, Sprite rightItemIcon)
        {
            _handLeft.ItemIcon = leftItemIcon;
            _handRight.ItemIcon = rightItemIcon;
        }

        public void SetActiveHand(bool leftIsActive)
        {
            _handLeft.EnableInClassList("inventory-slot--active-hand", leftIsActive);
            _handRight.EnableInClassList("inventory-slot--active-hand", !leftIsActive);
        }

        private InventorySlot GetGearSlot(GearSlot slot) => slot switch
        {
            GearSlot.Belt => _belt,
            GearSlot.Id => _id,
            GearSlot.Pda => _pda,
            GearSlot.Back => _back,
            _ => null,
        };

        private static InventorySlot CreateSlot(string label, Sprite emptyIcon, float size)
        {
            return new InventorySlot { Unknown = emptyIcon == null, EmptyIcon = emptyIcon, Size = size, SlotLabel = label };
        }
    }
}
