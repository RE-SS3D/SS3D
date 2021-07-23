using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SS3D.Engine.Inventory.UI
{
	/// <summary>
	/// Allows the chat user interface to be collapsible by clicking a button.
	/// </summary>
	public class ToggleChat : MonoBehaviour
	{
		private CanvasGroup ChatCanvas;
		private bool isShowing = true;
		private Button ExpandButton;

        void Start()
        {
			ExpandButton = GetComponent<Button>();
			ExpandButton.onClick.AddListener(TaskOnClick);
		}

        private void Update()
        {
			// Button to toggle the chat UI
			/*if (Input.GetButtonDown("Toggle Chat"))
			{
				Toggle();
			}*/
		}

		public void Init(CanvasGroup canvasGroup)
        {
			ChatCanvas = canvasGroup;
        }

        public void TaskOnClick()
		{
			Toggle();
		}

		/// <summary>
		/// Toggle the Chat UI using transparency and turning on or off the ability to block ray casts.
		/// </summary>
		private void Toggle()
        {
			if (isShowing)
			{
				ChatCanvas.alpha = 0f; //this makes everything transparent
				ChatCanvas.blocksRaycasts = false; //this prevents the UI element to receive input events
			}
			else
			{
				ChatCanvas.alpha = 1f; //this makes it visible again
				ChatCanvas.blocksRaycasts = true; //this allows the UI to receive inputs again.
			}
			isShowing = !isShowing;
		}
	}
}