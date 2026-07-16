namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Additional localized line shown in a detailed examine view.
    /// </summary>
    public readonly struct ExamineSection
    {
        public string Text { get; }

        public ExamineSection(string text)
        {
            Text = text ?? string.Empty;
        }
    }
}
