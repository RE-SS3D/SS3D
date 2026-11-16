namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Additional localized line shown in a detailed examine view. The text may carry
    /// <c>&lt;k=…&gt;</c> keyword tags (<see cref="ExamineKeywordFormatter"/>).
    /// </summary>
    public readonly struct ExamineSection
    {
        public string Text { get; }

        /// <summary>Which range band the line reads at (examine.md §2).</summary>
        public ExamineBand Band { get; }

        public ExamineSection(string text, ExamineBand band = ExamineBand.Any)
        {
            Text = text ?? string.Empty;
            Band = band;
        }
    }
}
