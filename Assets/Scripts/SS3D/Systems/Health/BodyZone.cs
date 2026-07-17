namespace SS3D.Systems.Health
{
    /// <summary>
    /// Seven gameplay damage zones per design spec. Groin has no dedicated collider — resolved via vertical banding on torso hits.
    /// </summary>
    public enum BodyZone
    {
        Head = 0,
        Chest = 1,
        LeftArm = 2,
        RightArm = 3,
        LeftLeg = 4,
        RightLeg = 5,
        Groin = 6,
    }
}
