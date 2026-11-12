namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Range band of an examine line (examine.md §2). <see cref="Close"/> is fine print that shows
    /// only within <see cref="ExamineConfig.CloseRange"/>.
    /// </summary>
    public enum ExamineBand
    {
        Any = 0,
        Close = 1,
    }
}
