using SS3D.Core.Behaviours;
using SS3D.Systems.Selection;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Examine
{
    [RequireComponent(typeof(Selectable))]
    public class SimpleExaminable : AbstractExaminable
    {
        [FormerlySerializedAs("key")]
        [SerializeField]
        private ExamineData _key;

        public override ExamineData GetData()
        {
            return _key;
        }
    }
}
