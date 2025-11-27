using SS3D.Attributes;
using SS3D.UI.Buttons;
using UnityEngine;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Manages the lobby tabs
    /// </summary>
    public sealed class LobbyTabsView : MonoBehaviour
    {
        [SerializeField]
        [NotNull]
        private GenericTabView[] _categoryUi;

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
                tab.SetTabActive(tab == _categoryUi[index]);
            }
        }
    }
}
