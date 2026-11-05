namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Shoes and gloves are one item worn on both sides, kept in the left container. The right
    /// container only takes over once the left side is gone from the body.
    /// </summary>
    public static class PairedClothing
    {
        /// <summary>The other side of a paired cloth type, as a body part and as a container.</summary>
        public static bool TryGetOtherSide(ClothType type, out ClothType otherCloth, out ContainerType otherContainer)
        {
            (otherCloth, otherContainer) = type switch
            {
                ClothType.ShoeLeft => (ClothType.ShoeRight, ContainerType.ShoeRight),
                ClothType.ShoeRight => (ClothType.ShoeLeft, ContainerType.ShoeLeft),
                ClothType.GloveLeft => (ClothType.GloveRight, ContainerType.GloveRight),
                ClothType.GloveRight => (ClothType.GloveLeft, ContainerType.GloveLeft),
                _ => (type, ContainerType.None),
            };
            return otherContainer != ContainerType.None;
        }

        /// <summary>The left side of a paired right container, or None.</summary>
        public static ContainerType FirstSideOf(ContainerType type) => type switch
        {
            ContainerType.ShoeRight => ContainerType.ShoeLeft,
            ContainerType.GloveRight => ContainerType.GloveLeft,
            _ => ContainerType.None,
        };
    }
}
