using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SS3D.Engine.Inventory.UI
{
	/// <summary>
	/// Allows the Body Target user interface to be collapsible by clicking a button.
	/// </summary>
	public class ToggleBodyTargetUI : MonoBehaviour
	{
		private Button ExpandButton;
		private CanvasGroup BodyTargetCanvas;
		private bool isShowing = true;
		private RectTransform ButtonTransform;

		void Start()
		{
			BodyTargetCanvas = GameObject.Find("Body Parts").GetComponent<CanvasGroup>();
			ExpandButton = GetComponent<Button>();
			ExpandButton.onClick.AddListener(TaskOnClick);
			ButtonTransform = GetComponent<RectTransform>();
		}

        private void Update()
        {
			// Button to toggle the internal clothing UI
			/*if (Input.GetButtonDown("Toggle Body Target"))
			{
				Toggle();
			}*/
		}
        public void TaskOnClick()
		{
			Toggle();
		}

		/// <summary>
		/// Toggle the Body Target UI using transparency and turning on or off the ability to block ray casts.
		/// </summary>
		private void Toggle()
        {
			if (isShowing)
			{
				BodyTargetCanvas.alpha = 0f; //this makes everything transparent
				BodyTargetCanvas.blocksRaycasts = false; //this prevents the UI element to receive input events
			}
			else
			{
				BodyTargetCanvas.alpha = 1f; //this makes it visible again
				BodyTargetCanvas.blocksRaycasts = true; //this allows the UI to receive inputs again.
			}
			this.gameObject.transform.GetChild(0).eulerAngles = new Vector3(this.gameObject.transform.GetChild(0).eulerAngles.x, this.gameObject.transform.GetChild(0).eulerAngles.y, this.gameObject.transform.GetChild(0).eulerAngles.z + 180);
			isShowing = !isShowing;
		}
	}
}