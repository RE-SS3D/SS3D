using UnityEngine;

namespace SS3D.Engine.Examine
{
    public class DataImage : IExamineData
    {
		private string Caption;
		private Sprite Image;
		
		public DataImage(string caption, Sprite image)
		{
			Caption = caption;
			Image = image;
		}
		
        public string GetName()
		{
			return Caption;
		}
		
		public Sprite GetImage()
		{
			return Image;
		}
		
		public ExamineType GetExamineType()
		{
			return ExamineType.SIMPLE_IMAGE;
		}
		
    }
}