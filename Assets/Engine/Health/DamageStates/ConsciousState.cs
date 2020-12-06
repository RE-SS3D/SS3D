namespace SS3D.Engine.Health
{
    public enum ConsciousState
    {
        Conscious = 0,          // alive and well
        BarelyConscious = 3,    // in crit, can crawl
        Unconscious = 1,        // unconscious, can't crawl
        Dead = 2                // really dead
    }
}