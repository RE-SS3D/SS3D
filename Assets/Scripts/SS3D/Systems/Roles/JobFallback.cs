namespace SS3D.Systems.Roles
{
    /// <summary>
    /// What a character does when none of its jobs is open (lobby.md §3). Saved per character and sent with its
    /// job priorities.
    /// </summary>
    public enum JobFallback : byte
    {
        /// <summary>Join as the fallback job, which has no slot cap.</summary>
        Assistant = 0,
        /// <summary>Stay in the lobby instead of spawning.</summary>
        ReturnToLobby = 1,
    }
}
