namespace SS3D.Systems.Health
{
    /// <summary>Values are stable: they are saved and sent on the wire, so only ever append.</summary>
    public enum HealthState
    {
        Healthy = 0,
        Critical = 1,
        CardiacArrest = 2,
        Dead = 3,
        /// <summary>Down on the floor but conscious, from shock or two broken legs (health.md §4).</summary>
        Downed = 4,
    }
}
