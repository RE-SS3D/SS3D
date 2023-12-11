using UnityEngine;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Controls the lobby screen as a whole
    /// </summary>
    public class LobbyView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            _canvasGroup.alpha = 1;
        }
    }
}
