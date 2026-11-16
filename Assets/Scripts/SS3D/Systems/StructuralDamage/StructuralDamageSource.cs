namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Who applied structural force. Blast / chemistry / shuttle sources land in later phases.
    /// </summary>
    public enum StructuralDamageSource : byte
    {
        Unknown = 0,
        Melee = 1,
        Blast = 2,
        Console = 3,
        Other = 4,
        Ranged = 5,
    }
}
