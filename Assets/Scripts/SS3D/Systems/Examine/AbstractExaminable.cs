using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    public abstract class AbstractExaminable : Actor
    {
        public abstract ExamineData GetData();
    }
}
