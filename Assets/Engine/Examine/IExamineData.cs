using UnityEngine;

namespace SS3D.Engine.Examine
{
    public interface IExamineData
    {
        ExamineType GetExamineType();
        string GetName();
    }
}