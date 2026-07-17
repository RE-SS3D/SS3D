using System;
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

        /// <summary>
        /// Fired when a hand well is clicked. Argument is true for the left (first) hand slot.
        /// </summary>
        public event Action<bool> HandClickRequested;

        /// <summary>
        /// Fired when a gear-strip slot (belt/ID/PDA/back) is clicked — opens that slot's storage
        /// panel (Documents/design/inventory-storage.md §6/§11).
        /// </summary>
        public event Action<GearSlot> GearSlotClicked;

        private readonly InventorySlot _belt;
        private readonly InventorySlot _id;
        private readonly InventorySlot _pda;
        private readonly InventorySlot _back;
        private readonly InventorySlot _handLeft;
        private readonly InventorySlot _handRight;

        public HandsGearStrip(MainHudIconSet icons)
        {
            AddToClassList("hands-gear-strip");

            _belt = CreateSlot("Belt", icons.Belt, 64);
            _id = CreateSlot("ID", icons.Id, 64);
            _pda = CreateSlot("PDA", icons.Pda, 64);
            _back = CreateSlot("Back", icons.Back, 64);
            _belt.RegisterCallback<ClickEvent>(_ => GearSlotClicked?.Invoke(GearSlot.Belt));
            _id.RegisterCallback<ClickEvent>(_ => GearSlotClicked?.Invoke(GearSlot.Id));
            _pda.RegisterCallback<ClickEvent>(_ => GearSlotClicked?.Invoke(GearSlot.Pda));
            _back.RegisterCallback<ClickEvent>(_ => GearSlotClicked?.Invoke(GearSlot.Back));

            VisualElement gear = new();
            gear.AddToClassList("hands-gear-strip__gear");
            gear.Add(_belt);
            gear.Add(_id);
            gear.Add(_pda);
            gear.Add(_back);

            VisualElement divider = new();
            divider.AddToClassList("hands-gear-strip__divider");

            _handLeft = CreateSlot("Left hand", icons.HandLeft, 96);
            _handRight = CreateSlot("Right hand", icons.HandRight, 96);
            _handLeft.RegisterCallback<ClickEvent>(_ => HandClickRequested?.Invoke(true));
            _handRight.RegisterCallback<ClickEvent>(_ => HandClickRequested?.Invoke(false));

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

        /// <summary>Panel-space bounds of a gear slot, used to anchor its storage panel near the click.</summary>
        public Rect GetGearSlotWorldBound(GearSlot slot) => GetGearSlot(slot).worldBound;

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
