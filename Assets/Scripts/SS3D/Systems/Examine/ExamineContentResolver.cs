using System.Collections.Generic;
using SS3D.Localization;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Resolves static and dynamic examine text from <see cref="ExamineData"/> and optional providers.
    /// Any item also gets the generic size and stack lines; an item with no examinable, or one without
    /// data, resolves through <see cref="ItemFallbackExaminable"/> to its name, those lines and its providers.
    /// </summary>
    public class ExamineContentResolver
    {
        public ExamineContent Resolve(IExaminable examinable)
        {
            if (examinable is ItemFallbackExaminable fallback)
            {
                if (fallback.Item == null)
                {
                    return ExamineContent.Empty;
                }

                List<ExamineSection> itemSections = new();
                ItemExamineLines.Append(fallback.Item, itemSections);
                AppendProviders(fallback.Item.GetComponents<IExamineContentProvider>(), examinable, itemSections);
                return new ExamineContent(fallback.Item.Name, string.Empty, itemSections);
            }

            ExamineData data = examinable?.GetData();
            if (data == null)
            {
                return ExamineContent.Empty;
            }

            string name = LocalizedTextService.GetString(data.Name);
            string description = LocalizedTextService.GetString(data.Description);

            List<ExamineSection> sections = new();
            if (examinable is Component owner && owner.TryGetComponent(out Item item))
            {
                ItemExamineLines.Append(item, sections);
            }

            AppendProviderSections(examinable, sections);

            return new ExamineContent(name, description, sections);
        }

        private static void AppendProviderSections(IExaminable examinable, List<ExamineSection> sections)
        {
            if (examinable is Component component)
            {
                AppendProviders(component.GetComponents<IExamineContentProvider>(), examinable, sections);
                return;
            }

            if (examinable is IExamineContentProvider provider)
            {
                provider.AppendSections(examinable, sections);
            }
        }

        private static void AppendProviders(IExamineContentProvider[] providers, IExaminable examinable, List<ExamineSection> sections)
        {
            for (int i = 0; i < providers.Length; i++)
            {
                providers[i].AppendSections(examinable, sections);
            }
        }
    }
}
