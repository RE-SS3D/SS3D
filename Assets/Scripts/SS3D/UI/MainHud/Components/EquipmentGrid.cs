using SS3D.UI.MachineInterface.Components;
using UnityEngine.UIElements;

namespace SS3D.UI.MainHud.Components
{
    /// <summary>
    /// Bottom-left worn-equipment doll: head/eyes/face-cover/ears on top, gloves/shirt/feet below - same
    /// 3-column arrangement as the Main HUD mockup. Occupancy is refreshed by
    /// <see cref="SS3D.UI.MainHud.MainHudSubSystem"/> via <see cref="SetIcon"/>.
    /// Held items live on the center <see cref="HandsGearStrip"/>, not here.
    /// </summary>
    public class EquipmentGrid : VisualElement
    {
        public enum Slot
        {
            Head,
            Eyes,
            Face,
            Ears,
            GloveLeft,
            Shirt,
            GloveRight,
            Feet,
        }

        private readonly InventorySlot _head;
        private readonly InventorySlot _eyes;
        private readonly InventorySlot _face;
        private readonly InventorySlot _ears;
        private readonly InventorySlot _gloveLeft;
        private readonly InventorySlot _shirt;
        private readonly InventorySlot _gloveRight;
        private readonly InventorySlot _feet;

        public EquipmentGrid(MainHudIconSet icons)
        {
            AddToClassList("equipment-grid");

            _head = CreateSlot("Head", icons.Head);
            _eyes = CreateSlot("Eyes", icons.Eyes);
            _face = CreateSlot("Face Cover", icons.Face);
            _ears = CreateSlot("Ears", icons.Ears);
            _gloveLeft = CreateSlot("Left Glove", icons.HandLeft);
            _shirt = CreateSlot("Shirt", icons.Shirt);
            _gloveRight = CreateSlot("Right Glove", icons.HandRight);
            _feet = CreateSlot("Feet", icons.Feet);

            Add(BuildRow(BuildSpacer(), _head, BuildSpacer()));
            Add(BuildRow(_eyes, _face, _ears));
            Add(BuildRow(_gloveLeft, _shirt, _gloveRight));
            Add(BuildRow(BuildSpacer(), _feet, BuildSpacer()));
        }

        public void SetIcon(Slot slot, UnityEngine.Sprite itemIcon)
        {
            GetSlot(slot).ItemIcon = itemIcon;
        }

        private InventorySlot GetSlot(Slot slot) => slot switch
        {
            Slot.Head => _head,
            Slot.Eyes => _eyes,
            Slot.Face => _face,
            Slot.Ears => _ears,
            Slot.GloveLeft => _gloveLeft,
            Slot.Shirt => _shirt,
            Slot.GloveRight => _gloveRight,
            Slot.Feet => _feet,
            _ => null,
        };

        private static InventorySlot CreateSlot(string label, UnityEngine.Sprite emptyIcon)
        {
            return new InventorySlot { Unknown = emptyIcon == null, EmptyIcon = emptyIcon, Size = 56, SlotLabel = label };
        }

        private static VisualElement BuildRow(params VisualElement[] children)
        {
            VisualElement row = new();
            row.AddToClassList("equipment-grid__row");
            foreach (VisualElement child in children)
            {
                row.Add(child);
            }

            return row;
        }

        private static VisualElement BuildSpacer()
        {
            VisualElement spacer = new();
            spacer.AddToClassList("equipment-grid__spacer");
            return spacer;
        }
    }
}
