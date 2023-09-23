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

        public void TaskOnClick()
        {
            Toggle();
        }

        protected void Start()
        {
            _bodyTargetCanvas = GameObject.Find("Body Parts").GetComponent<CanvasGroup>();
            _expandButton = GetComponent<Button>();
            _expandButton.onClick.AddListener(TaskOnClick);
            _buttonTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// Toggle the Body Target UI using transparency and turning on or off the ability to block ray casts.
        /// </summary>
        private void Toggle()
        {
            if (_isShowing)
            {
                _bodyTargetCanvas.alpha = 0f;
                _bodyTargetCanvas.blocksRaycasts = false;
            }
            else
            {
                _bodyTargetCanvas.alpha = 1f;
                _bodyTargetCanvas.blocksRaycasts = true;
            }

            Transform child = gameObject.transform.GetChild(0);
            Vector3 eulerAngles = child.eulerAngles;

            eulerAngles = new Vector3(eulerAngles.x, eulerAngles.y, eulerAngles.z + 180);
            child.eulerAngles = eulerAngles;
            _isShowing = !_isShowing;
        }
    }
}