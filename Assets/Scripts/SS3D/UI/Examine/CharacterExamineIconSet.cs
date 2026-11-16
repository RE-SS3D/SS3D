using UnityEngine;

namespace SS3D.UI.Examine
{
    /// <summary>
    /// Empty-slot silhouette sprites for the character-examine paperdoll's 15 wells
    /// (<see cref="SS3D.Systems.Examine.CharacterExamineSlot"/>), sourced from the same shipped
    /// <c>Assets/Art/Icons/Inventory</c> silhouettes Main HUD's equipment doll uses.
    /// </summary>
    [System.Serializable]
    public struct CharacterExamineIconSet
    {
        public Sprite Head;
        public Sprite Eyes;
        public Sprite Face;
        public Sprite Ears;
        public Sprite Suit;
        public Sprite Shirt;
        public Sprite Gloves;
        public Sprite Back;
        public Sprite Feet;
        public Sprite Belt;
        public Sprite IdCard;
        public Sprite Pocket;
        public Sprite HandLeft;
        public Sprite HandRight;
        public Sprite SuitStorage;
    }
}
