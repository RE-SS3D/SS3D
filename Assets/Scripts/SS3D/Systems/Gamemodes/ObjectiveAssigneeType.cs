namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// The type of assignee that an objective is eligible to have. For example,
    /// some objectives are only relevant to antagonists or non-antagonists
    /// antagonists.
    /// </summary>
    public enum Alignment
    {
        Any,
        Antagonists,          
        NonAntagonists,
    }

    /// <summary>
    /// Determines how this objective is shared among assignees.
    /// </summary>
    public enum CollaborationType
    {
        Individual,  // Individual objective.
        Cooperative, // Shared objective - if one assignee succeeds, the remainder succeed.
        Competitive  // Shared objective - if one assignee succeeds, the remainder fail.
    }
}
