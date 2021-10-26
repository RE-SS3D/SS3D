using UnityEngine;

namespace SS3D.Engine.Examine
{
    public interface IExaminable : ISelectable
    {
		IExamineRequirement GetRequirements();
		IExamineData GetData();
    }
}