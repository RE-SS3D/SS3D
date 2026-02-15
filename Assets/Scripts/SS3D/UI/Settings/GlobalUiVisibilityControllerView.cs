using System.Collections.Generic;
using SS3D.Attributes;
using SS3D.Core.Behaviours;
using UnityEngine;

namespace SS3D.UI.Settings
{
    /// <summary>
    /// Controls the visibility of tracked UI elements
    /// </summary>
    [RequiredLayer("UI")]
    public sealed class GlobalUiVisibilityControllerView : View
    {
        [SerializeField]
        private List<GameObject> _uiElements;

        protected override void OnDestroyed()
        {
            _uiElements.Clear();
        }

        /// <summary>
        /// Register object in the list of tracked objects
        /// </summary>
        /// <param name="go">GameObject to add</param>
        public void RegisterToggle(GameObject go)
        {
            if (!_uiElements.Contains(go))
            { 
                _uiElements.Add(go);    
            }
        }
        
        /// <summary>
        /// Removes an object from the list of tracked objects
        /// </summary>
        /// <param name="go">GameObject to remove</param>
        public void UnregisterToggle(GameObject go)
        {
            if (_uiElements.Contains(go))
            {
                _uiElements.Remove(go);
            }
        }
        
        /// <summary>
        /// Changes the visibility of tracked object
        /// </summary>
        /// <param name="isActive">State in which GameObjects should change</param>
        public void ToggleUIVisibility(bool isActive)
        {
            if (_uiElements.Count > 0)
            {
                foreach (var uiElement in _uiElements)
                {
                    uiElement.SetActive(isActive);
                }
            }
        }
    }
}