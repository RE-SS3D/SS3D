using System.Collections.Generic;
using SS3D.Systems.Examine;
using SS3D.Systems.IdAccess;
using UnityEngine;

namespace SS3D.Systems.Furniture.Disposal
{
    /// <summary>
    /// Runtime destination label attached to an item at a chute's tagger interaction (design doc §4).
    /// A real, examine-readable property on the object itself — not hidden metadata. It is its own
    /// examine provider, read up close (examine.md §7). Added at runtime on the server, so it does not
    /// replicate yet: only the host sees the tag (examine feedback effort, Phase 3 note).
    /// </summary>
    public sealed class DisposalTag : MonoBehaviour, IExamineContentProvider
    {
        public const string ExamineKey = "examine.disposal.tag";
        public const string ExamineFallback = "Disposal tag: {0}.";

        public Department Destination { get; private set; } = Department.None;

        public bool IsTagged => Destination != Department.None;

        public void SetDestination(Department department)
        {
            Destination = department;
        }

        public void Clear()
        {
            Destination = Department.None;
        }

        public void AppendSections(IExaminable examinable, List<ExamineSection> sections)
        {
            if (!IsTagged)
            {
                return;
            }

            sections.Add(new ExamineSection(
                ExamineText.Get(ExamineKey, ExamineFallback, DepartmentDisplay.GetName(Destination)),
                ExamineBand.Close));
        }
    }
}
