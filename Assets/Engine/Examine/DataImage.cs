using UnityEngine;

namespace SS3D.Engine.Examine
{
    public class DataImage : IExamineData
    {
		private string Caption;
		private Texture Image;
		
		public DataImage(string caption, Texture image)
		{
			Caption = caption;
			Image = image;
		}
		
        public string GetCaption()
		{
			return Caption;
		}
		
		public Texture GetImage()
		{
			return Image;
		}
		
		public ExamineType GetExamineType()
		{
			return ExamineType.SIMPLE_IMAGE;
		}
		
    }
}