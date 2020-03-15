namespace Engine.Inventory
{
    /// <summary>
    /// Used to reference the correct body part when severing them.
    /// If values here are changed, GameObjects using the BodyPart and Body components may need to be updated.
    /// </summary>
    public enum BodyPartType
    {
        EyeLeft = 1,
        EyeRight = 2,
        BicepsLeft = 3,
        BicepsRight = 4,
        FootLeft = 5,
        FootRight = 6,
        HandLeft = 7,
        HandRight = 8,
        Head = 9,
        LegLeft = 10,
        LegRight = 11,
        Torso = 12,
        EarLeft = 13,
        EarRight = 14
    }
}