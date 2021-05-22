using UnityEngine;

namespace SS3D.Engine.Examine
{
    public interface IExaminable
    {
		IExamineRequirement GetRequirements();
		IExamineData GetData();
    }
}