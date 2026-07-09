using System;
using System.Collections.Generic;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Resolved static and dynamic text for an examined object.
    /// </summary>
    public readonly struct ExamineContent
    {
        public static readonly ExamineContent Empty = new(string.Empty, string.Empty, Array.Empty<ExamineSection>());

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyList<ExamineSection> Sections { get; }

        public bool HasDescription => !string.IsNullOrEmpty(Description);

        public ExamineContent(string name, string description, IReadOnlyList<ExamineSection> sections)
        {
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            Sections = sections ?? Array.Empty<ExamineSection>();
        }
    }
}
