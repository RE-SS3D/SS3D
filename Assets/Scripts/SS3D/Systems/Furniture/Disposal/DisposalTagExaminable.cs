using SS3D.Systems.Examine;
using SS3D.Systems.IdAccess;
using System.Collections.Generic;

namespace SS3D.Systems.Furniture.Disposal
{
    /// <summary>
    /// Appends a disposal destination line when examining a tagged item — same
    /// "manifest is printed on the object" convention as a Cargo crate's manifest (design doc §4/§9).
    /// Item prefabs that should support disposal tagging use this in place of a plain
    /// <see cref="SimpleExaminable"/>, following the <c>IdentificationCardExaminable</c>/
    /// <c>AirAlarmExamineProvider</c> pattern.
    /// </summary>
    public class DisposalTagExaminable : SimpleExaminable, IExamineContentProvider
    {
        public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
        {
            DisposalTag tag = GetComponent<DisposalTag>();
            if (tag == null || !tag.IsTagged)
            {
                return;
            }

            string departmentName = DepartmentDisplay.GetName(tag.Destination);
            sections.Add(new ExamineSection($"Disposal tag: {departmentName}", ExamineBand.Close));
        }
    }
}
