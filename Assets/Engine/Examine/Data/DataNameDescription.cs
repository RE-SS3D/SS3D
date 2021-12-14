using UnityEngine;

namespace SS3D.Engine.Examine

{
    public class DataNameDescription : IExamineData
    {
		private string Name;
		private string Description;
		
		public DataNameDescription(string name, string description)
		{
			Name = name;
			Description = description;
		}
		
        public string GetName()
		{
			return Name;
		}
		
		public string GetDescription()
		{
			return Description;
		}
		
		public ExamineType GetExamineType()
		{
			return ExamineType.SIMPLE_TEXT;
		}
		
    }
}