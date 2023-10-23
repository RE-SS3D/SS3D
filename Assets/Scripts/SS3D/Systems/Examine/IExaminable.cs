namespace SS3D.Systems.Examine
{
    public interface IExaminable
    {
        IExamineRequirement GetRequirements();
        IExamineData GetData();
    }
}