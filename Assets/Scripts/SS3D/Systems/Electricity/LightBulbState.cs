namespace SS3D.Systems.Electricity
{
    /// <summary>What sits in a light fixture's socket. Only <see cref="Intact"/> can emit light.</summary>
    public enum LightBulbState : byte
    {
        Intact = 0,
        Broken = 1,
        Empty = 2,
    }
}
