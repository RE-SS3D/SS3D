using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// Allows the Body Target user interface to be collapsible by clicking a button.
    /// </summary>
    public class ToggleBodyTargetUI : MonoBehaviour
    {
        private Button _expandButton;
        private CanvasGroup _bodyTargetCanvas;
        private bool _isShowing = true;
        private RectTransform _buttonTransform;

        void Start()
        {
            _bodyTargetCanvas = GameObject.Find("Body Parts").GetComponent<CanvasGroup>();
            _expandButton = GetComponent<Button>();
            _expandButton.onClick.AddListener(TaskOnClick);
            _buttonTransform = GetComponent<RectTransform>();
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
            if (_isShowing)
            {
                _bodyTargetCanvas.alpha = 0f; //this makes everything transparent
                _bodyTargetCanvas.blocksRaycasts = false; //this prevents the UI element to receive input events
            }
            else
            {
                _bodyTargetCanvas.alpha = 1f; //this makes it visible again
                _bodyTargetCanvas.blocksRaycasts = true; //this allows the UI to receive inputs again.
            }
            this.gameObject.transform.GetChild(0).eulerAngles = new Vector3(this.gameObject.transform.GetChild(0).eulerAngles.x, this.gameObject.transform.GetChild(0).eulerAngles.y, this.gameObject.transform.GetChild(0).eulerAngles.z + 180);
            _isShowing = !_isShowing;
        }
    }
}