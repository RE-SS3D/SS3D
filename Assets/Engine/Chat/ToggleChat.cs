using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using SS3D.Engine.Chat;

namespace SS3D.Engine.Inventory.UI
{
	/// <summary>
	/// Allows the chat user interface to be collapsible by clicking a button.
	/// </summary>
	public class ToggleChat : MonoBehaviour
	{
		private List<CanvasGroup> ChatCanvas;
		private bool isShowing = true;
		private Button ExpandButton;

        void Start()
        {
			ExpandButton = GetComponent<Button>();
			ExpandButton.onClick.AddListener(TaskOnClick);
			// Go ahead and hide it on the start
			Toggle();
		}

        private void Update()
        {
			// Button to toggle the chat UI
			if (Input.GetButtonDown("Toggle Chat") && EventSystem.current.currentSelectedGameObject == null)
			{
				Toggle();
			}
			// Button to focus chat. Also unhides chat if pressed.
			if (Input.GetButtonDown("Focus Chat") && EventSystem.current.currentSelectedGameObject == null)
			{
				if (!isShowing)
				{
					Toggle();
				}
				foreach (GameObject chatWindow in GameObject.FindGameObjectsWithTag("ChatWindow"))
				{
					chatWindow.GetComponent<ChatWindow>().FocusInputField();
				}
			}
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
			ChatCanvas = new List<CanvasGroup>();
			foreach (GameObject chatWindow in GameObject.FindGameObjectsWithTag("ChatWindow"))
			{
				ChatCanvas.Add(chatWindow.GetComponentInChildren<CanvasGroup>());
			}

			if (isShowing)
			{
				foreach (CanvasGroup chat in ChatCanvas)
				{
					chat.alpha = 0f; //this makes everything transparent
					chat.blocksRaycasts = false; //this prevents the UI element to receive input events
				}
			}
			else
			{
				foreach (CanvasGroup chat in ChatCanvas)
				{
					chat.alpha = 1f; //this makes everything transparent
					chat.blocksRaycasts = true; //this prevents the UI element to receive input events
				}
			}
			isShowing = !isShowing;
			EventSystem.current.SetSelectedGameObject(null);
		}
	}
}