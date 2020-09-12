namespace SS3D.Engine.Health
{
    /// <summary>
    /// Used to reference the correct body part when severing them.
    /// If values here are changed, GameObjects using the BodyPart and Body components need to be updated.
    /// Also used in the BodyPart selector in the UI.
    ///
    /// This is intended to be used for humanoids, but with dogs it might work the same way, spiders though?
    /// </summary>
    public enum BodyPartType
    {
        EyeLeft = 1,
        EyeRight = 2,
        ArmLeft = 3,
        ArmRight = 4,
        FootLeft = 5,
        FootRight = 6,
        HandLeft = 7,
        HandRight = 8,
        Head = 9,
        LegLeft = 10,
        LegRight = 11,
        Chest = 12,
        EarLeft = 13,
        EarRight = 14,
        Groin = 15,
        Mouth = 16
    }
}