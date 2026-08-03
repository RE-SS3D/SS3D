namespace SS3D.Systems.Entities.Humanoid
{
    /// <summary>
    /// The broad body posture a humanoid can be in, used to drive the base movement layer
    /// (Sit/Prone animator bools) and to gate which emotes are allowed to play.
    /// </summary>
    public enum Posture
    {
        Standing,
        Sitting,
        Prone,
    }
}
