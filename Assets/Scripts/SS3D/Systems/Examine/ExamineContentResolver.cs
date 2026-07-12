using System.Collections.Generic;
using SS3D.Localization;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Resolves static and dynamic examine text from <see cref="ExamineData"/> and optional providers.
    /// </summary>
    public class ExamineContentResolver
    {
        public ExamineContent Resolve(IExaminable examinable)
        {
            ExamineData data = examinable?.GetData();
            if (data == null)
            {
                return ExamineContent.Empty;
            }

            string name = LocalizedTextService.GetString(data.Name);
            string description = LocalizedTextService.GetString(data.Description);

            List<ExamineSection> sections = new();
            if (examinable is IExamineContentProvider provider)
            {
                provider.AppendSections(examinable, sections);
            }

            return new ExamineContent(name, description, sections);
        }
    }
}
