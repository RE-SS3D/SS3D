using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Inventory.UI
{
	/// <summary>
	/// Allows the Internal Clothing user interface to be collapsible by clicking a button.
	/// </summary>
	public class ToggleInternalClothingUI : MonoBehaviour
	{
		private Button _expandButton;
		private CanvasGroup _internalClothingCanvas;
		private bool _isShowing = true;

		private void Start()
		{
			_internalClothingCanvas = GameObject.Find("InternalClothing").GetComponent<CanvasGroup>();
			_expandButton = GetComponent<Button>();
			_expandButton.onClick.AddListener(TaskOnClick);
		}

        private void Update()
        {
			// Button to toggle the internal clothing UI
			if (Input.GetButtonDown("Toggle Internal Clothing"))
			{
				Toggle();
			}
		}

        private void TaskOnClick()
		{
			Toggle();
		}

		/// <summary>
		/// Toggle the Internal Clothing UI using transparency and turning on or off the ability to block ray casts.
		/// </summary>
		private void Toggle()
        {
			if (_isShowing)
			{
				_internalClothingCanvas.alpha = 0f; //this makes everything transparent
				_internalClothingCanvas.blocksRaycasts = false; //this prevents the UI element to receive input events
			}
			else
			{
				_internalClothingCanvas.alpha = 1f; //this makes it visible again
				_internalClothingCanvas.blocksRaycasts = true; //this allows the UI to receive inputs again.
			}
			_isShowing = !_isShowing;
		}
	}
}