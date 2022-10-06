using FishNet;
using SS3D.Attributes;
using SS3D.Core;
using SS3D.Systems.Rounds.Messages;
using SS3D.UI.Buttons;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Controls a button that starts or stops a round
    /// </summary>
    public class LobbyRoundView : SpessBehaviour
    {
        [FormerlySerializedAs("_embarkButton")] [SerializeField][NotNull] private ToggleLabelButton _startRoundButton;

        private void Start()
        {
            AddEventListeners();
        }

        private void AddEventListeners()
        {
            _startRoundButton.OnPressed += HandleEmbarkButtonPressed;
        }

        private void HandleEmbarkButtonPressed(bool state)
        {
            ChangeRoundState(state);
        }

        private void ChangeRoundState(bool state)
        {
            ChangeRoundStateMessage changeRoundStateMessage = new(state);
            InstanceFinder.ClientManager.Broadcast(changeRoundStateMessage);
        }
    }
}
