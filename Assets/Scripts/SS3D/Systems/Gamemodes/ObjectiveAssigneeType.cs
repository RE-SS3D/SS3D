namespace SS3D.Systems.Gamemodes
{
    /// <summary>
    /// The type of assignee that an Objective is eligible to have. Shared objectives succeed and fail together.
    /// </summary>
    public enum ObjectiveAssigneeType
    {
        Personal,
        SharedAntagonists,          
        SharedNonAntagonists,
        SharedDepartment,
        SharedAllCrew,
    }
}
