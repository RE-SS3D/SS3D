using System.Collections.Generic;
using System.Text;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Builds the body of the detailed examine panel from resolved content, applying the range bands
    /// (examine.md §2): beyond <see cref="ExamineConfig.CloseRange"/> the flavour description and every
    /// <see cref="ExamineBand.Close"/> line drop out. Returns keyword markup; the view formats it with
    /// <see cref="ExamineKeywordFormatter"/>.
    /// </summary>
    public static class ExamineDetailText
    {
        private const string LineBreak = "\n\n";

        public static string Build(
            ExamineContent content,
            bool withinCloseRange,
            IReadOnlyList<ExamineSection> extraSections = null)
        {
            StringBuilder builder = new();
            if (withinCloseRange && content.HasDescription)
            {
                builder.Append(content.Description);
            }

            AppendSections(builder, content.Sections, withinCloseRange);
            AppendSections(builder, extraSections, withinCloseRange);
            return builder.ToString();
        }

        /// <summary>True if <paramref name="section"/> shows at the given range.</summary>
        public static bool IsShown(ExamineSection section, bool withinCloseRange) =>
            !string.IsNullOrWhiteSpace(section.Text) && (withinCloseRange || section.Band == ExamineBand.Any);

        private static void AppendSections(StringBuilder builder, IReadOnlyList<ExamineSection> sections, bool withinCloseRange)
        {
            if (sections == null)
            {
                return;
            }

            for (int i = 0; i < sections.Count; i++)
            {
                if (!IsShown(sections[i], withinCloseRange))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(LineBreak);
                }

                builder.Append(sections[i].Text);
            }
        }
    }
}
