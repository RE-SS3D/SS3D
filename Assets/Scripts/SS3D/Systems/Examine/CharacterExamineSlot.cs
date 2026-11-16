namespace SS3D.Systems.Examine
{
    /// <summary>
    /// The conceptual equipment wells shown on the character-examine paperdoll
    /// (Documents/architecture/systems/examine.md § character examine). Distinct from
    /// <see cref="SS3D.UI.MainHud.Components.EquipmentGrid.Slot"/>/<see cref="SS3D.UI.MainHud.Components.HandsGearStrip.GearSlot"/>
    /// since the paperdoll's layout and slot set (adds Suit/Back/IdCard/Pocket, merges gloves into one
    /// well) differs from Main HUD's.
    /// </summary>
    public enum CharacterExamineSlot
    {
        Head,
        Eyes,
        Face,
        Ears,
        Suit,
        Shirt,
        Gloves,
        Back,
        Feet,
        Belt,
        IdCard,
        Pocket,
        HandLeft,
        HandRight,
        SuitStorage,
    }
}
