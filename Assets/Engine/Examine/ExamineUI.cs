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

		// The AbstractExamineUIElement corresponding to the current examine data passed in
		private AbstractExamineUIElement CurrentUI;

		// Text box to display the item name when we hover over it (without pressing 'Shift')
		public TMP_Text HoverName;

		// Whether the hover text is enabled 
		public bool HoverTextEnabled;

		// Values required to periodically update the UI
		public const int FRAMES_PER_REFRESH = 10;
		private int frames = 0;

		// Update will periodically update the displayed UI (for example, moving the panel around
		// the screen); however, it will NOT pass any additional ExamineData to the UI.
		private void Update()
		{
			// Routine refresh of the UI.
			if (CurrentUI)
			{
				frames++;
				if (frames >= FRAMES_PER_REFRESH)
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

			// We only want the Hover Name to work when another UI is not displayed.
			if (!Input.GetButton("Examine"))
			{
				UpdateHoverName(data[0].GetName());
				//ClearData(true);
				return;
			}
			else
            {
				UpdateHoverName("");
			}

			// The highest Examinable listed in the target's Inspector will decide the UI to use.
			ExamineType dataType = data[0].GetExamineType();
			
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

		// Changes the name that is displayed in the HoverName text box. It will
		// only allow a new name if the user settings permit, but it will always
		// allow the current name to be cleared.
		public void UpdateHoverName(string newName)
        {
			if (HoverTextEnabled || newName.Equals("")) HoverName.text = newName;
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
