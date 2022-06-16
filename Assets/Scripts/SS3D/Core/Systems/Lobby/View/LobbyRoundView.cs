using FishNet;
using FishNet.Object;
using SS3D.Core.Rounds.Messages;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Core.Systems.Lobby.View
{
    public class LobbyRoundView : NetworkBehaviour
    {
        [SerializeField] private Button _embarkButton;

        private void Start()
        {
            _embarkButton.onClick.AddListener(HandleEmbarkButtonPressed);
        }

        private void HandleEmbarkButtonPressed()
        {
            RequestStartRoundMessage requestStartRoundMessage = new();
            InstanceFinder.ClientManager.Broadcast(requestStartRoundMessage);
        }
    }
}
