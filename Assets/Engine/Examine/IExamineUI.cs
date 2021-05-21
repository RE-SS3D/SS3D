using UnityEngine;

namespace SS3D.Engine.Examine
{
    public interface IExamineUI
    {
        void LoadExamineData(IExamineData[] data);
		void SetPosition(Vector2 position);
		ExamineType GetExamineType();
		void Unload();
    }
}