using System.Collections.Generic;
using SS3D.Localization;
using SS3D.Systems.Inventory.Items.Generic;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Appends runtime owner and role lines when examining identification cards.
    /// </summary>
    public class IdentificationCardExaminable : SimpleExaminable, IExamineContentProvider
    {
        public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
        {
            ExamineData data = examinable?.GetData();
            if (data == null || data.Type != ExamineType.IDENTIFICATION_CARD)
            {
                return;
            }

            IDCard idCard = GetComponent<IDCard>();
            if (idCard == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(idCard.OwnerName))
            {
                string ownerLine = LocalizedTextService.GetFormattedString(
                    ExamineCanonicalKeyGenerator.ExamineTableName,
                    ExamineIdentificationKeys.OwnerLine,
                    new object[] { idCard.OwnerName },
                    string.Format(ExamineIdentificationKeys.OwnerFallback, idCard.OwnerName));

                sections.Add(new ExamineSection(ownerLine));
            }

            if (!string.IsNullOrWhiteSpace(idCard.RoleName))
            {
                string roleLine = LocalizedTextService.GetFormattedString(
                    ExamineCanonicalKeyGenerator.ExamineTableName,
                    ExamineIdentificationKeys.RoleLine,
                    new object[] { idCard.RoleName },
                    string.Format(ExamineIdentificationKeys.RoleFallback, idCard.RoleName));

                sections.Add(new ExamineSection(roleLine));
            }
        }
    }
}
