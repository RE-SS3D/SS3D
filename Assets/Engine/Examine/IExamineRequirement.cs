using UnityEngine;

namespace SS3D.Engine.Examine
{
    public interface IExamineRequirement
    {
        bool CanExamine(GameObject examinator);
		GameObject GetBaseObject();
    }
}