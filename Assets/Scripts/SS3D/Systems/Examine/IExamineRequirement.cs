using UnityEngine;

namespace SS3D.Systems.Examine
{
    public interface IExamineRequirement
    {
        bool CanExamine(GameObject examinator);
        GameObject GetBaseObject();
    }
}
