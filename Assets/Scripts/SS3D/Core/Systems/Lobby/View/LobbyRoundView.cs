using FishNet;
using FishNet.Connection;
using FishNet.Object;
using SS3D.Core.Rounds.Messages;
using SS3D.Core.Systems.Rounds.Messages;
using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Core.Systems.Lobby.View
{
    public class LobbyRoundView : NetworkBehaviour
    {
        [SerializeField][NotNull] private Button _embarkButton;

        private void Start()
        {
            AddEventListeners();
        }

        private void AddEventListeners()
        {
            _embarkButton.onClick.AddListener(HandleEmbarkButtonPressed);
        }

        private void HandleEmbarkButtonPressed()
        {
            RequestStartRound();
        }

        private void RequestStartRound()
        {
            RequestStartRoundMessage requestStartRoundMessage = new();
            InstanceFinder.ClientManager.Broadcast(requestStartRoundMessage);
        }
    }
}
