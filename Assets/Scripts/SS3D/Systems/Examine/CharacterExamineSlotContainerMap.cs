using SS3D.Systems.Inventory.Containers;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Maps the character-examine paperdoll's conceptual slots onto the closest existing
    /// <see cref="ContainerType"/> — reusing Main HUD's slot↔container precedent
    /// (<c>MainHudSubSystem.EquipmentSlotToContainerType</c>/<c>GearSlotToContainerType</c>) rather than
    /// extending the shared enum. <see cref="CharacterExamineSlot.HandLeft"/>/<see cref="CharacterExamineSlot.HandRight"/>
    /// are not covered here — hand contents come from <c>HumanInventory.Hands</c> directly, not a
    /// <see cref="ContainerType"/> lookup.
    /// </summary>
    public static class CharacterExamineSlotContainerMap
    {
        /// <summary>
        /// Returns the container type(s) backing a paperdoll slot. <paramref name="secondary"/> is
        /// <see cref="ContainerType.None"/> when the slot has no alternate (paired) container to fall
        /// back to.
        /// </summary>
        public static bool TryGetContainerTypes(CharacterExamineSlot slot, out ContainerType primary, out ContainerType secondary)
        {
            switch (slot)
            {
                case CharacterExamineSlot.Head:
                    primary = ContainerType.Head;
                    secondary = ContainerType.None;
                    return true;
                case CharacterExamineSlot.Eyes:
                    primary = ContainerType.Glasses;
                    secondary = ContainerType.None;
                    return true;
                case CharacterExamineSlot.Face:
                    primary = ContainerType.Mask;
                    secondary = ContainerType.None;
                    return true;
                case CharacterExamineSlot.Ears:
                    primary = ContainerType.EarLeft;
                    secondary = ContainerType.EarRight;
                    return true;
                case CharacterExamineSlot.Suit:
                    // ExoSuit has no other UI consumer today — first real surface for it.
                    primary = ContainerType.ExoSuit;
                    secondary = ContainerType.None;
                    return true;
                case CharacterExamineSlot.Shirt:
                    primary = ContainerType.Jumpsuit;
                    secondary = ContainerType.None;
                    return true;
                case CharacterExamineSlot.Gloves:
                    primary = ContainerType.GloveLeft;
                    secondary = ContainerType.GloveRight;
                    return true;
                case CharacterExamineSlot.Back:
                    primary = ContainerType.Bag;
                    secondary = ContainerType.None;
                    return true;
                case CharacterExamineSlot.Feet:
                    primary = ContainerType.ShoeLeft;
                    secondary = ContainerType.ShoeRight;
                    return true;
                case CharacterExamineSlot.Belt:
                    primary = ContainerType.Belt;
                    secondary = ContainerType.None;
                    return true;
                case CharacterExamineSlot.IdCard:
                    primary = ContainerType.Identification;
                    secondary = ContainerType.None;
                    return true;
                case CharacterExamineSlot.Pocket:
                    primary = ContainerType.Pocket;
                    secondary = ContainerType.None;
                    return true;
                case CharacterExamineSlot.SuitStorage:
                    primary = ContainerType.SuitStorage;
                    secondary = ContainerType.None;
                    return true;
                default:
                    primary = ContainerType.None;
                    secondary = ContainerType.None;
                    return false;
            }
        }
    }
}
