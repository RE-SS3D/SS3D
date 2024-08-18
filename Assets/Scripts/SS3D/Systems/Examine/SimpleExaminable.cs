using SS3D.Core.Behaviours;
using SS3D.Systems.Selection;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    [RequireComponent(typeof(Selectable))]
    public class SimpleExaminable : Actor, IExaminable
    {
        [SerializeField] private ExamineData key;

        public ExamineData GetData()
        {
            return key;
        }
    }
}