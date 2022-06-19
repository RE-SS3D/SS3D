using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Core.Networking.Lobby.View
{
    /// <summary>
    /// Generic class to manage a simple tab/panel UI
    /// </summary>
    public sealed class GenericTabView : MonoBehaviour
    {
        [SerializeField] private bool _active;
        [SerializeField] private Transform _panelUI;
        [SerializeField] private Button _tabButton;

        public Button Button => _tabButton;
        public bool Active => _active;
        
        public void UpdateCategoryState(bool state)
        {
            _tabButton.interactable = !state;
            _panelUI.gameObject.SetActive(state);
            _active = state;
        }
    
    }
}
