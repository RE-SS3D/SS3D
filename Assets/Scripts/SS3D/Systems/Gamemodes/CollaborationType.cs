namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// Determines how this objective is shared among assignees.
    /// </summary>
    public enum CollaborationType
    {
        Individual = 1,  // Individual objective.
        Cooperative = 2, // Shared objective - if one assignee succeeds, the remainder succeed.
        Competitive = 3, // Shared objective - if one assignee succeeds, the remainder fail.
    }
}
