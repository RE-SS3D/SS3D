using System.Collections.Generic;
using SS3D.Localization;
using SS3D.Systems.IdAccess;
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

            string taggedJob = new PresentedIdentity(idCard.OwnerName, idCard.RoleName, idCard.Department).TaggedJob;
            if (!string.IsNullOrWhiteSpace(taggedJob))
            {
                string roleLine = LocalizedTextService.GetFormattedString(
                    ExamineCanonicalKeyGenerator.ExamineTableName,
                    ExamineIdentificationKeys.RoleLine,
                    new object[] { taggedJob },
                    string.Format(ExamineIdentificationKeys.RoleFallback, taggedJob));

                sections.Add(new ExamineSection(roleLine));
            }

            string departmentName = DepartmentDisplay.GetName(idCard.Department);
            if (!string.IsNullOrWhiteSpace(departmentName))
            {
                sections.Add(new ExamineSection($"Department: {departmentName}"));
            }
        }
    }
}
