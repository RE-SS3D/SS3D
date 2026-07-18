using UnityEngine;

namespace SS3D.UI.MainHud
{
    /// <summary>
    /// The shipped inventory silhouette sprites (<c>Assets/Art/Graphics/UI/Containers/InventoryIcons</c>) the
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
        public Sprite Shirt;
        public Sprite Feet;
        public Sprite Belt;
        public Sprite Id;
        public Sprite Pocket;
        public Sprite Back;
    }
}
