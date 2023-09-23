using SS3D.Attributes;
using SS3D.Core.Behaviours;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.UI.Buttons
{
    /// <summary>
    /// Generic class to manage a simple tab/panel UI
    /// </summary>
    [RequiredLayer("UI")]
    public sealed class GenericTabView : Actor
    {
        [SerializeField]
        private bool _active;

        [SerializeField]
        [NotNull]
        private Transform _panelUI;

        [SerializeField]
        [NotNull]
        private Button _tabButton;

        public Button Button => _tabButton;

        public bool Active => _active;

        public void SetTabActive(bool state)
        {
            if (_tabButton != null)
            {
                _tabButton.interactable = !state;
            }

            if (_panelUI != null)
            {
                _panelUI.gameObject.SetActive(state);
            }

            _active = state;
        }
    }
}
