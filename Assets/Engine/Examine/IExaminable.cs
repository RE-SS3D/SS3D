using UnityEngine;

namespace SS3D.Engine.Examine
{
    public interface IExaminable
    {
        bool CanExamine(GameObject examinator);
		IExamineData GetData();
    }
}