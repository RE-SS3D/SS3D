namespace SS3D.Systems.Roles
{
    /// <summary>
    /// A player's preference for one job. Resolution tries every player's High jobs before any Medium,
    /// and Medium before Low. Never excludes the job.
    /// </summary>
    public enum JobPriority : byte
    {
        Never = 0,
        Low = 1,
        Medium = 2,
        High = 3,
    }
}
