using UnityEngine;

namespace SS3D.Core.Systems.Lobby.View
{
    /// <summary>
    /// Manages the lobby tabs
    /// </summary>
    public sealed class LobbyTabsView : MonoBehaviour
    {
        [SerializeField][NotNull] private GenericTabView[] _categoryUi;
        
        private void Start()
        {
            SetupGenericsTabs();
            OnTabButtonClicked(0);
        }
        
        /// <summary>
        /// Sets up UI logic
        /// </summary>
        private void SetupGenericsTabs()
        {
            for (int i = 0; i < _categoryUi.Length; i++)
            {
                int index = i;
                _categoryUi[i].Button.onClick.AddListener(() => OnTabButtonClicked(index));
            }
        }

        private void OnTabButtonClicked(int index)
        {
            foreach (GenericTabView tab in _categoryUi)
            {
                tab.UpdateCategoryState(tab == _categoryUi[index]);
            }
        }
    }
}
