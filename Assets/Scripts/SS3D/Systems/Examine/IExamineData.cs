using UnityEngine;

namespace SS3D.Systems.Examine
{
    public interface IExamineData
    {
        ExamineType GetExamineType();
        string GetName();
    }
}