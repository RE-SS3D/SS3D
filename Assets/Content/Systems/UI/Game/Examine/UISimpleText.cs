using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using SS3D.Engine.Examine;

namespace SS3D.Content.Systems.Examine.UI
{
    public class UISimpleText : AbstractExamineUIElement
    {
        public Transform Panel;
        public TMP_Text Text;		
		private string displayText;

        public void Start()
        {
            Assert.IsNotNull(Panel);
            Assert.IsNotNull(Text);
        }
        
        public override void RefreshDisplay()
        {
            Text.text = displayText;
			Panel.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        }
		
		public override void LoadExamineData(IExamineData[] data)
		{
			StringBuilder builder = new StringBuilder();
			DataNameDescription currentExaminable;
			string displayName;
			string displayDesc;
			
			foreach (IExamineData examineData in data)
            {
				currentExaminable = examineData as DataNameDescription;
				if (currentExaminable != null)
				{
					displayName = currentExaminable.GetName();
					displayDesc = currentExaminable.GetDescription();
					
					// Prevent blank lines being appended (relevant where a GameObject has multiple components implementing IExaminable.
					// (in this case, make displayName blank in all but one of them. For example, see Water Cooler prefab)
					if (displayName != ""){
						builder.AppendLine("<size=30>" + displayName);
					}
					if (displayDesc != ""){
						builder.AppendLine("<size=26>" + displayDesc);
					}
				}
            }
			
			gameObject.SetActive(true);   // Should not need in here.
			displayText = builder.ToString();
		}
		
		public override ExamineType GetExamineType()
		{
			return ExamineType.SIMPLE_TEXT;
		}
		
		public override void DisableElement()
		{
			displayText = "";
			RefreshDisplay();
			gameObject.SetActive(false);
		}
		
    }
}