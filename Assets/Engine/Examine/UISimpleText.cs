using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Engine.Examine
{
    public class UISimpleText : MonoBehaviour, IExamineUI
    {
        public Transform Panel;
        public TMP_Text Text;		

        public void Start()
        {
            Assert.IsNotNull(Panel);
            Assert.IsNotNull(Text);
        }
        
        private void SetText(string text)
        {
            Text.text = text;
        }

        public void SetPosition(Vector2 position)
        {
            Panel.position = new Vector3(position.x, position.y, 0);
        }
		
		public void LoadExamineData(IExamineData[] data)
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
						builder.AppendLine("<b>" + displayName + "</b>");
					}
					if (displayDesc != ""){
						builder.AppendLine(displayDesc);
					}
				}
            }
			
			gameObject.SetActive(true);
			SetText(builder.ToString());
		}
		
		public ExamineType GetExamineType()
		{
			return ExamineType.SIMPLE_TEXT;
		}
		
		public void Unload()
		{
			SetText("");
			gameObject.SetActive(false);
		}
		
    }
}