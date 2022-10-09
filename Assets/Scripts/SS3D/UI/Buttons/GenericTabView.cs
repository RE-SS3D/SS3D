using SS3D.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.UI.Buttons
{
    /// <summary>
    /// Generic class to manage a simple tab/panel UI
    /// </summary>
    [RequiredLayer("UI")]
    public sealed class GenericTabView : MonoBehaviour
    {
        [SerializeField] private bool _active;
        [SerializeField] [NotNull] private Transform _panelUI;
        [SerializeField] [NotNull] private Button _tabButton;

        public Button Button => _tabButton;
        public bool Active => _active;
        
        public void SetActive(bool state)
        {
            _tabButton.interactable = !state;
            _panelUI.gameObject.SetActive(state);
            _active = state;
        }
    }
}
