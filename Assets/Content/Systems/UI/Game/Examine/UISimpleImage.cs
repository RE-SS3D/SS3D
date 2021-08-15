using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using SS3D.Engine.Examine;

namespace SS3D.Content.Systems.Examine.UI
{
    public class UISimpleImage : AbstractExamineUIElement
    {
        public Transform Panel;
        public TMP_Text Caption;		
		public Image ImageUI;
		private string displayCaption;
		private Sprite displayImage; 
        
        public override void RefreshDisplay()
        {
            Caption.text = displayCaption;
			ImageUI.sprite = displayImage;
			
			Panel.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);

        }
		
		public override void LoadExamineData(IExamineData[] data)
		{
			StringBuilder builder = new StringBuilder();
			DataImage currentExaminable;
			
			foreach (IExamineData examineData in data)
            {
				currentExaminable = examineData as DataImage;
				if (currentExaminable != null)
				{
					gameObject.SetActive(true);
					displayCaption = currentExaminable.GetName();
					displayImage = currentExaminable.GetImage();
				}
            }			
		}
		
		public override ExamineType GetExamineType()
		{
			return ExamineType.SIMPLE_IMAGE;
		}
		
		public override void DisableElement()
		{
			displayCaption = "";
			displayImage = null;
			RefreshDisplay();
			gameObject.SetActive(false);
		}
		
    }
}