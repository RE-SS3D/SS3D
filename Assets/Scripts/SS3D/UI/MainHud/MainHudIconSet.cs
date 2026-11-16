using UnityEngine;

namespace SS3D.UI.MainHud
{
    /// <summary>
    /// The shipped inventory silhouette sprites (<c>Assets/Art/Icons/Inventory</c>) the
    /// HUD's equipment grid and hands/gear strip need as empty-slot placeholders. Assigned on
    /// <see cref="MainHudSubSystem"/> and threaded down to the components that build the slots.
    /// </summary>
    [System.Serializable]
    public struct MainHudIconSet
    {
        public Sprite Head;
        public Sprite Eyes;
        public Sprite Face;
        public Sprite Ears;
        public Sprite HandLeft;
        public Sprite HandRight;
        public Sprite Gloves;
        public Sprite Shirt;
        public Sprite Feet;
        public Sprite Belt;
        public Sprite Id;
        public Sprite Pocket;
        public Sprite Back;
        public Sprite Suit;
        public Sprite SuitStorage;

        /// <summary>Show/hide button for the worn-equipment doll.</summary>
        public Sprite EquipmentToggle;

        /// <summary>Corner button on a slot whose item is itself storage (backpack, toolbelt, box).</summary>
        public Sprite OpenStorage;
    }
}
