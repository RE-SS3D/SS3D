using System.Collections.Generic;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Allows examinable objects to append runtime-specific localized sections.
    /// </summary>
    public interface IExamineContentProvider
    {
        void AppendSections(IExaminable examinable, List<ExamineSection> sections);
    }
}
