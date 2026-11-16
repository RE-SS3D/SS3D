using SS3D.Core.Behaviours;
using SS3D.Systems.Selection;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    /// <summary>
    /// Examine content for hover and shift-hold detailed panels. Not an interaction target —
    /// <see cref="ExamineSubSystem"/> reads <see cref="IExaminable"/> from selection directly.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public abstract class ExaminableBase : Actor, IExaminable
    {
        [SerializeField] private ExamineData key;

        public ExamineData GetData()
        {
            return key;
        }
    }
}
