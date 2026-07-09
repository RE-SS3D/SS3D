using System.Collections.Generic;
using SS3D.Localization;
using UnityEngine.Localization;

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

            string name = ResolveName(data);
            string description = ResolveDescription(data);

            List<ExamineSection> sections = new();
            if (examinable is IExamineContentProvider provider)
            {
                provider.AppendSections(examinable, sections);
            }

            return new ExamineContent(name, description, sections);
        }

        private static string ResolveName(ExamineData data)
        {
            if (!data.Name.IsEmpty)
            {
                return LocalizedTextService.GetString(data.Name, data.NameKey);
            }

            return LocalizedTextService.GetString(data.LocalizationTable, data.NameKey, data.NameKey);
        }

        private static string ResolveDescription(ExamineData data)
        {
            if (!data.Description.IsEmpty)
            {
                return LocalizedTextService.GetString(data.Description, data.DescriptionKey);
            }

            return LocalizedTextService.GetString(
                data.LocalizationTable,
                data.DescriptionKey,
                data.DescriptionKey);
        }
    }
}
