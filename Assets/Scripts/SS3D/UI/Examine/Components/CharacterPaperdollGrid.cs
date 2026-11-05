using SS3D.Systems.Examine;
using SS3D.UI.MachineInterface.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Examine.Components
{
    /// <summary>
    /// Shared equipment-slot grid for the character-examine paperdoll — one 3-column-per-row layout
    /// (Head / Eyes-Face-Ears / Suit-Shirt-Gloves / Back-Feet-Belt / IdCard-Pocket) plus a hand row,
    /// reused at two sizes: the hover quick-look preview (<see cref="CharacterQuickLookView"/>, 40px)
    /// and the full examine window (<see cref="CharacterExamineWindowView"/>, 64px).
    /// </summary>
    public sealed class CharacterPaperdollGrid : VisualElement
    {
        /// <summary>Matches <c>.character-paperdoll-grid__row > .inventory-slot</c> margin-right in Examine.uss.</summary>
        private const float SlotMargin = 2f;

        private readonly InventorySlot[] _slots = new InventorySlot[SlotOrder.Length];

        private static readonly CharacterExamineSlot[] SlotOrder =
        {
            CharacterExamineSlot.Head,
            CharacterExamineSlot.Eyes,
            CharacterExamineSlot.Face,
            CharacterExamineSlot.Ears,
            CharacterExamineSlot.Suit,
            CharacterExamineSlot.Shirt,
            CharacterExamineSlot.Gloves,
            CharacterExamineSlot.Back,
            CharacterExamineSlot.Feet,
            CharacterExamineSlot.Belt,
            CharacterExamineSlot.IdCard,
            CharacterExamineSlot.Pocket,
            CharacterExamineSlot.HandLeft,
            CharacterExamineSlot.HandRight,
            CharacterExamineSlot.SuitStorage,
        };

        public CharacterPaperdollGrid(float slotSize, CharacterExamineIconSet icons)
        {
            AddToClassList("character-paperdoll-grid");

            InventorySlot head = CreateSlot(CharacterExamineSlot.Head, icons.Head, slotSize);
            InventorySlot eyes = CreateSlot(CharacterExamineSlot.Eyes, icons.Eyes, slotSize);
            InventorySlot face = CreateSlot(CharacterExamineSlot.Face, icons.Face, slotSize);
            InventorySlot ears = CreateSlot(CharacterExamineSlot.Ears, icons.Ears, slotSize);
            InventorySlot suit = CreateSlot(CharacterExamineSlot.Suit, icons.Suit, slotSize);
            InventorySlot shirt = CreateSlot(CharacterExamineSlot.Shirt, icons.Shirt, slotSize);
            InventorySlot gloves = CreateSlot(CharacterExamineSlot.Gloves, icons.Gloves, slotSize);
            InventorySlot back = CreateSlot(CharacterExamineSlot.Back, icons.Back, slotSize);
            InventorySlot feet = CreateSlot(CharacterExamineSlot.Feet, icons.Feet, slotSize);
            InventorySlot belt = CreateSlot(CharacterExamineSlot.Belt, icons.Belt, slotSize);
            InventorySlot idCard = CreateSlot(CharacterExamineSlot.IdCard, icons.IdCard, slotSize);
            InventorySlot pocket = CreateSlot(CharacterExamineSlot.Pocket, icons.Pocket, slotSize);
            InventorySlot suitStorage = CreateSlot(CharacterExamineSlot.SuitStorage, icons.Suit, slotSize);
            InventorySlot handLeft = CreateSlot(CharacterExamineSlot.HandLeft, icons.HandLeft, slotSize);
            InventorySlot handRight = CreateSlot(CharacterExamineSlot.HandRight, icons.HandRight, slotSize);

            // Two hand wells span the same outer width as a 3-slot row (each slot has SlotMargin trailing).
            float handWidth = (slotSize * 3f + SlotMargin) / 2f;
            handLeft.SetWellSize(handWidth, slotSize);
            handRight.SetWellSize(handWidth, slotSize);

            Add(BuildRow(BuildSpacer(slotSize), head, BuildSpacer(slotSize)));
            Add(BuildRow(eyes, face, ears));
            Add(BuildRow(suit, shirt, gloves));
            Add(BuildRow(back, feet, belt));
            Add(BuildRow(idCard, pocket, suitStorage));

            VisualElement handRow = BuildRow(handLeft, handRight);
            handRow.AddToClassList("character-paperdoll-grid__hand-row");
            Add(handRow);
        }

        public InventorySlot GetSlot(CharacterExamineSlot slot) => _slots[(int)slot];

        public void SetSlot(CharacterExamineSlotContent content)
        {
            InventorySlot slot = GetSlot(content.Slot);
            if (slot == null)
            {
                return;
            }

            slot.ItemIcon = content.ItemIcon;
            slot.SlotLabel = string.IsNullOrEmpty(content.ItemName)
                ? DefaultSlotLabel(content.Slot)
                : content.ItemName;
        }

        private InventorySlot CreateSlot(CharacterExamineSlot slot, Sprite emptyIcon, float size)
        {
            InventorySlot inventorySlot = new()
            {
                Unknown = false,
                EmptyIcon = emptyIcon,
                Size = size,
                SlotLabel = DefaultSlotLabel(slot),
            };
            _slots[(int)slot] = inventorySlot;
            return inventorySlot;
        }

        private static string DefaultSlotLabel(CharacterExamineSlot slot) => slot switch
        {
            CharacterExamineSlot.Head => "Head",
            CharacterExamineSlot.Eyes => "Eyes",
            CharacterExamineSlot.Face => "Face Cover",
            CharacterExamineSlot.Ears => "Ears",
            CharacterExamineSlot.Suit => "Suit",
            CharacterExamineSlot.Shirt => "Shirt",
            CharacterExamineSlot.Gloves => "Gloves",
            CharacterExamineSlot.Back => "Back",
            CharacterExamineSlot.Feet => "Feet",
            CharacterExamineSlot.Belt => "Belt",
            CharacterExamineSlot.IdCard => "ID Card",
            CharacterExamineSlot.Pocket => "Pocket",
            CharacterExamineSlot.SuitStorage => "Suit Storage",
            CharacterExamineSlot.HandLeft => "Left Hand",
            CharacterExamineSlot.HandRight => "Right Hand",
            _ => string.Empty,
        };

        private static VisualElement BuildRow(params VisualElement[] children)
        {
            VisualElement row = new();
            row.AddToClassList("character-paperdoll-grid__row");
            foreach (VisualElement child in children)
            {
                row.Add(child);
            }

            return row;
        }

        private static VisualElement BuildSpacer(float size)
        {
            VisualElement spacer = new();
            spacer.AddToClassList("character-paperdoll-grid__spacer");
            spacer.style.width = size;
            return spacer;
        }
    }
}
