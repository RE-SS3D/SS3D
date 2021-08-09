using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Engine.Examine
{
    public class ExamineUI : MonoBehaviour
    {
		
		// These should be the AbstractExamineUIElements, listed in the same order
		// as the corresponding ExamineTypes.
		public AbstractExamineUIElement[] UIElements;
		public TMP_Text HoverName;
		public int framesPerRefresh = 10;
		private AbstractExamineUIElement CurrentUI;
		private int frames = 0;

		// Update will periodically update the displayed UI (for example, moving the panel around
		// the screen); however, it will NOT pass any additional ExamineData to the UI.
		private void Update()
		{
			// Routine refresh of the UI.
			if (CurrentUI)
			{
				frames++;
				if (frames >= framesPerRefresh)
				{
					frames = 0;
					CurrentUI.RefreshDisplay();
				}
				
				// Disable UI if user not holding down Examine button.
				if (!Input.GetButton("Examine"))
				{
					ClearData(true);
				}
			}
		}
		
		public void Start()
		{
			foreach (AbstractExamineUIElement UI in UIElements)
			{
				UI.DisableElement();
			}
		}
		
		public void LoadExamineData(IExamineData[] data)
		{
			
			// If there's no data, can't load anything.
			if (data.Length == 0){return;}
			
			// The highest Examinable listed in the target's Inspector will decide the UI to use.
			ExamineType dataType = data[0].GetExamineType();
			UpdateHoverName(data[0].GetName());
			
			// If the UI is not set, set the appropriate one.
			if (!CurrentUI)
			{
				CurrentUI = UIElements[(int)dataType];
			}
			
			// If the current UI isn't the appropriate one, change it.
			if (CurrentUI.GetExamineType() != dataType)
			{
				CurrentUI.DisableElement();
				CurrentUI = UIElements[(int)dataType];
				CurrentUI.gameObject.SetActive(true);
			}
			
			// Load the new data to the UI.
			CurrentUI.LoadExamineData(data);
			CurrentUI.RefreshDisplay();
		}

		public void UpdateHoverName(string newName)
        {
			HoverName.text = newName;
        }
		
		public void ClearData(bool preserveHoverName)
		{
			if (CurrentUI)
			{
				CurrentUI.DisableElement();
				CurrentUI = null;
			}

			if (!preserveHoverName)
            {
				UpdateHoverName("");
            }
		}
    }
}
