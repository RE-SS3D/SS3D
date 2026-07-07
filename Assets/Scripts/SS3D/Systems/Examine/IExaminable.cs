using UnityEngine;

namespace SS3D.Systems.Examine
{
    public interface IExaminable
    {
        //IExamineRequirement GetRequirements();
        ExamineData GetData();
    }

    public interface IImageExaminable : IExaminable
    {
        Sprite GetDetailedImage();
    }
}