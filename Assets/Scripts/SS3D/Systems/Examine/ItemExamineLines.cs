using System.Collections.Generic;
using SS3D.Systems.Inventory.Items;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Lines every item shows whatever its examinable (examine.md §7): size class at any range, and
    /// the count of a stack up close.
    /// </summary>
    public static class ItemExamineLines
    {
        public static void Append(Item item, List<ExamineSection> sections)
        {
            if (item == null)
            {
                return;
            }

            (string sizeKey, string sizeEnglish) = ExamineItemKeys.SizeWord(item.SizeClass);
            string size = ExamineText.Get(sizeKey, sizeEnglish);
            sections.Add(new ExamineSection(ExamineText.Get(ExamineItemKeys.Size, ExamineItemKeys.SizeFallback, size)));

            if (item.IsStackable)
            {
                sections.Add(new ExamineSection(
                    ExamineText.Get(ExamineItemKeys.Stack, ExamineItemKeys.StackFallback, item.StackCount),
                    ExamineBand.Close));
            }
        }
    }
}
